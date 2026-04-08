using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace MultiagentSetup;

/// <summary>
/// Updates an existing multiagent workspace with the latest templates.
/// Shared operational files (docs/, tools/) are always candidates for update.
/// Provider files are re-extracted for every provider detected in the workspace.
/// Existing files are preserved by default; use --force to overwrite.
/// </summary>
public sealed class UpdateCommand(bool force = false)
{
    // Resources that should be updated but are not provider-specific.
    // CLAUDE.md and context files (GEMINI.md etc.) are intentionally excluded —
    // users customise these and we don't want to clobber their edits.
    private static readonly string[] SharedUpdatePrefixes =
    [
        "docs/",
        "tools/",
        ".claude/hooks/",
        ".claude/mcp.json",
        ".claude/commands/orchestrator.md",
    ];

    public async Task<int> ExecuteAsync()
    {
        var cwd = Directory.GetCurrentDirectory();

        if (!File.Exists(Path.Combine(cwd, "CLAUDE.md")) ||
            !File.Exists(Path.Combine(cwd, "docs", "process.md")))
        {
            Console.Error.WriteLine("Error: not in a multiagent workspace (CLAUDE.md or docs/process.md not found)");
            Console.Error.WriteLine("Run this command from the workspace root directory.");
            return 1;
        }

        var projectName = Path.GetFileName(cwd);
        var providers   = DetectProviders(cwd);
        var vars        = BuildVars(cwd, projectName);

        Console.WriteLine($"\nUpdating workspace: {cwd}");
        Console.WriteLine($"Detected providers: {(providers.Length > 0 ? string.Join(", ", providers) : "claude (default)")}");
        Console.WriteLine($"Mode: {(force ? "overwrite (--force)" : "skip existing")}");
        Console.WriteLine();

        ExtractResources(cwd, vars, providers);

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var scripts = Directory.GetFiles(cwd, "*.sh", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(cwd, "*.zsh", SearchOption.AllDirectories));
            foreach (var s in scripts)
                await ProcessHelper.RunAsync("chmod", ["+x", s], allowFailure: true);
        }

        Console.WriteLine();
        Console.WriteLine("Workspace updated.");
        if (!force)
            Console.WriteLine("Tip: use --force to overwrite all existing files.");
        Console.WriteLine();
        return 0;
    }

    private static string[] DetectProviders(string root)
    {
        var found = new List<string>();

        if (Directory.Exists(Path.Combine(root, ".claude")))
            found.Add("claude");

        if (Directory.Exists(Path.Combine(root, ".codex")))
            found.Add("codex");

        if (Directory.Exists(Path.Combine(root, ".qwen")))
            found.Add("qwen");

        if (Directory.Exists(Path.Combine(root, ".cursor", "rules")))
            found.Add("cursor");

        if (Directory.Exists(Path.Combine(root, ".windsurf", "rules")))
            found.Add("windsurf");

        if (File.Exists(Path.Combine(root, ".github", "copilot-instructions.md")))
            found.Add("copilot");

        if (Directory.Exists(Path.Combine(root, ".gemini")))
            found.Add("gemini");

        if (File.Exists(Path.Combine(root, ".clinerules")))
            found.Add("cline");

        if (File.Exists(Path.Combine(root, ".aider.conf.yml")))
            found.Add("aider");

        if (Directory.Exists(Path.Combine(root, ".continue")))
            found.Add("continue");

        if (Directory.Exists(Path.Combine(root, ".roo", "rules")))
            found.Add("roo");

        return [.. found];
    }

    private static Dictionary<string, string> BuildVars(string root, string projectName)
    {
        var claudeMd = Path.Combine(root, "CLAUDE.md");
        var existing = File.Exists(claudeMd) ? File.ReadAllText(claudeMd) : "";

        var orgMatch  = Regex.Match(existing, @"GitHub Project in org `([^`]+)`");
        var repoMatch = Regex.Match(existing, @"Issues in `[^/]+/([^`]+)`");

        var githubOrg  = orgMatch.Success  ? orgMatch.Groups[1].Value  : Environment.UserName;
        var githubRepo = repoMatch.Success ? repoMatch.Groups[1].Value : projectName;

        var graphName = $"{projectName.ToLower()}-ops";
        return new()
        {
            ["{{PROJECT_NAME}}"]        = projectName,
            ["{{PROJECT_DESCRIPTION}}"] = $"{projectName} project workspace",
            ["{{FOUNDER}}"]             = Environment.UserName,
            ["{{PHASE}}"]               = "early development",
            ["{{GITHUB_ORG}}"]          = githubOrg,
            ["{{GITHUB_REPO}}"]         = githubRepo,
            ["{{GRAPH_NAME}}"]          = graphName,
            ["{{DATE}}"]                = DateTime.Today.ToString("yyyy-MM-dd"),
            ["{{HOOK_EXEC}}"]           = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                                            ? @"$env:USERPROFILE\.dotnet\tools\multiagent-setup.exe"
                                            : "$HOME/.dotnet/tools/multiagent-setup",
        };
    }

    private void ExtractResources(string root, Dictionary<string, string> vars, string[] providers)
    {
        var asm = Assembly.GetExecutingAssembly();

        foreach (var resourceName in asm.GetManifestResourceNames())
        {
            var outputRel = ResolveOutputPath(resourceName, providers);
            if (outputRel is null) continue;

            var outputPath = Path.Combine(root, outputRel.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(outputPath) && !force)
            {
                Console.WriteLine($"  SKIP: {outputRel}");
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
                Console.WriteLine($"  OK:   {outputRel}");
            }
            else
            {
                using var file = File.Create(outputPath);
                stream.CopyTo(file);
                Console.WriteLine($"  OK:   {outputRel}");
            }
        }
    }

    private static string? ResolveOutputPath(string resourceName, string[] providers)
    {
        // Shared operational files — always update regardless of provider
        if (SharedUpdatePrefixes.Any(p => resourceName == p || resourceName.StartsWith(p)))
        {
            // Only extract .claude/ shared files when claude is a detected provider
            if (resourceName.StartsWith(".claude/") && !providers.Contains("claude"))
                return null;
            return resourceName;
        }

        // Provider-specific files
        if (resourceName.StartsWith("providers/codex/"))
            return providers.Contains("codex") ? resourceName["providers/codex/".Length..] : null;

        if (resourceName.StartsWith("providers/qwen/"))
            return providers.Contains("qwen") ? resourceName["providers/qwen/".Length..] : null;

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

        if (resourceName.StartsWith("providers/continue/"))
            return providers.Contains("continue") ? resourceName["providers/continue/".Length..] : null;

        if (resourceName.StartsWith("providers/roo/"))
            return providers.Contains("roo") ? resourceName["providers/roo/".Length..] : null;

        // GitHub Actions workflow — update only for claude workspaces
        if (resourceName == ".github/workflows/orchestrator.yml")
            return providers.Contains("claude") ? resourceName : null;

        return null;
    }

}
