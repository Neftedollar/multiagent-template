using System.Text;

namespace MultiagentSetup;

public sealed class SyncRolesCommand(string action, string? agencyDirOverride, string provider = "claude")
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

        var (commandsDir, markerPrefix) = provider.ToLowerInvariant() switch
        {
            "codex" => (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex", "skills"), ""),
            "qwen"  => (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".qwen", "commands"), ""),
            _       => (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", "commands"), Marker + "\n\n")
        };

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
                    Console.Error.WriteLine($"  WARN: could not clone agency-agents — {err.Trim()}");
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
            if (code != 0) Console.WriteLine("  WARN: git pull failed, using existing");
        }

        if (!Directory.Exists(agencyDir))
        {
            Console.Error.WriteLine($"Error: agency-agents not found at {agencyDir}");
            Console.Error.WriteLine("Run: multiagent-setup sync-roles --clone");
            return 1;
        }

        // ── Sync ──────────────────────────────────────────────────────────────
        Directory.CreateDirectory(commandsDir);

        // Remove previously auto-generated files
        foreach (var f in Directory.GetFiles(commandsDir, "*.md"))
        {
            var first = File.ReadLines(f).FirstOrDefault() ?? "";
            if (first == Marker || (provider != "claude" && first.StartsWith("Adopt the following"))) File.Delete(f);
        }

        int count = 0, skipped = 0;

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

            // Don't overwrite project-level commands
            var projectCmdDir = provider.ToLowerInvariant() switch
            {
                "codex" => Path.Combine(Directory.GetCurrentDirectory(), ".codex", "skills"),
                "qwen"  => Path.Combine(Directory.GetCurrentDirectory(), ".qwen", "commands"),
                _       => Path.Combine(Directory.GetCurrentDirectory(), ".claude", "commands")
            };
            var projectCmd = Path.Combine(projectCmdDir, $"{cmdName}.md");
            if (File.Exists(projectCmd)) { skipped++; continue; }

            var body = ExtractAfterFrontmatter(await File.ReadAllTextAsync(roleFile));
            var output = provider.ToLowerInvariant() switch
            {
                "codex" => $$"""
{{body}}

Task: $ARGUMENTS
""",
                "qwen" => $$"""
{{body}}

Task: $ARGUMENTS
""",
                _ => $$"""
{{Marker}}

Adopt the following expert role for this conversation. Apply this role's full knowledge, methodology, and communication style to the task below.

<role>
{{body}}
</role>

Now, using the expertise above, help with the following:

$ARGUMENTS
"""
            };

            await File.WriteAllTextAsync(
                Path.Combine(commandsDir, $"{cmdName}.md"),
                output,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            count++;
        }

        Console.WriteLine();
        Console.WriteLine($"Synced {count} roles to {commandsDir}");
        if (skipped > 0) Console.WriteLine($"Skipped {skipped} (project-level override exists)");
        Console.WriteLine();
        Console.WriteLine("Check for new roles periodically:");
        Console.WriteLine("  multiagent-setup sync-roles --pull");
        return 0;
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
