using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace MultiagentSetup;

public sealed class InitCommand(string targetDir, string? requestedOrg, string provider = "claude", bool force = false)
{
    // Resource names for provider-specific workspace instruction files
    // (in init mode, the CLAUDE.md template is repurposed as the workspace instructions base)
    private static readonly HashSet<string> ProviderWorkspaceInstructionResources = new(StringComparer.Ordinal)
    {
        "providers/codex/AGENTS.md",
        "providers/gemini/GEMINI.md",
        "providers/qwen/QWEN.md",
        "providers/aider/AIDER.md",
        "providers/continue/CONTINUE.md",
        "providers/nessy/NESSY.md",
    };

    public async Task<int> ExecuteAsync()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
        Console.WriteLine($"\nmultiagent-setup v{version}");
        Console.WriteLine($"  Mode:     init (add workspace files to existing repo)");
        Console.WriteLine($"  Target:   {targetDir}");
        Console.WriteLine($"  Provider: {provider}");
        Console.WriteLine();

        // Verify this is an existing git repo
        var (gitCheckCode, _, _) = await ProcessHelper.RunAsync(
            "git", ["rev-parse", "--git-dir"], workingDir: targetDir,
            captureOutput: true, allowFailure: true);
        if (gitCheckCode != 0)
        {
            Console.Error.WriteLine($"Error: {targetDir} is not a git repository.");
            Console.Error.WriteLine("       Run `git init` first, then re-run `multiagent-setup init`.");
            Console.Error.WriteLine("       Or use `multiagent-setup new <name>` to create a new workspace from scratch.");
            return 1;
        }

        Console.WriteLine("Pre-flight checks...");
        Console.WriteLine();
        if (!CheckTools(provider)) return 1;

        // Infer project name and org from git remote
        var (inferredProject, inferredOrg) = await InferFromGitRemoteAsync(targetDir);

        var projectName = inferredProject ?? Path.GetFileName(targetDir.TrimEnd('/', '\\'));
        var org = requestedOrg ?? inferredOrg ?? await ResolveOrgViaGhAsync();
        if (org is null) return 1;

        var graphName = $"{projectName.ToLower()}-ops";

        Console.WriteLine("Adding workspace files...");
        Console.WriteLine($"  Project:    {projectName}");
        Console.WriteLine($"  GitHub org: {org}");
        Console.WriteLine($"  Graph:      {graphName}");
        Console.WriteLine($"  Target:     {targetDir}");
        Console.WriteLine($"  Force:      {force}");
        Console.WriteLine();

        var providers = provider == "all"
            ? ProviderRegistry.AllExpansion.ToArray()
            : new[] { provider };

        // Create provider-specific directories (but NOT code/)
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

        // git add -A + commit (no git init — repo already exists)
        await GitCommitAsync(targetDir);
        Console.WriteLine("  OK: changes committed");

        Console.WriteLine();
        Console.WriteLine($"Done! Workspace injected into: {targetDir}");
        Console.WriteLine();
        Console.WriteLine("Next steps:");
        Console.WriteLine($"  1. (Optional) Install MCPs: multiagent-setup install-mcps");
        if (providers.Any(p => p is "claude" or "nessy"))
            Console.WriteLine($"  2. (Optional) Update roles: multiagent-setup sync-roles --pull");
        Console.WriteLine($"  3. Start working:");
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

    // ── Git remote inference ──────────────────────────────────────────────────

    /// <summary>
    /// Attempts to parse project name and org from the git remote URL.
    /// Supports HTTPS (https://github.com/org/repo.git) and SSH (git@github.com:org/repo.git) formats.
    /// </summary>
    private static async Task<(string? project, string? org)> InferFromGitRemoteAsync(string repoDir)
    {
        var (code, stdout, _) = await ProcessHelper.RunAsync(
            "git", ["remote", "get-url", "origin"],
            workingDir: repoDir, captureOutput: true, allowFailure: true);

        if (code != 0 || string.IsNullOrWhiteSpace(stdout))
            return (null, null);

        var url = stdout.Trim();

        // HTTPS: https://github.com/org/repo.git  or  https://github.com/org/repo
        // SSH:   git@github.com:org/repo.git
        string? orgPart = null;
        string? repoPart = null;

        if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            // Strip protocol + host, take last two path segments
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Trim('/').Split('/');
            if (segments.Length >= 2)
            {
                orgPart  = segments[^2];
                repoPart = segments[^1];
            }
        }
        else if (url.StartsWith("git@", StringComparison.Ordinal))
        {
            // git@github.com:org/repo.git
            var colonIdx = url.IndexOf(':');
            if (colonIdx >= 0)
            {
                var path = url[(colonIdx + 1)..];
                var segments = path.Trim('/').Split('/');
                if (segments.Length >= 2)
                {
                    orgPart  = segments[^2];
                    repoPart = segments[^1];
                }
            }
        }

        // Strip .git suffix
        if (repoPart is not null && repoPart.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            repoPart = repoPart[..^4];

        return (repoPart, orgPart);
    }

    // ── GitHub org resolution ─────────────────────────────────────────────────

