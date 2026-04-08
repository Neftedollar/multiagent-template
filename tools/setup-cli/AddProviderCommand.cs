using System.Runtime.InteropServices;

namespace MultiagentSetup;

public sealed class AddProviderCommand(string provider, string? workspaceDir = null, bool force = false)
{
    private static readonly string[] AllProviders = ["claude", "nessy", "gemini", "codex", "qwen"];

    public async Task<int> ExecuteAsync()
    {
        var wsRoot = Path.GetFullPath(workspaceDir ?? Directory.GetCurrentDirectory());

        if (!Directory.Exists(wsRoot))
        {
            Console.Error.WriteLine($"Error: directory not found: {wsRoot}");
            return 1;
        }

        if (!IsWorkspace(wsRoot))
            Console.WriteLine("  WARN: directory doesn't look like a multiagent workspace (no CLAUDE.md or .claude/)");

        var providers = provider == "all" ? AllProviders : new[] { provider };
        var installed = DetectInstalled(wsRoot);
        var toAdd     = force ? providers : providers.Where(p => !installed.Contains(p)).ToArray();
        var skipped   = providers.Except(toAdd).ToArray();

        if (skipped.Length > 0)
            Console.WriteLine($"  Already configured: {string.Join(", ", skipped)}" +
                              (force ? "" : " — use --force to overwrite"));

        if (toAdd.Length == 0)
        {
            Console.WriteLine("Nothing to add.");
            return 0;
        }

        Console.WriteLine($"Adding: {string.Join(", ", toAdd)}");
        Console.WriteLine();

        // Infer project name from workspace folder name
        var projectName = Path.GetFileName(wsRoot);
        var graphName   = $"{projectName.ToLower()}-ops";

        // Resolve org (best-effort — used only in template substitution)
        var (_, orgOut, _) = await ProcessHelper.RunAsync("gh", ["api", "user", "--jq", ".login"],
            captureOutput: true, allowFailure: true);
        var org = string.IsNullOrWhiteSpace(orgOut) ? projectName : orgOut.Trim();

        SetupCommand.CreateDirectories(wsRoot, toAdd);

        var vars = SetupCommand.BuildVars(projectName, org, graphName);
        SetupCommand.ExtractTemplates(wsRoot, vars, toAdd, skipExisting: !force);
        Console.WriteLine("  OK: templates extracted");

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            await SetupCommand.ChmodShellScriptsAsync(wsRoot);
            Console.WriteLine("  OK: permissions set");
        }

        if (toAdd.Any(p => p is "claude" or "nessy"))
            await SetupCommand.SetupAgencyRolesAsync(wsRoot);

        Console.WriteLine();
        Console.WriteLine($"Done! Added {string.Join(", ", toAdd)} to {wsRoot}");
        Console.WriteLine();

        foreach (var p in toAdd)
        {
            var cmd = p switch
            {
                "claude" or "nessy" => p,
                "gemini"            => "gemini",
                "codex"             => "codex",
                "qwen"              => "qwen-code",
                _                   => p
            };
            Console.WriteLine($"  Start {p}: {cmd}  →  /orchestrator <task>");
        }
        Console.WriteLine();
        return 0;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsWorkspace(string root) =>
        File.Exists(Path.Combine(root, "CLAUDE.md"))  ||
        Directory.Exists(Path.Combine(root, ".claude")) ||
        File.Exists(Path.Combine(root, "GEMINI.md"))  ||
        Directory.Exists(Path.Combine(root, ".gemini"));

    private static HashSet<string> DetectInstalled(string root)
    {
        var result = new HashSet<string>();
        if (Directory.Exists(Path.Combine(root, ".claude"))) { result.Add("claude"); result.Add("nessy"); }
        if (Directory.Exists(Path.Combine(root, ".gemini"))) result.Add("gemini");
        if (Directory.Exists(Path.Combine(root, ".codex")))  result.Add("codex");
        if (Directory.Exists(Path.Combine(root, ".qwen")))   result.Add("qwen");
        return result;
    }
}
