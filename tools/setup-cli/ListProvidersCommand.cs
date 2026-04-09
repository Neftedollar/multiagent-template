namespace MultiagentSetup;

public sealed class ListProvidersCommand
{
    public Task<int> ExecuteAsync()
    {
        var cwd = Directory.GetCurrentDirectory();

        if (!File.Exists(Path.Combine(cwd, "CLAUDE.md")) ||
            !File.Exists(Path.Combine(cwd, "docs", "process.md")))
        {
            Console.Error.WriteLine("Error: not in a multiagent workspace (CLAUDE.md or docs/process.md not found)");
            Console.Error.WriteLine("Run this command from the workspace root directory.");
            return Task.FromResult(1);
        }

        Console.WriteLine($"\nProviders in: {cwd}\n");

        foreach (var def in ProviderRegistry.All)
        {
            var detected = IsProviderDetected(cwd, def);
            var marker   = detected ? "[+]" : "[ ]";
            Console.Write($"  {marker} {def.Name,-12}");
            if (detected && def.WorkspaceInstructionsFile is not null)
                Console.WriteLine($"  {def.WorkspaceInstructionsFile}");
            else
                Console.WriteLine();
        }

        Console.WriteLine();
        Console.WriteLine("To add a provider:    multiagent-setup add-provider <name>");
        Console.WriteLine("To remove a provider: multiagent-setup remove-provider <name>");
        Console.WriteLine();

        return Task.FromResult(0);
    }

    private static bool IsProviderDetected(string root, ProviderDef def)
    {
        if (def.Detection.DetectFile is not null)
            return File.Exists(Path.Combine(root, def.Detection.DetectFile.Replace('/', Path.DirectorySeparatorChar)));
        if (def.Detection.DetectDir is not null)
            return Directory.Exists(Path.Combine(root, def.Detection.DetectDir.Replace('/', Path.DirectorySeparatorChar)));
        // Fallback for providers with DetectionHint.Never (e.g. nessy): check instructions file
        if (def.WorkspaceInstructionsFile is not null)
            return File.Exists(Path.Combine(root, def.WorkspaceInstructionsFile));
        return false;
    }
}
