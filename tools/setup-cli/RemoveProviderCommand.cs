namespace MultiagentSetup;

public sealed class RemoveProviderCommand(string provider, bool force = false)
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

        if (provider == "claude")
        {
            Console.Error.WriteLine("Error: cannot remove provider 'claude' (base provider).");
            Console.Error.WriteLine("       To remove all non-claude providers, remove them individually.");
            return Task.FromResult(1);
        }

        var def = ProviderRegistry.Find(provider);
        if (def is null)
        {
            Console.Error.WriteLine($"Error: unknown provider '{provider}'.");
            Console.Error.WriteLine($"       Valid: {string.Join(", ", ProviderRegistry.ValidForAdd)}");
            return Task.FromResult(1);
        }

        if (!IsProviderDetected(cwd, def))
        {
            Console.WriteLine($"Provider '{provider}' is not installed in this workspace.");
            return Task.FromResult(0);
        }

        if (!force)
        {
            if (Console.IsInputRedirected)
            {
                Console.Error.WriteLine("Error: running non-interactively — use --force to confirm removal.");
                return Task.FromResult(1);
            }
            Console.Write($"Remove provider '{provider}' from {cwd}? [y/N] ");
            var key = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (key != "y" && key != "yes")
            {
                Console.WriteLine("Aborted.");
                return Task.FromResult(0);
            }
        }

        // Directories shared by other active providers — don't delete those
        var otherActiveDirs = new HashSet<string>(
            ProviderRegistry.All
                .Where(p => p.Name != provider && IsProviderDetected(cwd, p))
                .SelectMany(p => p.Directories),
            StringComparer.OrdinalIgnoreCase);

        var removed = new List<string>();
        var skipped = new List<string>();

        // Delete exclusive provider directories
        foreach (var dir in def.Directories)
        {
            // Never delete shared infrastructure directories
            if (dir.StartsWith(".github", StringComparison.OrdinalIgnoreCase) ||
                dir.StartsWith(".claude", StringComparison.OrdinalIgnoreCase))
            {
                skipped.Add($"{dir} (shared infrastructure — remove manually if needed)");
                continue;
            }

            if (otherActiveDirs.Contains(dir))
            {
                skipped.Add($"{dir} (also used by another provider)");
                continue;
            }

            // Delete from the top-level dir of the path
            var rootSegment = dir.Split('/')[0];
            var fullPath = Path.Combine(cwd, rootSegment);
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, recursive: true);
                removed.Add(rootSegment + "/");
            }
        }

        // Delete the detection file (e.g. .clinerules, .aider.conf.yml, copilot-instructions.md)
        if (def.Detection.DetectFile is not null)
        {
            var detectPath = Path.Combine(cwd, def.Detection.DetectFile.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(detectPath))
            {
                File.Delete(detectPath);
                removed.Add(def.Detection.DetectFile);
            }
        }

        // Delete workspace instructions file (GEMINI.md, NESSY.md, etc.)
        if (def.WorkspaceInstructionsFile is not null)
        {
            var instrPath = Path.Combine(cwd, def.WorkspaceInstructionsFile);
            if (File.Exists(instrPath))
            {
                File.Delete(instrPath);
                removed.Add(def.WorkspaceInstructionsFile);
            }
        }

        Console.WriteLine($"\nProvider '{provider}' removed from {cwd}.");

        if (removed.Count > 0)
        {
            Console.WriteLine("Removed:");
            foreach (var r in removed) Console.WriteLine($"  - {r}");
        }

        if (skipped.Count > 0)
        {
            Console.WriteLine("Skipped:");
            foreach (var s in skipped) Console.WriteLine($"  - {s}");
        }

        Console.WriteLine();
        return Task.FromResult(0);
    }

    private static bool IsProviderDetected(string root, ProviderDef def)
    {
        if (def.Detection.DetectFile is not null)
            return File.Exists(Path.Combine(root, def.Detection.DetectFile.Replace('/', Path.DirectorySeparatorChar)));
        if (def.Detection.DetectDir is not null)
            return Directory.Exists(Path.Combine(root, def.Detection.DetectDir.Replace('/', Path.DirectorySeparatorChar)));
        if (def.WorkspaceInstructionsFile is not null)
            return File.Exists(Path.Combine(root, def.WorkspaceInstructionsFile));
        return false;
    }
}
