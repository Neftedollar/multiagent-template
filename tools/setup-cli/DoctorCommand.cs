namespace MultiagentSetup;

public sealed class DoctorCommand(string? workspaceRoot = null, string? homeDir = null, string? forCommand = null)
{
    public async Task<int> ExecuteAsync()
    {
        var cwd  = workspaceRoot ?? Directory.GetCurrentDirectory();
        var home = homeDir       ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Pre-flight mode: targeted checks for a specific command
        if (forCommand is not null)
            return await RunPreflightAsync(cwd, forCommand);

        Console.WriteLine("multiagent-setup doctor");
        Console.WriteLine("=======================");
        Console.WriteLine();

        int errors = 0, warnings = 0;

        // ── Workspace files ───────────────────────────────────────────────────
        Console.WriteLine("Workspace files:");
        errors   += Check(File.Exists(Path.Combine(cwd, "CLAUDE.md")),       "  CLAUDE.md", required: true);
        errors   += Check(File.Exists(Path.Combine(cwd, "docs", "process.md")), "  docs/process.md", required: true);
        warnings += Check(File.Exists(Path.Combine(cwd, "docs", "role-capabilities.md")), "  docs/role-capabilities.md", required: false);
        warnings += Check(Directory.Exists(Path.Combine(cwd, ".claude")),    "  .claude/ directory", required: false);
        Console.WriteLine();

        // ── Agent roles ───────────────────────────────────────────────────────
        Console.WriteLine("Agent roles:");
        var globalCommands = Path.Combine(home, ".claude", "commands");
        var projectCommands = Path.Combine(cwd, ".claude", "commands");
        var globalCount  = Directory.Exists(globalCommands)  ? Directory.GetFiles(globalCommands,  "*.md").Length : 0;
        var projectCount = Directory.Exists(projectCommands) ? Directory.GetFiles(projectCommands, "*.md").Length : 0;
        Console.WriteLine(globalCount > 0
            ? $"  OK   ~/.claude/commands/ — {globalCount} roles"
            : "  WARN ~/.claude/commands/ — no roles (run: multiagent-setup sync-roles --clone)");
        if (globalCount == 0) warnings++;
        if (projectCount > 0) Console.WriteLine($"  OK   .claude/commands/ — {projectCount} project-level roles");
        Console.WriteLine();

        // ── Hook configuration ────────────────────────────────────────────────
        Console.WriteLine("Hook configuration:");
        var settingsPath = Path.Combine(cwd, ".claude", "settings.json");
        if (!File.Exists(settingsPath))
        {
            Console.WriteLine("  WARN .claude/settings.json — not found (hooks disabled)");
            warnings++;
        }
        else
        {
            Console.WriteLine("  OK   .claude/settings.json");
            var settingsContent = await File.ReadAllTextAsync(settingsPath);
            if (settingsContent.Contains("{{HOOK_EXEC}}"))
            {
                Console.WriteLine("  WARN .claude/settings.json — contains unresolved {{HOOK_EXEC}} (recreate workspace)");
                warnings++;
            }
        }
        Console.WriteLine();

        // ── Required CLI tools ────────────────────────────────────────────────
        Console.WriteLine("Required tools:");
        errors   += Check(ProcessHelper.IsOnPath("git"),  "  git");
        warnings += Check(ProcessHelper.IsOnPath("gh"),   "  gh (GitHub CLI)", required: false);
        warnings += Check(ProcessHelper.IsOnPath("dotnet"), "  dotnet", required: false);
        Console.WriteLine();

        // ── Agent CLI (at least one required) ────────────────────────────────
        Console.WriteLine("Agent CLIs (need at least one):");
        string[] agents = ["claude", "nessy", "codex", "qwen-code", "gemini"];
        var foundAgents = agents.Where(ProcessHelper.IsOnPath).ToArray();
        bool hasIdeProviders = Directory.Exists(Path.Combine(cwd, ".cursor", "rules"))
            || Directory.Exists(Path.Combine(cwd, ".windsurf", "rules"))
            || File.Exists(Path.Combine(cwd, ".github", "copilot-instructions.md"))
            || File.Exists(Path.Combine(cwd, ".clinerules"))
            || File.Exists(Path.Combine(cwd, ".aider.conf.yml"))
            || Directory.Exists(Path.Combine(cwd, ".continue"))
            || Directory.Exists(Path.Combine(cwd, ".roo", "rules"));

        if (foundAgents.Length == 0 && !hasIdeProviders)
        {
            Console.WriteLine("  WARN no agent CLI found — install claude, codex, qwen-code, or gemini");
            warnings++;
        }
        foreach (var a in foundAgents)
            Console.WriteLine($"  OK   {a}");
        if (hasIdeProviders)
            Console.WriteLine("  OK   IDE/extension provider (cursor/windsurf/copilot/cline/aider/continue/roo) detected");
        Console.WriteLine();

        // ── Optional infrastructure ───────────────────────────────────────────
        Console.WriteLine("Optional infrastructure:");
        var mcpPath = Path.Combine(cwd, ".claude", "mcp.json");
        if (File.Exists(mcpPath))
        {
            var mcp = await File.ReadAllTextAsync(mcpPath);
            Console.WriteLine(mcp.Contains("age-mcp")    ? "  OK   age-mcp configured"    : "  --   age-mcp not configured");
            Console.WriteLine(mcp.Contains("o-brien")    ? "  OK   o-brien configured"     : "  --   o-brien not configured");
        }
        else
        {
            Console.WriteLine("  --   no .claude/mcp.json (run: multiagent-setup install-mcps)");
        }
        Console.WriteLine();

        // ── Summary ───────────────────────────────────────────────────────────
        if (errors == 0 && warnings == 0)
        {
            Console.WriteLine("All checks passed. Workspace is healthy.");
        }
        else
        {
            if (errors > 0)
                Console.WriteLine($"{errors} error(s) — workspace may not function correctly.");
            if (warnings > 0)
                Console.WriteLine($"{warnings} warning(s) — workspace may have reduced functionality.");
        }
        Console.WriteLine();
        return errors > 0 ? 1 : 0;
    }

