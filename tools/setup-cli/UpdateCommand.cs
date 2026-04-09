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
        foreach (var def in ProviderRegistry.All)
        {
            if (def.Detection == DetectionHint.Never) continue;
            var detected = def.Detection.DetectFile is not null
                ? File.Exists(Path.Combine(root, def.Detection.DetectFile.Replace('/', Path.DirectorySeparatorChar)))
                : Directory.Exists(Path.Combine(root, def.Detection.DetectDir!.Replace('/', Path.DirectorySeparatorChar)));
            if (detected) found.Add(def.Name);
        }
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

        if (!orgMatch.Success)
            Console.WriteLine($"  WARN: could not read GitHub org from CLAUDE.md — using '{githubOrg}' (edit provider files if wrong)");

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
            ["{{HOOK_EXEC}}"]           = TemplateResources.ResolveHookExec(),
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

        // .github/workflows/ — update only for claude/nessy workspaces
        if (resourceName.StartsWith(".github/workflows/"))
            return (providers.Contains("claude") || providers.Contains("nessy")) ? resourceName : null;

        // Provider-prefixed templates (registry-driven; nessy has null prefix and is handled via .claude/ above)
        foreach (var def in ProviderRegistry.All)
        {
            if (def.TemplatePrefix is null) continue;
            if (resourceName.StartsWith(def.TemplatePrefix))
                return providers.Contains(def.Name)
                    ? resourceName[def.TemplatePrefix.Length..]
                    : null;
        }

        return null;
    }

}
