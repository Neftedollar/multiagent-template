using System.Text;

namespace MultiagentSetup;

public sealed class SyncRolesCommand(string action, string? agencyDirOverride, bool globalSync = false, string? localSyncDir = null)
{
    private const string AgencyRepo = "https://github.com/msitarzewski/agency-agents.git";
    private const string Marker     = "<!-- auto-generated from agency-agents -->";

    private static readonly string[] SkipFiles =
        ["README.md", "CONTRIBUTING.md", "LICENSE", "PULL_REQUEST_TEMPLATE.md",
         "EXECUTIVE-BRIEF.md", "QUICKSTART.md"];
    private static readonly string[] SkipTopDirs =
        ["strategy", "examples", "integrations", ".github"];

    public async Task<int> ExecuteAsync()
    {
        var agencyDir = agencyDirOverride
            ?? Environment.GetEnvironmentVariable("AGENCY_DIR")
            ?? Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "agency-agents"));

        var globalCommandsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".claude", "commands");

        var currentAction = action;

        // ── Clone ─────────────────────────────────────────────────────────────
        if (currentAction == "--clone")
        {
            if (!Directory.Exists(agencyDir))
            {
                Console.WriteLine("Cloning agency-agents...");
                var (code, _, err) = await ProcessHelper.RunAsync("git",
                    ["clone", AgencyRepo, agencyDir], captureOutput: true, allowFailure: true);
                if (code != 0)
                {
                    Console.Error.WriteLine($"  FAIL: could not clone agency-agents — {err.Trim()}");
                    Console.Error.WriteLine($"        Try: git clone {AgencyRepo} --depth 1 {agencyDir}");
                    Console.Error.WriteLine( "        Or:  multiagent-setup sync-roles --pull  (if already cloned manually)");
                    return 1;
                }
                Console.WriteLine("  OK: agency-agents cloned");
            }
            else
            {
                Console.WriteLine($"  OK: agency-agents already at {agencyDir}");
            }
            currentAction = "--pull";
        }

        // ── Pull ──────────────────────────────────────────────────────────────
        if (currentAction == "--pull" && Directory.Exists(Path.Combine(agencyDir, ".git")))
        {
            Console.WriteLine("Pulling latest roles...");
            var (code, _, _) = await ProcessHelper.RunAsync("git", ["pull", "--ff-only"],
                workingDir: agencyDir, captureOutput: true, allowFailure: true);
            if (code != 0)
            {
                Console.WriteLine($"  WARN: git pull failed — syncing with existing roles in {agencyDir}");
                Console.WriteLine($"        To retry: cd {agencyDir} && git pull --ff-only");
            }
        }

        if (!Directory.Exists(agencyDir))
        {
            Console.Error.WriteLine($"Error: agency-agents not found at {agencyDir}");
            Console.Error.WriteLine("Run: multiagent-setup sync-roles --clone");
            return 1;
        }

        // ── Build role list ────────────────────────────────────────────────────
        var roles = new List<(string CmdName, string Output)>();

        foreach (var roleFile in Directory.GetFiles(agencyDir, "*.md", SearchOption.AllDirectories).Order())
        {
            var basename = Path.GetFileName(roleFile);
            if (SkipFiles.Contains(basename)) continue;

            var relPath = Path.GetRelativePath(agencyDir, roleFile);
            var topDir  = relPath.Split(Path.DirectorySeparatorChar)[0];
            if (SkipTopDirs.Contains(topDir)) continue;

            // Must have frontmatter with name:
            if (!File.ReadLines(roleFile).Take(20).Any(l => l.StartsWith("name:"))) continue;

            var cmdName = Path.GetFileNameWithoutExtension(basename);
            var body    = ExtractAfterFrontmatter(await File.ReadAllTextAsync(roleFile));
            var output  = $$"""
{{Marker}}

Adopt the following expert role for this conversation. Apply this role's full knowledge, methodology, and communication style to the task below.

<role>
{{body}}
</role>

Now, using the expertise above, help with the following:

$ARGUMENTS
""";
            roles.Add((cmdName, output));
        }

        // ── Sync targets ───────────────────────────────────────────────────────
        var cwd = localSyncDir ?? Directory.GetCurrentDirectory();

        // Claude / Nessy — local .claude/commands/ by default; global only with --global
        var localClaudeDir = Path.Combine(cwd, ".claude", "commands");
        var hasLocalClaude = Directory.Exists(Path.GetDirectoryName(localClaudeDir)!); // .claude/ exists

        if (hasLocalClaude)
        {
            var (localCount, localSkipped, localSkippedNames) = await SyncToDirectoryAsync(
                roles, localClaudeDir, projectOverrideDir: localClaudeDir);
            Console.WriteLine();
            Console.WriteLine($"Synced {localCount} roles to {localClaudeDir}");
            if (localSkipped > 0)
            {
                Console.WriteLine($"Skipped {localSkipped} roles (project-level override takes precedence):");
                Console.WriteLine($"  {string.Join(", ", localSkippedNames)}");
            }
        }

        if (globalSync)
        {
            var overrideDir = hasLocalClaude ? localClaudeDir : Path.Combine(cwd, ".claude", "commands");
            var (claudeCount, claudeSkipped, claudeSkippedNames) = await SyncToDirectoryAsync(
                roles, globalCommandsDir, projectOverrideDir: overrideDir);
            Console.WriteLine();
            Console.WriteLine($"Synced {claudeCount} roles to {globalCommandsDir}");
            if (claudeSkipped > 0)
            {
                Console.WriteLine($"Skipped {claudeSkipped} roles (project-level override takes precedence):");
                Console.WriteLine($"  {string.Join(", ", claudeSkippedNames)}");
            }
        }

        if (!hasLocalClaude && !globalSync)
        {
            Console.Error.WriteLine($"  WARN: no local .claude/ workspace found in {cwd}");
            Console.Error.WriteLine($"  Use --global to sync to ~/.claude/commands/");
        }

        // Qwen — workspace-level (auto-detected)
        var qwenDir = Path.Combine(cwd, ".qwen", "commands");
        if (Directory.Exists(Path.GetDirectoryName(qwenDir)!))
        {
            Directory.CreateDirectory(qwenDir);
            var (qCount, qSkipped, qSkippedNames) = await SyncToDirectoryAsync(roles, qwenDir, projectOverrideDir: qwenDir);
            Console.WriteLine($"Synced {qCount} roles to {qwenDir} (qwen)");
            if (qSkipped > 0)
            {
                Console.WriteLine($"Skipped {qSkipped} roles (project-level override takes precedence):");
                Console.WriteLine($"  {string.Join(", ", qSkippedNames)}");
            }
        }

        // Codex — workspace-level (auto-detected)
        var codexDir = Path.Combine(cwd, ".codex", "skills");
        if (Directory.Exists(Path.GetDirectoryName(codexDir)!))
        {
            Directory.CreateDirectory(codexDir);
            var (cCount, cSkipped, cSkippedNames) = await SyncToDirectoryAsync(roles, codexDir, projectOverrideDir: codexDir);
            Console.WriteLine($"Synced {cCount} roles to {codexDir} (codex)");
            if (cSkipped > 0)
            {
                Console.WriteLine($"Skipped {cSkipped} roles (project-level override takes precedence):");
                Console.WriteLine($"  {string.Join(", ", cSkippedNames)}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Check for new roles periodically:");
        Console.WriteLine("  multiagent-setup sync-roles --pull");
        Console.WriteLine("  multiagent-setup sync-roles --pull --global  # also update ~/.claude/commands/");
        return 0;
    }

    private static async Task<(int count, int skipped, List<string> skippedNames)> SyncToDirectoryAsync(
        List<(string CmdName, string Output)> roles,
        string targetDir,
        string? projectOverrideDir)
    {
        Directory.CreateDirectory(targetDir);

        // Remove previously auto-generated files
        foreach (var f in Directory.GetFiles(targetDir, "*.md"))
        {
            var first = File.ReadLines(f).FirstOrDefault() ?? "";
            if (first == Marker) File.Delete(f);
        }

        int count = 0, skipped = 0;
        var skippedNames = new List<string>();

        foreach (var (cmdName, output) in roles)
        {
            // Don't overwrite project-level overrides (only skip when override dir differs from target)
            if (projectOverrideDir is not null && projectOverrideDir != targetDir)
            {
                var projectCmd = Path.Combine(projectOverrideDir, $"{cmdName}.md");
                if (File.Exists(projectCmd)) { skipped++; skippedNames.Add(cmdName); continue; }
            }

            await File.WriteAllTextAsync(
                Path.Combine(targetDir, $"{cmdName}.md"),
                output,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            count++;
        }

        return (count, skipped, skippedNames);
    }

    private static string ExtractAfterFrontmatter(string content)
    {
        var lines  = content.Split('\n');
        int dashes = 0;
        var result = new List<string>();
        foreach (var line in lines)
        {
            if (line.TrimEnd('\r') == "---") { dashes++; continue; }
            if (dashes >= 2) result.Add(line);
        }
        return string.Join('\n', result).Trim();
    }
}