    // ── Pre-flight mode ────────────────────────────────────────────────────────

    private static async Task<int> RunPreflightAsync(string cwd, string command)
    {
        Console.WriteLine($"Pre-flight check: multiagent-setup {command}");
        Console.WriteLine();
        int errors = 0;

        switch (command.ToLowerInvariant())
        {
            case "sync-roles":
            {
                errors += Check(ProcessHelper.IsOnPath("git"), "git is on PATH");

                var agencyDir = Environment.GetEnvironmentVariable("AGENCY_DIR")
                    ?? Path.GetFullPath(Path.Combine(cwd, "..", "agency-agents"));

                if (Directory.Exists(Path.Combine(agencyDir, ".git")))
                {
                    Console.WriteLine($"  OK   agency-agents found at {agencyDir}");
                }
                else
                {
                    Console.WriteLine($"  WARN agency-agents not found at {agencyDir}");
                    Console.WriteLine( "       Run: multiagent-setup sync-roles --clone");
                    errors++;
                }

                var hasLocalClaude = Directory.Exists(Path.Combine(cwd, ".claude"));
                if (hasLocalClaude)
                    Console.WriteLine($"  OK   local .claude/ workspace found");
                else
                {
                    Console.WriteLine($"  WARN no local .claude/ workspace — roles will only sync with --global");
                    Console.WriteLine( "       Run: multiagent-setup sync-roles --clone --global");
                }
                break;
            }

            case "init":
            {
                errors += Check(ProcessHelper.IsOnPath("git"), "git is on PATH");
                errors += Check(ProcessHelper.IsOnPath("gh"),  "gh (GitHub CLI) is on PATH");

                var (code, _, _) = await ProcessHelper.RunAsync("git",
                    ["rev-parse", "--git-dir"], workingDir: cwd,
                    captureOutput: true, allowFailure: true);
                if (code == 0)
                    Console.WriteLine($"  OK   {cwd} is a git repository");
                else
                {
                    Console.WriteLine($"  FAIL {cwd} is NOT a git repository");
                    Console.WriteLine( "       Run: git init, then re-run multiagent-setup init");
                    errors++;
                }
                break;
            }

            case "update":
            {
                var hasClaude  = File.Exists(Path.Combine(cwd, "CLAUDE.md"));
                var hasProcess = File.Exists(Path.Combine(cwd, "docs", "process.md"));
                errors += Check(hasClaude,  "CLAUDE.md found (workspace root)");
                errors += Check(hasProcess, "docs/process.md found");
                break;
            }

            default:
                Console.Error.WriteLine($"Unknown command for --for: {command}");
                Console.Error.WriteLine("Supported: sync-roles, init, update");
                return 1;
        }

        Console.WriteLine();
        if (errors == 0)
            Console.WriteLine($"Pre-flight passed — {command} should succeed.");
        else
            Console.WriteLine($"{errors} issue(s) found — fix above before running {command}.");
        Console.WriteLine();
        return errors > 0 ? 1 : 0;
    }

    private static int Check(bool condition, string label, bool required = true)
    {
        if (condition)
        {
            Console.WriteLine($"  OK   {label.TrimStart()}");
            return 0;
        }
        var prefix = required ? "  FAIL" : "  WARN";
        Console.WriteLine($"{prefix} {label.TrimStart()}");
        return 1;
    }
}
