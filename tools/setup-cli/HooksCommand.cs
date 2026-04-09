using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace MultiagentSetup;

public sealed class HooksCommand(string hookName)
{
    public async Task<int> ExecuteAsync()
    {
        var input = await Console.In.ReadToEndAsync();
        return hookName switch
        {
            "block-dangerous"    => BlockDangerous(input),
            "enforce-commit-msg" => EnforceCommitMsg(input),
            "auto-lint"          => await AutoLintAsync(input),
            "log-agent"          => await LogAgentAsync(input),
            "stop-guard"         => await StopGuardAsync(input),
            "research-reminder"  => ResearchReminder(),
            _                    => UnknownHook(hookName)
        };
    }

    // ── block-dangerous ───────────────────────────────────────────────────────

    // isSqlPattern = true: skip when command executable is a text/search tool (grep, gh, git, etc.)
    private static readonly (Regex re, string label, bool isSqlPattern)[] DangerousPatterns =
    [
        (new(@"rm\s+-rf\s+/",                                RegexOptions.IgnoreCase), "rm -rf /",                                           false),
        (new(@"rm\s+-rf\s+\.",                               RegexOptions.IgnoreCase), "rm -rf .",                                           false),
        (new(@"rm\s+-rf\s+\*",                               RegexOptions.IgnoreCase), "rm -rf *",                                           false),
        // Match: git push [--force|-f] [origin] main|master   or   git push origin [--force|-f] main|master
        (new(@"git\s+push\s+(?:(?:--force|-f)\s+(?:origin\s+)?|origin\s+(?:--force|-f)\s+)(main|master)(?:\s|$)", RegexOptions.IgnoreCase), "force push to main/master",              false),
        (new(@"git\s+push\s+(--force|-f)\s*(?:2>|$)",        RegexOptions.IgnoreCase), "force push (no branch — affects tracked branch)",    false),
        (new(@"git\s+reset\s+--hard\s+origin/(main|master)", RegexOptions.IgnoreCase), "git reset --hard origin/main",                       false),
        (new(@"git\s+clean\s+-fd",                           RegexOptions.IgnoreCase), "git clean -fd",                                      false),
        (new(@"DROP\s+(TABLE|DATABASE)",                     RegexOptions.IgnoreCase), "DROP TABLE/DATABASE",                                true),
        (new(@"TRUNCATE\s+TABLE",                            RegexOptions.IgnoreCase), "TRUNCATE TABLE",                                     true),
        (new(@"mkfs\.",                                      RegexOptions.IgnoreCase), "mkfs",                                               false),
        (new(@"dd\s+if=.*of=/dev/",                         RegexOptions.IgnoreCase), "dd to device",                                       false),
        (new(@"chmod\s+-R\s+777\s+/",                       RegexOptions.IgnoreCase), "chmod -R 777 /",                                     false),
        (new(@"chown\s+-R.*\s+/",                           RegexOptions.IgnoreCase), "chown -R /",                                         false),
    ];

