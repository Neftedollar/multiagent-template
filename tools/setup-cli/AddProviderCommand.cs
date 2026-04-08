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
        var def = ProviderRegistry.Find(provider);
        if (def is not null)
            Console.WriteLine(def.NextStepTemplate.Replace("{cwd}", cwd));
        Console.WriteLine();
        return 0;
    }

    private static void CreateProviderDirectories(string root, string[] providers)
    {
        foreach (var name in providers)
        {
            var def = ProviderRegistry.Find(name);
            if (def is null) continue;
            foreach (var dir in def.Directories)
                Directory.CreateDirectory(Path.Combine(root, dir.Replace('/', Path.DirectorySeparatorChar)));
        }
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

        if (!orgMatch.Success)
            Console.WriteLine($"  WARN: could not read GitHub org from CLAUDE.md — using '{githubOrg}' (edit provider files manually if wrong)");
        if (!repoMatch.Success)
            Console.WriteLine($"  WARN: could not read GitHub repo from CLAUDE.md — using '{githubRepo}' (edit provider files manually if wrong)");

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
        // Provider-prefixed templates (registry-driven; nessy has null prefix handled below)
        foreach (var def in ProviderRegistry.All)
        {
            if (def.TemplatePrefix is null) continue;
            if (resourceName.StartsWith(def.TemplatePrefix))
                return providers.Contains(def.Name)
                    ? resourceName[def.TemplatePrefix.Length..]
                    : null;
        }

        // nessy reuses .claude/ — emit if nessy is being added
        if (resourceName.StartsWith(".claude/") && providers.Contains("nessy"))
            return resourceName;

        return null;
    }

}