    private async Task<string?> ResolveOrgViaGhAsync()
    {
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
        // Shared workspace dirs (no code/ — that IS the repo)
        foreach (var d in new[] { "docs/workflows", "docs/archive", "docs/obsolete-docs", "tools" })
            Directory.CreateDirectory(Path.Combine(root, d.Replace('/', Path.DirectorySeparatorChar)));

        // Provider-specific dirs
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

    private void ExtractTemplates(string root, Dictionary<string, string> vars, string[] providers)
    {
        var asm = Assembly.GetExecutingAssembly();
        foreach (var resourceName in asm.GetManifestResourceNames())
        {
            var outputRel = ResolveOutputPathForInit(resourceName, providers);
            if (outputRel is null) continue;

            var outputPath = Path.Combine(root, outputRel.Replace('/', Path.DirectorySeparatorChar));

            // Skip existing files unless --force is set
            if (!force && File.Exists(outputPath))
            {
                Console.WriteLine($"  SKIP (exists): {outputRel}");
                continue;
            }

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

    private string? ResolveOutputPathForInit(string resourceName, string[] providers)
    {
        // CLAUDE.md template → write as the provider's WorkspaceInstructionsFile (if any)
        if (resourceName == "CLAUDE.md")
        {
            // For --provider all: write CLAUDE.md for claude (the base template)
            // Individual provider instruction files are handled by their own resources below
            if (providers.Contains("claude"))
                return "CLAUDE.md";
            // If no claude in providers but another provider that has WorkspaceInstructionsFile == "CLAUDE.md"
            // would need it — but only claude uses CLAUDE.md, so skip for others
            return null;
        }

        // Provider-specific workspace instruction files (e.g. providers/nessy/NESSY.md)
        // These get promoted to root level based on each provider's WorkspaceInstructionsFile
        foreach (var def in ProviderRegistry.All)
        {
            if (def.WorkspaceInstructionsFile is null) continue;
            if (def.TemplatePrefix is null) continue;  // nessy: handled via its own resources/prefix
            var expectedResource = $"{def.TemplatePrefix}{def.WorkspaceInstructionsFile}";
            if (resourceName == expectedResource)
                return providers.Contains(def.Name) ? def.WorkspaceInstructionsFile : null;
        }

        // nessy/NESSY.md — special case: TemplatePrefix is null but we have a dedicated resource
        if (resourceName == "providers/nessy/NESSY.md")
            return providers.Contains("nessy") ? "NESSY.md" : null;

        // Skip the "old-style" provider workspace instruction resources that are being
        // replaced by the workspace instructions file above (avoid double-writing).
        // These are the ones listed in ProviderWorkspaceInstructionResources that don't
        // start with a known TemplatePrefix that matches a provider — already handled above.
        if (ProviderWorkspaceInstructionResources.Contains(resourceName))
            return null;

        // .claude/ resources — active for claude or nessy
        if (resourceName.StartsWith(".claude/"))
            return (providers.Contains("claude") || providers.Contains("nessy")) ? resourceName : null;

        // .github/workflows/ — scaffolded for claude/nessy workspaces
        if (resourceName.StartsWith(".github/workflows/"))
            return (providers.Contains("claude") || providers.Contains("nessy")) ? resourceName : null;

        // Provider-prefixed templates (registry-driven)
        foreach (var def in ProviderRegistry.All)
        {
            if (def.TemplatePrefix is null) continue;
            if (resourceName.StartsWith(def.TemplatePrefix))
                return providers.Contains(def.Name)
                    ? resourceName[def.TemplatePrefix.Length..]
                    : null;
        }

        // docs/*, tools/* — always write (subject to --force check above)
        if (resourceName.StartsWith("docs/") || resourceName.StartsWith("tools/"))
            return resourceName;

        // Everything else — skip (don't create code/ directory, etc.)
        return null;
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
        await new SyncRolesCommand("--clone", agencyDir, globalSync: false, localSyncDir: workspaceRoot).ExecuteAsync();
    }

    // ── Git commit ────────────────────────────────────────────────────────────

    private static async Task GitCommitAsync(string root)
    {
        Console.WriteLine("Committing changes...");

        var (addCode, _, addErr) = await ProcessHelper.RunAsync(
            "git", ["add", "-A"], workingDir: root, captureOutput: true);
        if (addCode != 0) throw new InvalidOperationException($"git add failed: {addErr}");

        // Check if there is anything to commit
        var (statusCode, statusOut, _) = await ProcessHelper.RunAsync(
            "git", ["status", "--porcelain"], workingDir: root, captureOutput: true);
        if (statusCode == 0 && string.IsNullOrWhiteSpace(statusOut))
        {
            Console.WriteLine("  INFO: nothing to commit (all files already existed)");
            return;
        }

        var (commitCode, _, commitErr) = await ProcessHelper.RunAsync(
            "git", ["-c", "commit.gpgsign=false", "commit", "-q", "-m", "chore: add multiagent workspace setup"],
            workingDir: root, captureOutput: true);
        if (commitCode != 0) throw new InvalidOperationException($"git commit failed: {commitErr}");
    }
}