    // Text-manipulation tools whose command line may legitimately contain SQL keywords
    // without executing them (e.g. grep patterns, gh pr bodies, git log searches).
    // Patterns with isSqlPattern=true are skipped when the command starts with one of these.
    private static readonly HashSet<string> SqlSafeFirstWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "grep", "rg", "ag", "awk", "sed", "cat", "head", "tail", "less", "more",
        "echo", "printf", "gh", "git", "curl", "wget", "jq",
        "python", "python3", "node", "npx", "ruby", "perl"
    };

    private static int BlockDangerous(string input)
    {
        var command = JsonGet(input, "tool_input", "command");
        if (string.IsNullOrEmpty(command)) return 0;

        // Extract first command word to decide whether SQL patterns apply.
        var firstWord = command.Split([' ', '\t', '\n', '\r'], 2, StringSplitOptions.RemoveEmptyEntries)
                               .FirstOrDefault() ?? "";
        var isSqlSafeContext = SqlSafeFirstWords.Contains(firstWord);

        foreach (var (re, label, isSqlPattern) in DangerousPatterns)
        {
            // Skip SQL-destructive patterns when the command is a text-manipulation or
            // search tool — those may mention SQL keywords without executing them.
            if (isSqlSafeContext && isSqlPattern)
                continue;

            if (re.IsMatch(command))
            {
                WriteDeny("PreToolUse",
                    $"Blocked by safety hook: dangerous command ({label}). " +
                    "If you need this, ask the user to run it manually.");
                return 0;
            }
        }
        return 0;
    }

    // ── enforce-commit-msg ────────────────────────────────────────────────────

    private static readonly Regex ConventionalCommit = new(
        @"^(feat|fix|chore|docs|style|refactor|perf|test|ci|build|revert)(\(.+\))?!?:\s+.+",
        RegexOptions.IgnoreCase);

    private static int EnforceCommitMsg(string input)
    {
        var command = JsonGet(input, "tool_input", "command");
        if (string.IsNullOrEmpty(command)) return 0;

        // Only intercept actual git-commit invocations — not commands that mention
        // "git commit" in their arguments (e.g. gh pr create --body "...git commit...").
        // Check only the first executable token to avoid false positives.
        var tokens = command.TrimStart().Split(new[] { ' ', '\t', '\n', '\r' }, 3, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 2) return 0;
        if (!tokens[0].Equals("git", StringComparison.OrdinalIgnoreCase)) return 0;
        if (!tokens[1].Equals("commit", StringComparison.OrdinalIgnoreCase)) return 0;

        var msg = ExtractCommitMessage(command);
        if (string.IsNullOrEmpty(msg)) return 0;

        var firstLine = msg.Split('\n')[0].Trim();
        if (!ConventionalCommit.IsMatch(firstLine))
            WriteDeny("PreToolUse",
                $"Commit message must follow conventional commits: type(scope)?: description. " +
                $"Types: feat, fix, chore, docs, style, refactor, perf, test, ci, build, revert. " +
                $"Got: '{firstLine}'");
        return 0;
    }

    private static string ExtractCommitMessage(string cmd)
    {
        // Heredoc: git commit -m "$(cat <<'EOF'\nfeat: message\nEOF\n)"
        // Handles the CLAUDE.md-recommended heredoc commit pattern.
        var heredocMatch = Regex.Match(cmd, @"<<'?EOF'?\r?\n([\s\S]*?)\r?\n\s*EOF\b", RegexOptions.Multiline);
        if (heredocMatch.Success) return heredocMatch.Groups[1].Value.Trim();

        var m = Regex.Match(cmd, @"-m\s+""([^""]+)""");
        if (m.Success) return m.Groups[1].Value;
        m = Regex.Match(cmd, @"-m\s+'([^']+)'");
        if (m.Success) return m.Groups[1].Value;
        // Fallback: scan for a conventional-commit type line (handles \n-escaped commands)
        return cmd.Replace("\\n", "\n")
                  .Split('\n')
                  .FirstOrDefault(l => Regex.IsMatch(l.Trim(),
                      @"^(feat|fix|chore|docs|style|refactor|perf|test|ci|build|revert)",
                      RegexOptions.IgnoreCase)) ?? "";
    }

    // ── auto-lint ─────────────────────────────────────────────────────────────

    private static async Task<int> AutoLintAsync(string input)
    {
        var filePath = JsonGet(input, "tool_input", "file_path");
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return 0;

        var ext        = Path.GetExtension(filePath).ToLowerInvariant();
        var projDir    = Environment.GetEnvironmentVariable("CLAUDE_PROJECT_DIR") ?? ".";
        var lintConfig = Path.Combine(projDir, ".claude", "hooks", "lint.json");

        if (File.Exists(lintConfig))
        {
            var cfg = JsonNode.Parse(await File.ReadAllTextAsync(lintConfig));
            var cmd = cfg?["linters"]?[ext]?.GetValue<string>();
            if (!string.IsNullOrEmpty(cmd))
            {
                var parts = cmd.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var args  = parts.Length > 1
                    ? [.. parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries), filePath]
                    : new[] { filePath };
                await ProcessHelper.RunAsync(parts[0], args, allowFailure: true, captureOutput: true);
                return 0;
            }
        }

        switch (ext)
        {
            case ".ts" or ".tsx" or ".js" or ".jsx" or ".css" or ".html" or ".json" or ".md":
                if (HasFile(projDir, ".prettierrc", ".prettierrc.json", "prettier.config.js"))
                    await ProcessHelper.RunAsync("npx", ["prettier", "--write", filePath],
                        allowFailure: true, captureOutput: true);
                else if (HasFile(projDir, ".eslintrc", ".eslintrc.json", "eslint.config.js"))
                    await ProcessHelper.RunAsync("npx", ["eslint", "--fix", filePath],
                        allowFailure: true, captureOutput: true);
                break;
            case ".fs" or ".fsx" or ".fsi":
                if (ProcessHelper.IsOnPath("fantomas"))
                    await ProcessHelper.RunAsync("fantomas", [filePath], allowFailure: true, captureOutput: true);
                else
                    await ProcessHelper.RunAsync("dotnet", ["fantomas", filePath], allowFailure: true, captureOutput: true);
                break;
            case ".py":
                var pyFmt = ProcessHelper.IsOnPath("ruff") ? "ruff" : ProcessHelper.IsOnPath("black") ? "black" : null;
                if (pyFmt == "ruff") await ProcessHelper.RunAsync("ruff",  ["format", filePath], allowFailure: true, captureOutput: true);
                else if (pyFmt == "black") await ProcessHelper.RunAsync("black", ["-q", filePath], allowFailure: true, captureOutput: true);
                break;
            case ".cs":
                await ProcessHelper.RunAsync("dotnet", ["format", "--include", filePath],
                    allowFailure: true, captureOutput: true); break;
            case ".go":
                await ProcessHelper.RunAsync("gofmt",   ["-w",  filePath], allowFailure: true, captureOutput: true); break;
            case ".rs":
                await ProcessHelper.RunAsync("rustfmt", [filePath],        allowFailure: true, captureOutput: true); break;
            case ".rb":
                await ProcessHelper.RunAsync("rubocop", ["-A",  filePath], allowFailure: true, captureOutput: true); break;
            case ".php":
                var pint = Path.Combine(projDir, "vendor", "bin", "pint");
                if (File.Exists(pint))
                    await ProcessHelper.RunAsync(pint,             [filePath],       allowFailure: true, captureOutput: true);
                else
                    await ProcessHelper.RunAsync("php-cs-fixer",   ["fix", filePath], allowFailure: true, captureOutput: true);
                break;
        }
        return 0;
    }

    private static bool HasFile(string dir, params string[] names) =>
        names.Any(n => File.Exists(Path.Combine(dir, n)));

    // ── log-agent ─────────────────────────────────────────────────────────────

    private static async Task<int> LogAgentAsync(string input)
    {
        if (JsonGet(input, "tool_name") != "Agent") return 0;

        var projDir = Environment.GetEnvironmentVariable("CLAUDE_PROJECT_DIR") ?? ".";
        var logDir  = Path.Combine(projDir, ".claude");
        Directory.CreateDirectory(logDir);

        var prompt  = JsonGet(input, "tool_input", "prompt");
        if (prompt.Length > 500) prompt = prompt[..500];

        var entry = new JsonObject
        {
            ["timestamp"]      = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ["session"]        = JsonGetOr(input, "unknown",        "session_id"),
            ["agent_type"]     = JsonGetOr(input, "general-purpose","tool_input", "subagent_type"),
            ["model"]          = JsonGetOr(input, "default",        "tool_input", "model"),
            ["description"]    = JsonGet(input,                     "tool_input", "description"),
            ["prompt_preview"] = prompt
        };

        await File.AppendAllTextAsync(Path.Combine(logDir, "agent-log.jsonl"), entry.ToJsonString() + "\n");
        return 0;
    }

    // ── stop-guard ────────────────────────────────────────────────────────────

    private static async Task<int> StopGuardAsync(string input)
    {
        var doc = JsonNode.Parse(input);
        if (doc?["stop_hook_active"]?.GetValue<bool>() == true) return 0;

        var projDir = Environment.GetEnvironmentVariable("CLAUDE_PROJECT_DIR") ?? ".";
        if (!await HasCodeChangesAsync(projDir)) return 0;

        Console.WriteLine("""
            STOP GUARD: Code files were changed in this session. Before finishing, verify:
            1. Were tests run? If not, run them now.
            2. If this was a pipeline task:
               - Was the result tagged in O'Brien (o-brien MCP store tool)?
               - Was the AGE graph updated (age-mcp)?
            If all done, you may finish.
            """);
        return 0;
    }

    private static async Task<bool> HasCodeChangesAsync(string projDir)
    {
        // Use a session-scoped baseline so pre-session changes don't trigger stop-guard.
        // On first invocation, capture the current HEAD and store it in a temp file.
        // Subsequent invocations compare against that baseline instead of current HEAD.
        var sessionId = Environment.GetEnvironmentVariable("CLAUDE_SESSION_ID")
                        ?? Environment.GetEnvironmentVariable("TERM_SESSION_ID")
                        ?? "default";
        var baselineFile = Path.Combine(Path.GetTempPath(), $"multiagent-stop-baseline-{sessionId}");

        string baseline;
        if (File.Exists(baselineFile))
        {
            baseline = (await File.ReadAllTextAsync(baselineFile)).Trim();
        }
        else
        {
            var (revCode, revOut, _) = await ProcessHelper.RunAsync("git",
                ["-C", projDir, "rev-parse", "HEAD"],
                captureOutput: true, allowFailure: true);
            if (revCode != 0) return false;
            baseline = revOut.Trim();
            await File.WriteAllTextAsync(baselineFile, baseline);
        }

        var (code, diff, _) = await ProcessHelper.RunAsync("git",
            ["-C", projDir, "diff", "--name-only", baseline],
            captureOutput: true, allowFailure: true);
        if (code != 0)
        {
            Console.Error.WriteLine($"  WARN: stop-guard git diff failed (baseline: {baseline[..Math.Min(8, baseline.Length)]}...) — skipping check");
            return false;
        }
        return Regex.IsMatch(diff,
            @"\.(ts|tsx|js|jsx|py|go|rs|rb|php|fs|fsx|cs|java|kt|swift|vue|svelte)$",
            RegexOptions.Multiline);
    }

    private static int UnknownHook(string name)
    {
        Console.Error.WriteLine($"Unknown hook: {name}");
        return 1;
    }

    // ── research-reminder ─────────────────────────────────────────────────────

    private static int ResearchReminder()
    {
        Console.WriteLine(
            "RESEARCH PROTOCOL: save findings to O'Brien (o-brien MCP store) " +
            "and/or AGE graph (age-mcp) with relevant tags before completing task.");
        return 0;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void WriteDeny(string eventName, string reason)
    {
        Console.WriteLine(new JsonObject
        {
            ["hookSpecificOutput"] = new JsonObject
            {
                ["hookEventName"]            = eventName,
                ["permissionDecision"]       = "deny",
                ["permissionDecisionReason"] = reason
            }
        }.ToJsonString());
    }

    private static string JsonGet(string json, params string[] path)
    {
        try
        {
            JsonNode? node = JsonNode.Parse(json);
            foreach (var key in path) node = node?[key];
            return node?.GetValue<string>() ?? "";
        }
        catch { return ""; }
    }

    private static string JsonGetOr(string json, string fallback, params string[] path)
    {
        var v = JsonGet(json, path);
        return string.IsNullOrEmpty(v) ? fallback : v;
    }
}
