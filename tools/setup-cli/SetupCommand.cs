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
            ? new[] { "claude", "codex", "qwen", "cursor", "windsurf", "copilot", "gemini", "cline", "aider" }
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
        if (providers.Contains("claude") || providers.Contains("nessy"))
            Console.WriteLine($"  4. (Optional) Update roles: multiagent-setup sync-roles --pull");
        Console.WriteLine($"  5. Start working:");
        if (providers.Contains("claude"))
            Console.WriteLine($"       claude        → /orchestrator <task>");
        if (providers.Contains("nessy"))
            Console.WriteLine($"       nessy         → /orchestrator <task>");
        if (providers.Contains("codex"))
            Console.WriteLine($"       codex         → /orchestrator <task>");
        if (providers.Contains("qwen"))
            Console.WriteLine($"       qwen-code     → /orchestrator <task>");
        if (providers.Contains("cursor"))
            Console.WriteLine($"       cursor        → open {targetDir}, rules load automatically");
        if (providers.Contains("windsurf"))
            Console.WriteLine($"       windsurf      → open {targetDir}, rules load automatically");
        if (providers.Contains("copilot"))
            Console.WriteLine($"       copilot       → open {targetDir} in VS Code, reads .github/copilot-instructions.md");
        if (providers.Contains("gemini"))
            Console.WriteLine($"       gemini        → /orchestrator <task>");
        if (providers.Contains("cline"))
            Console.WriteLine($"       cline         → open {targetDir} in VS Code, .clinerules loads automatically");
        if (providers.Contains("aider"))
            Console.WriteLine($"       aider         → run 'aider' from {targetDir}, CLAUDE.md loaded automatically");
        Console.WriteLine();
        return 0;
    }

    // ── Pre-flight ────────────────────────────────────────────────────────────

    private static bool CheckTools(string provider)
    {
        var ok = true;
        ok &= Require("git", macOs: "brew install git",      win: "winget install Git.Git");
        ok &= Require("jq",  macOs: "brew install jq",       win: "winget install jqlang.jq");
        ok &= Require("gh",  macOs: "brew install gh",        win: "winget install GitHub.cli");
        var providers = provider == "all" ? new[] { "claude", "codex", "qwen", "cursor", "windsurf", "copilot", "gemini", "cline", "aider" } : new[] { provider };
        if (providers.Contains("claude"))
            Suggest("claude",     "https://docs.anthropic.com/en/docs/claude-code");
        if (providers.Contains("nessy"))
            Suggest("nessy",      "https://nessy.ai");
        if (providers.Contains("codex"))
            Suggest("codex",      "https://github.com/openai/codex");
        if (providers.Contains("qwen"))
            Suggest("qwen-code",  "https://github.com/QwenLM/qwen-code");
        if (providers.Contains("cursor"))
            Console.WriteLine("  INFO: cursor — IDE tool, install from https://cursor.com");
        if (providers.Contains("windsurf"))
            Console.WriteLine("  INFO: windsurf — IDE tool, install from https://windsurf.com");
        if (providers.Contains("copilot"))
            Console.WriteLine("  INFO: copilot — GitHub Copilot, install VS Code extension");
        if (providers.Contains("gemini"))
            Suggest("gemini",     "https://ai.google.dev/gemini-api/docs/gemini-cli");
        if (providers.Contains("cline"))
            Console.WriteLine("  INFO: cline — VS Code extension, install from marketplace");
        if (providers.Contains("aider"))
            Suggest("aider",      "https://aider.chat");
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

        if (providers.Contains("claude") || providers.Contains("nessy"))
        {
            Directory.CreateDirectory(Path.Combine(root, ".claude", "commands"));
            Directory.CreateDirectory(Path.Combine(root, ".claude", "hooks"));
        }
        if (providers.Contains("codex"))
            Directory.CreateDirectory(Path.Combine(root, ".codex", "skills"));
        if (providers.Contains("qwen"))
            Directory.CreateDirectory(Path.Combine(root, ".qwen"));
        if (providers.Contains("cursor"))
            Directory.CreateDirectory(Path.Combine(root, ".cursor", "rules"));
        if (providers.Contains("windsurf"))
            Directory.CreateDirectory(Path.Combine(root, ".windsurf", "rules"));
        if (providers.Contains("copilot"))
            Directory.CreateDirectory(Path.Combine(root, ".github"));
        if (providers.Contains("gemini"))
            Directory.CreateDirectory(Path.Combine(root, ".gemini"));
        // cline and aider write files to workspace root — no subdirectory needed
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
        ["{{HOOK_EXEC}}"]           = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                                        ? @"$env:USERPROFILE\.dotnet\tools\multiagent-setup.exe"
                                        : "$HOME/.dotnet/tools/multiagent-setup",
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
        if (resourceName.StartsWith(".claude/"))
            return (providers.Contains("claude") || providers.Contains("nessy")) ? resourceName : null;

        if (resourceName.StartsWith("providers/codex/"))
            return providers.Contains("codex") ? resourceName["providers/codex/".Length..] : null;

        if (resourceName.StartsWith("providers/qwen/"))
            return providers.Contains("qwen")  ? resourceName["providers/qwen/".Length..]  : null;

        if (resourceName.StartsWith("providers/cursor/"))
            return providers.Contains("cursor") ? resourceName["providers/cursor/".Length..] : null;

        if (resourceName.StartsWith("providers/windsurf/"))
            return providers.Contains("windsurf") ? resourceName["providers/windsurf/".Length..] : null;

        if (resourceName.StartsWith("providers/copilot/"))
            return providers.Contains("copilot") ? resourceName["providers/copilot/".Length..] : null;

        if (resourceName.StartsWith("providers/gemini/"))
            return providers.Contains("gemini") ? resourceName["providers/gemini/".Length..] : null;

        if (resourceName.StartsWith("providers/cline/"))
            return providers.Contains("cline") ? resourceName["providers/cline/".Length..] : null;

        if (resourceName.StartsWith("providers/aider/"))
            return providers.Contains("aider") ? resourceName["providers/aider/".Length..] : null;

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
        var profile = Environment.GetEnvironmentVariable("PROFILE")
            ?? Path.Combine(
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
