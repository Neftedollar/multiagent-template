using System.Reflection;
using System.Text;

namespace MultiagentSetup;

public sealed class CompletionsCommand(string shell)
{
    private static readonly Dictionary<string, string> ShellToResource = new(StringComparer.OrdinalIgnoreCase)
    {
        ["zsh"]  = "tools/completions.zsh",
        ["pwsh"] = "tools/completions.ps1",
        ["ps1"]  = "tools/completions.ps1",
    };

    public async Task<int> ExecuteAsync()
    {
        if (!ShellToResource.TryGetValue(shell, out var resourceName))
        {
            Console.Error.WriteLine($"Unsupported shell '{shell}'. Supported: zsh, pwsh");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Usage:");
            Console.Error.WriteLine("  multiagent-setup completions zsh   # zsh");
            Console.Error.WriteLine("  multiagent-setup completions pwsh  # PowerShell");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Install examples:");
            Console.Error.WriteLine("  eval \"$(multiagent-setup completions zsh)\"");
            Console.Error.WriteLine("  multiagent-setup completions zsh >> ~/.zshrc");
            return 1;
        }

        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            Console.Error.WriteLine($"Internal error: completions resource '{resourceName}' not found.");
            return 1;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        var content = await reader.ReadToEndAsync();
        Console.Write(content);
        return 0;
    }
}
