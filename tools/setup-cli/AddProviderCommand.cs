using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace MultiagentSetup;

public sealed class AddProviderCommand(string provider, bool force = false)
{
    public async Task<int> ExecuteAsync()
    {
        var cwd = Directory.GetCurrentDirectory();

        // Detect workspace root — look for CLAUDE.md and docs/process.md
        if (!File.Exists(Path.Combine(cwd, "CLAUDE.md")) ||
            !File.Exists(Path.Combine(cwd, "docs", "process.md")))
        {
            Console.Error.WriteLine("Error: not in a multiagent workspace (CLAUDE.md or docs/process.md not found)");
            Console.Error.WriteLine("Run this command from the workspace root directory.");
            return 1;
        }

        var projectName = Path.GetFileName(cwd);
        Console.WriteLine($"\nAdding {provider} provider to: {cwd}");
        Console.WriteLine();

        var providers = new[] { provider };

        // Create provider-specific directories
        CreateProviderDirectories(cwd, providers);

        // Build template vars from existing workspace
        var vars = BuildVarsFromWorkspace(cwd, projectName);

        // Extract only provider-specific templates (skip shared)
        ExtractProviderTemplates(cwd, vars, providers, force);
        Console.WriteLine($"  OK: {provider} templates extracted");

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var scripts = Directory.GetFiles(cwd, "*.sh", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(cwd, "*.zsh", SearchOption.AllDirectories));
            foreach (var s in scripts)
                await ProcessHelper.RunAsync("chmod", ["+x", s], allowFailure: true);
        }

        Console.WriteLine();
        Console.WriteLine($"Provider '{provider}' added!");
        Console.WriteLine();
        Console.WriteLine("Next steps:");
        if (provider is "nessy")
            Console.WriteLine($"  nessy         → /orchestrator <task>");
        else if (provider is "codex")
            Console.WriteLine($"  codex         → /orchestrator <task>");
        else if (provider is "qwen")
            Console.WriteLine($"  qwen-code     → /orchestrator <task>");
        else if (provider is "cursor")
            Console.WriteLine($"  cursor        → open {cwd}, rules load automatically");
        else if (provider is "windsurf")
            Console.WriteLine($"  windsurf      → open {cwd}, rules load automatically");
        else if (provider is "copilot")
            Console.WriteLine($"  copilot       → open {cwd} in VS Code, reads .github/copilot-instructions.md");
        else if (provider is "gemini")
            Console.WriteLine($"  gemini        → /orchestrator <task>");
        Console.WriteLine();
        return 0;
    }

    private static void CreateProviderDirectories(string root, string[] providers)
    {
        if (providers.Contains("nessy"))
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
    }

    private static Dictionary<string, string> BuildVarsFromWorkspace(string root, string projectName)
    {
        // Parse vars from existing CLAUDE.md; fall back to defaults where missing
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

    private static void ExtractProviderTemplates(string root, Dictionary<string, string> vars,
        string[] providers, bool force)
    {
        var asm = Assembly.GetExecutingAssembly();
        foreach (var resourceName in asm.GetManifestResourceNames())
        {
            // Only extract provider-specific templates, not shared ones
            var outputRel = ResolveProviderOutputPath(resourceName, providers);
            if (outputRel is null) continue;

            var outputPath = Path.Combine(root, outputRel.Replace('/', Path.DirectorySeparatorChar));

            // Skip existing files unless --force
            if (File.Exists(outputPath) && !force)
            {
                Console.WriteLine($"  SKIP: {outputRel} (already exists, use --force to overwrite)");
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

    private static string? ResolveProviderOutputPath(string resourceName, string[] providers)
    {
        // Only provider-specific templates, not shared (CLAUDE.md, docs/, tools/)
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

        // nessy reuses .claude/ — extract only if there's no .claude/ already
        if (resourceName.StartsWith(".claude/") && providers.Contains("nessy"))
            return resourceName;

        return null;
    }

}
