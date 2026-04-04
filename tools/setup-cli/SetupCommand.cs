using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace MultiagentSetup;

public sealed class SetupCommand(string projectName, string? requestedOrg)
{
    public async Task<int> ExecuteAsync()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
        Console.WriteLine($"\nmultiagent-setup v{version}");
        Console.WriteLine($"  Project: {projectName}");
        Console.WriteLine();

        Console.WriteLine("Pre-flight checks...");
        Console.WriteLine();
        if (!CheckTools()) return 1;

        var org = await ResolveOrgAsync();
        if (org is null) return 1;

        var targetDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), projectName));
        if (Directory.Exists(targetDir))
        {
            Console.Error.WriteLine($"Error: {targetDir} already exists");
            return 1;
        }

        var graphName = $"{projectName.ToLower()}-ops";

        Console.WriteLine($"Creating workspace...");
        Console.WriteLine($"  Project:    {projectName}");
        Console.WriteLine($"  GitHub org: {org}");
        Console.WriteLine($"  Graph:      {graphName}");
        Console.WriteLine($"  Target:     {targetDir}");
        Console.WriteLine();

        CreateDirectories(targetDir);

        var vars = BuildVars(projectName, org, graphName);
        ExtractTemplates(targetDir, vars);
        Console.WriteLine("  OK: templates extracted");

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            await ChmodShellScriptsAsync(targetDir);
            Console.WriteLine("  OK: permissions set");
        }

        await SetupAgencyRolesAsync(targetDir);
        await GitInitAsync(targetDir);
        Console.WriteLine("  OK: git initialized");

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            await OfferCompletionsAsync(targetDir);

        Console.WriteLine();
        Console.WriteLine($"Done! Workspace created at: {targetDir}");
        Console.WriteLine();
        Console.WriteLine("Next steps:");
        Console.WriteLine($"  1. cd {targetDir}");
        Console.WriteLine($"  2. Clone your code repo into code/{projectName}");
        var mcpScript = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? @".\tools\install-mcps.ps1"
            : "./tools/install-mcps.sh";
        Console.WriteLine($"  3. (Optional) Install MCPs: {mcpScript}");
        Console.WriteLine($"  4. Start working: claude then /orchestrator <task>");
        Console.WriteLine();
        return 0;
    }

    // ── Pre-flight ────────────────────────────────────────────────────────────

    private static bool CheckTools()
    {
        var ok = true;
        ok &= Require("git");
        ok &= Require("jq");
        ok &= Require("gh");
        Suggest("claude", "https://docs.anthropic.com/en/docs/claude-code");
        Console.WriteLine();
        return ok;
    }

    private static bool Require(string name)
    {
        if (IsOnPath(name)) { Console.WriteLine($"  OK: {name}"); return true; }
        Console.Error.WriteLine($"  FAIL: {name} not found — install it and re-run");
        return false;
    }

    private static void Suggest(string name, string installUrl)
    {
        if (IsOnPath(name)) Console.WriteLine($"  OK: {name}");
        else Console.WriteLine($"  WARN: {name} not found — install: {installUrl}");
    }

    private static bool IsOnPath(string name)
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
        var sep = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';
        string[] exts = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? [".exe", ".cmd", ".bat", ""] : [""];
        return pathVar.Split(sep).Any(dir =>
            exts.Any(ext => File.Exists(Path.Combine(dir, name + ext))));
    }

    // ── GitHub org resolution ─────────────────────────────────────────────────

    private async Task<string?> ResolveOrgAsync()
    {
        if (requestedOrg is not null) return requestedOrg;

        var (authCode, _, _) = await RunAsync("gh", ["auth", "status"],
            captureOutput: true, allowFailure: true);

        if (authCode != 0)
        {
            Console.WriteLine("GitHub CLI not authenticated. Launching login...");
            var loginCode = await RunInteractiveAsync("gh", ["auth", "login"]);
            if (loginCode != 0)
            {
                Console.Error.WriteLine("FAIL: gh auth login failed");
                return null;
            }
        }

        var (code, stdout, _) = await RunAsync("gh", ["api", "user", "--jq", ".login"],
            captureOutput: true);
        if (code != 0 || string.IsNullOrWhiteSpace(stdout))
        {
            Console.Error.WriteLine("FAIL: could not resolve GitHub username");
            return null;
        }
        return stdout.Trim();
    }

    // ── Directory creation ────────────────────────────────────────────────────

    private static void CreateDirectories(string root)
    {
        string[] dirs =
        [
            "code",
            "docs/workflows", "docs/archive", "docs/obsolete-docs",
            ".claude/commands", ".claude/hooks",
            "tools",
        ];
        foreach (var d in dirs)
            Directory.CreateDirectory(Path.Combine(root, d.Replace('/', Path.DirectorySeparatorChar)));
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
    };

    private static void ExtractTemplates(string root, Dictionary<string, string> vars)
    {
        var asm = Assembly.GetExecutingAssembly();
        foreach (var resourceName in asm.GetManifestResourceNames())
        {
            // LogicalName uses "/" separators; convert to OS separator for file path
            var relPath = resourceName.Replace('/', Path.DirectorySeparatorChar);
            var outputPath = Path.Combine(root, relPath);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            using var stream = asm.GetManifestResourceStream(resourceName)!;

            if (IsTextResource(resourceName))
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

    private static bool IsTextResource(string name) =>
        name.EndsWith(".md")   ||
        name.EndsWith(".json") ||
        name.EndsWith(".sh")   ||
        name.EndsWith(".zsh")  ||
        name.EndsWith(".ps1");

    // ── Permissions ───────────────────────────────────────────────────────────

    private static async Task ChmodShellScriptsAsync(string root)
    {
        var scripts = Directory.GetFiles(root, "*.sh", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(root, "*.zsh", SearchOption.AllDirectories));
        foreach (var s in scripts)
            await RunAsync("chmod", ["+x", s], allowFailure: true);
    }

    // ── Agency-agents + role sync ─────────────────────────────────────────────

    private static async Task SetupAgencyRolesAsync(string workspaceRoot)
    {
        var agencyDir = Path.GetFullPath(Path.Combine(workspaceRoot, "..", "agency-agents"));

        if (!Directory.Exists(agencyDir))
        {
            Console.WriteLine("Cloning agency-agents...");
            var (code, _, err) = await RunAsync("git",
                ["clone", "https://github.com/msitarzewski/agency-agents.git", agencyDir],
                captureOutput: true, allowFailure: true);
            if (code != 0)
            {
                Console.WriteLine($"  WARN: could not clone agency-agents — {err.Trim()}");
                return;
            }
            Console.WriteLine("  OK: agency-agents cloned");
        }
        else
        {
            Console.WriteLine("Updating agency-agents...");
            await RunAsync("git", ["pull", "--ff-only"],
                workingDir: agencyDir, captureOutput: true, allowFailure: true);
            Console.WriteLine("  OK: agency-agents updated");
        }

        Console.WriteLine("Syncing roles...");
        var env = new Dictionary<string, string> { ["AGENCY_DIR"] = agencyDir };
        var (syncCode, _, syncErr) = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? await RunAsync("pwsh", [Path.Combine(workspaceRoot, "tools", "sync-roles.ps1")],
                workingDir: workspaceRoot, env: env, captureOutput: true, allowFailure: true)
            : await RunAsync("bash", [Path.Combine(workspaceRoot, "tools", "sync-roles.sh")],
                workingDir: workspaceRoot, env: env, captureOutput: true, allowFailure: true);
        Console.WriteLine(syncCode == 0
            ? "  OK: roles synced to ~/.claude/commands/"
            : $"  WARN: sync-roles failed — {syncErr.Trim()}");
    }

    // ── Git init ──────────────────────────────────────────────────────────────

    private static async Task GitInitAsync(string root)
    {
        Console.WriteLine("Initializing git...");
        await RunAsync("git", ["init", "-q"], workingDir: root, captureOutput: true);
        await File.WriteAllTextAsync(
            Path.Combine(root, ".gitignore"),
            "code/\n*.png\n.DS_Store\n.claude/settings.local.json\n");
        await RunAsync("git", ["add", "-A"], workingDir: root, captureOutput: true);
        await RunAsync("git",
            ["commit", "-q", "-m", "init: multi-agent workspace from template"],
            workingDir: root, captureOutput: true);
    }

    // ── Zsh completions ───────────────────────────────────────────────────────

    private static async Task OfferCompletionsAsync(string workspaceRoot)
    {
        var zshrc = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".zshrc");
        if (!File.Exists(zshrc)) return;

        var existing = await File.ReadAllTextAsync(zshrc);
        if (existing.Contains("completions.zsh")) return;

        if (Console.IsInputRedirected) return;

        Console.Write("\nAdd zsh completions to ~/.zshrc? [y/N] ");
        var key = Console.ReadKey(intercept: false);
        Console.WriteLine();

        if (key.KeyChar is 'y' or 'Y')
        {
            var completionsPath = Path.Combine(workspaceRoot, "tools", "completions.zsh");
            await File.AppendAllTextAsync(zshrc,
                $"\n# Multi-agent workspace completions\nsource \"{completionsPath}\"\n");
            Console.WriteLine("  OK: completions added (restart shell or: source ~/.zshrc)");
        }
    }

    // ── Process helpers ───────────────────────────────────────────────────────

    private static async Task<(int exitCode, string stdout, string stderr)> RunAsync(
        string exe, string[] args,
        string? workingDir = null,
        Dictionary<string, string>? env = null,
        bool captureOutput = false,
        bool allowFailure = false)
    {
        var psi = new ProcessStartInfo(exe)
        {
            WorkingDirectory     = workingDir ?? "",
            RedirectStandardOutput = captureOutput,
            RedirectStandardError  = captureOutput,
            UseShellExecute      = false,
        };

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        if (env is not null)
            foreach (var (k, v) in env)
                psi.Environment[k] = v;

        Process? proc;
        try { proc = Process.Start(psi); }
        catch (Exception ex)
        {
            if (!allowFailure) Console.Error.WriteLine($"FAIL: could not start {exe}: {ex.Message}");
            return (1, "", ex.Message);
        }

        if (proc is null) return (1, "", $"Failed to start {exe}");

        var stdoutTask = captureOutput ? proc.StandardOutput.ReadToEndAsync() : Task.FromResult("");
        var stderrTask = captureOutput ? proc.StandardError.ReadToEndAsync()  : Task.FromResult("");

        await proc.WaitForExitAsync();
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return (proc.ExitCode, stdout, stderr);
    }

    private static async Task<int> RunInteractiveAsync(string exe, string[] args, string? workingDir = null)
    {
        var psi = new ProcessStartInfo(exe)
        {
            WorkingDirectory = workingDir ?? "",
            UseShellExecute  = false,
        };
        foreach (var arg in args) psi.ArgumentList.Add(arg);

        Process? proc;
        try { proc = Process.Start(psi); }
        catch { return 1; }
        if (proc is null) return 1;

        await proc.WaitForExitAsync();
        return proc.ExitCode;
    }
}
