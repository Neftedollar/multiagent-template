using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace MultiagentSetup;

public sealed class SetupCommand(string projectName, string? requestedOrg, string provider = "claude")
{
    public async Task<int> ExecuteAsync()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
        Console.WriteLine($"\nmultiagent-setup v{version}");
        Console.WriteLine($"  Project:  {projectName}");
        Console.WriteLine($"  Provider: {provider}");
        Console.WriteLine();

        Console.WriteLine("Pre-flight checks...");
        Console.WriteLine();
        if (!CheckTools(provider)) return 1;

        var org = await ResolveOrgAsync();
        if (org is null) return 1;

        var targetDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), projectName));
        if (Directory.Exists(targetDir))
        {
            Console.Error.WriteLine($"Error: {targetDir} already exists");
            return 1;
        }

        var graphName = $"{projectName.ToLower()}-ops";

        Console.WriteLine("Creating workspace...");
        Console.WriteLine($"  Project:    {projectName}");
        Console.WriteLine($"  GitHub org: {org}");
        Console.WriteLine($"  Graph:      {graphName}");
        Console.WriteLine($"  Target:     {targetDir}");
        Console.WriteLine();

        var providers = provider == "all"
            ? ProviderRegistry.AllExpansion.ToArray()
            : new[] { provider };

        CreateDirectories(targetDir, providers);

        var vars = BuildVars(projectName, org, graphName);
        ExtractTemplates(targetDir, vars, providers);
        Console.WriteLine("  OK: templates extracted");

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            await ChmodShellScriptsAsync(targetDir);
            Console.WriteLine("  OK: permissions set");
        }

        if (providers.Any(p => p is "claude" or "nessy"))
            await SetupAgencyRolesAsync(targetDir);
        await GitInitAsync(targetDir);
        Console.WriteLine("  OK: git initialized");

        await OfferCompletionsAsync(targetDir);

        Console.WriteLine();
        Console.WriteLine($"Done! Workspace created at: {targetDir}");
        Console.WriteLine();
        Console.WriteLine("Next steps:");
        Console.WriteLine($"  1. cd {targetDir}");
        Console.WriteLine($"  2. Clone your code repo into code/{projectName}");
        Console.WriteLine($"  3. (Optional) Install MCPs: multiagent-setup install-mcps");
        if (providers.Any(p => p is "claude" or "nessy"))
            Console.WriteLine($"  4. (Optional) Update roles: multiagent-setup sync-roles --pull");
        Console.WriteLine($"  5. Start working:");
        foreach (var name in providers)
        {
            var def = ProviderRegistry.Find(name);
            if (def is not null)
                Console.WriteLine(def.NextStepTemplate.Replace("{cwd}", targetDir));
        }
        Console.WriteLine();
        return 0;
    }

    // ── Pre-flight ────────────────────────────────────────────────────────────

    private static bool CheckTools(string provider)
    {
        var ok = true;
        ok &= Require("git", macOs: "brew install git", win: "winget install Git.Git");
        ok &= Require("jq",  macOs: "brew install jq",  win: "winget install jqlang.jq");
        ok &= Require("gh",  macOs: "brew install gh",   win: "winget install GitHub.cli");
        var providers = provider == "all" ? ProviderRegistry.AllExpansion.ToArray() : new[] { provider };
        foreach (var name in providers)
        {
            var def = ProviderRegistry.Find(name);
            if (def is null) continue;
            switch (def.ToolCheck)
            {
                case ToolCheckMode.Suggest: Suggest(def.BinaryName!, def.InstallHint); break;
                case ToolCheckMode.Info:    Console.WriteLine($"  INFO: {def.InstallHint}"); break;
            }
        }
        Console.WriteLine();
        return ok;
    }

    private static bool Require(string name, string macOs = "", string win = "")
    {
        if (ProcessHelper.IsOnPath(name)) { Console.WriteLine($"  OK: {name}"); return true; }
        var hint = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? win : macOs;
        var install = string.IsNullOrEmpty(hint) ? "install it and re-run" : $"{hint}";
        Console.Error.WriteLine($"  FAIL: {name} not found — {install}");
        return false;
    }

    private static void Suggest(string name, string installUrl)
    {
        if (ProcessHelper.IsOnPath(name)) Console.WriteLine($"  OK: {name}");
        else Console.WriteLine($"  WARN: {name} not found — install: {installUrl}");
    }

    // ── GitHub org resolution ─────────────────────────────────────────────────

    private async Task<string?> ResolveOrgAsync()
    {
        if (requestedOrg is not null) return requestedOrg;

        var (authCode, _, _) = await ProcessHelper.RunAsync("gh", ["auth", "status"],
            captureOutput: true, allowFailure: true);

        if (authCode != 0)
        {
            Console.WriteLine("GitHub CLI not authenticated. Launching login...");
            var loginCode = await ProcessHelper.RunInteractiveAsync("gh", ["auth", "login"]);
            if (loginCode != 0)
            {
                Console.Error.WriteLine("FAIL: gh auth login failed");
                return null;
            }
        }

        var (code, stdout, _) = await ProcessHelper.RunAsync("gh", ["api", "user", "--jq", ".login"],
            captureOutput: true);
        if (code != 0 || string.IsNullOrWhiteSpace(stdout))
        {
            Console.Error.WriteLine("FAIL: could not resolve GitHub username");
            return null;
        }
        return stdout.Trim();
    }

    // ── Directory creation ────────────────────────────────────────────────────

    private static void CreateDirectories(string root, string[] providers)
    {
        // Shared
        foreach (var d in new[] { "code", "docs/workflows", "docs/archive", "docs/obsolete-docs", "tools" })
            Directory.CreateDirectory(Path.Combine(root, d.Replace('/', Path.DirectorySeparatorChar)));

        // Provider-specific dirs (driven by registry; Directory.CreateDirectory is idempotent)
        foreach (var name in providers)
        {
            var def = ProviderRegistry.Find(name);
            if (def is null) continue;
            foreach (var dir in def.Directories)
                Directory.CreateDirectory(Path.Combine(root, dir.Replace('/', Path.DirectorySeparatorChar)));
        }
    }

    // ── Template extraction ───────────────────────────────────────────────────

    private static Dictionary<string, string> BuildVars(string project, string org, string graph) => new()
    {
        ["{{PROJECT_NAME}}"]        = project,
        ["{{PROJECT_DESCRIPTION}}"] = $"{project} project workspace",
        ["{{FOUNDER}}"]             = Environment.UserName,
        ["{{PHASE}}"]               = "early development",
        ["{{GITHUB_ORG}}"]          = org,
        ["{{GITHUB_REPO}}"]         = project,
        ["{{GRAPH_NAME}}"]          = graph,
        ["{{DATE}}"]                = DateTime.Today.ToString("yyyy-MM-dd"),
        ["{{HOOK_EXEC}}"]           = TemplateResources.ResolveHookExec(),
    };

    private static void ExtractTemplates(string root, Dictionary<string, string> vars, string[] providers)
    {
        var asm = Assembly.GetExecutingAssembly();
        foreach (var resourceName in asm.GetManifestResourceNames())
        {
            var outputRel = ResolveOutputPath(resourceName, providers);
            if (outputRel is null) continue;

            var outputPath = Path.Combine(root, outputRel.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            using var stream = asm.GetManifestResourceStream(resourceName)!;

            if (TemplateResources.IsTextResource(resourceName))
            {
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var content = reader.ReadToEnd();
                foreach (var (k, v) in vars)
                    content = content.Replace(k, v, StringComparison.Ordinal);
                File.WriteAllText(outputPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }
            else
            {
                using var file = File.Create(outputPath);
                stream.CopyTo(file);
            }
        }
    }

    private static string? ResolveOutputPath(string resourceName, string[] providers)
    {
        // .claude/ resources — active for claude or nessy
        if (resourceName.StartsWith(".claude/"))
            return (providers.Contains("claude") || providers.Contains("nessy")) ? resourceName : null;

        // .github/workflows/ — scaffolded for claude/nessy workspaces
        if (resourceName.StartsWith(".github/workflows/"))
            return (providers.Contains("claude") || providers.Contains("nessy")) ? resourceName : null;

        // Provider-prefixed templates (registry-driven; nessy has null prefix and is handled above)
        foreach (var def in ProviderRegistry.All)
        {
            if (def.TemplatePrefix is null) continue;
            if (resourceName.StartsWith(def.TemplatePrefix))
                return providers.Contains(def.Name)
                    ? resourceName[def.TemplatePrefix.Length..]
                    : null;
        }

        // Shared (CLAUDE.md, docs/, tools/)
        return resourceName;
    }


    // ── Permissions ───────────────────────────────────────────────────────────

    private static async Task ChmodShellScriptsAsync(string root)
    {
        var scripts = Directory.GetFiles(root, "*.sh", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(root, "*.zsh", SearchOption.AllDirectories));
        foreach (var s in scripts)
            await ProcessHelper.RunAsync("chmod", ["+x", s], allowFailure: true);
    }

    // ── Agency-agents + role sync ─────────────────────────────────────────────

    private static async Task SetupAgencyRolesAsync(string workspaceRoot)
    {
        var agencyDir = Path.GetFullPath(Path.Combine(workspaceRoot, "..", "agency-agents"));
        await new SyncRolesCommand("--clone", agencyDir).ExecuteAsync();
    }

    // ── Git init ──────────────────────────────────────────────────────────────

    private static async Task GitInitAsync(string root)
    {
        Console.WriteLine("Initializing git...");

        var (initCode, _, initErr) = await ProcessHelper.RunAsync(
            "git", ["init", "-q"], workingDir: root, captureOutput: true);
        if (initCode != 0) throw new InvalidOperationException($"git init failed: {initErr}");

        await File.WriteAllTextAsync(
            Path.Combine(root, ".gitignore"),
            "code/\n*.png\n.DS_Store\n.claude/settings.local.json\n");

        var (addCode, _, addErr) = await ProcessHelper.RunAsync(
            "git", ["add", "-A"], workingDir: root, captureOutput: true);
        if (addCode != 0) throw new InvalidOperationException($"git add failed: {addErr}");

        var (commitCode, _, commitErr) = await ProcessHelper.RunAsync(
            "git", ["commit", "-q", "-m", "init: multi-agent workspace from template"],
            workingDir: root, captureOutput: true);
        if (commitCode != 0) throw new InvalidOperationException($"git commit failed: {commitErr}");
    }

    // ── Zsh completions ───────────────────────────────────────────────────────

    private static async Task OfferCompletionsAsync(string workspaceRoot)
    {
        if (Console.IsInputRedirected) return;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            await OfferCompletionsWindowsAsync(workspaceRoot);
        else
            await OfferCompletionsUnixAsync(workspaceRoot);
    }

    private static async Task OfferCompletionsUnixAsync(string workspaceRoot)
    {
        var zshrc = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".zshrc");
        if (!File.Exists(zshrc)) return;

        var existing = await File.ReadAllTextAsync(zshrc);
        if (existing.Contains("completions.zsh")) return;

        Console.Write("\nAdd zsh completions to ~/.zshrc? [y/N] ");
        var key = Console.ReadKey(intercept: false);
        Console.WriteLine();

        if (key.KeyChar is 'y' or 'Y')
        {
            var completionsPath = Path.Combine(workspaceRoot, "tools", "completions.zsh");
            // Single-quote the path to prevent shell interpretation of special characters
            var escapedPath = completionsPath.Replace("'", "'\\''");
            await File.AppendAllTextAsync(zshrc,
                $"\n# Multi-agent workspace completions\nsource -- '{escapedPath}'\n");
            Console.WriteLine("  OK: completions added (restart shell or: source ~/.zshrc)");
        }
    }

    private static async Task OfferCompletionsWindowsAsync(string workspaceRoot)
    {
        // $PROFILE is a PowerShell-internal variable, not exported to child processes.
        // Always derive the path from SpecialFolder.MyDocuments.
        var profile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "PowerShell", "Microsoft.PowerShell_profile.ps1");

        if (!string.IsNullOrEmpty(profile) && File.Exists(profile))
        {
            var existing = await File.ReadAllTextAsync(profile);
            if (existing.Contains("completions.ps1")) return;
        }

        Console.Write("\nAdd PowerShell completions to $PROFILE? [y/N] ");
        var key = Console.ReadKey(intercept: false);
        Console.WriteLine();

        if (key.KeyChar is 'y' or 'Y')
        {
            var completionsPath = Path.Combine(workspaceRoot, "tools", "completions.ps1");
            Directory.CreateDirectory(Path.GetDirectoryName(profile)!);
            await File.AppendAllTextAsync(profile,
                $"\r\n# Multi-agent workspace completions\r\n. \"{completionsPath}\"\r\n");
            Console.WriteLine("  OK: completions added (restart PowerShell or: . $PROFILE)");
        }
    }
}
