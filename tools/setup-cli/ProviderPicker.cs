namespace MultiagentSetup;

/// <summary>
/// Interactive provider selection wizard shown when <c>multiagent-setup new</c> is called
/// without --provider on an interactive terminal.
/// </summary>
internal static class ProviderPicker
{
    // Short display descriptions, keyed by provider name.
    private static readonly Dictionary<string, string> Descriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["claude"]   = "Claude Code by Anthropic  (recommended)",
        ["nessy"]    = "Nessy CLI — Claude-compatible alias",
        ["codex"]    = "OpenAI Codex CLI",
        ["gemini"]   = "Google Gemini CLI",
        ["qwen"]     = "Qwen Code by Alibaba",
        ["aider"]    = "Aider — terminal AI pair programmer",
        ["cursor"]   = "Cursor IDE",
        ["windsurf"] = "Windsurf IDE by Codeium",
        ["copilot"]  = "GitHub Copilot (VS Code)",
        ["cline"]    = "Cline — VS Code extension",
        ["continue"] = "Continue.dev (VS Code / JetBrains)",
        ["roo"]      = "Roo Code — VS Code extension",
        ["kiro"]     = "Amazon Kiro — VS Code extension",
    };

    /// <summary>
    /// Displays the provider menu and returns the chosen provider name (or "all").
    /// Called only when stdin is not redirected.
    /// </summary>
    internal static string Ask()
    {
        var terminal = new[] { "claude", "nessy", "codex", "gemini", "qwen", "aider" };
        var ide      = new[] { "cursor", "windsurf", "copilot", "cline", "continue", "roo", "kiro" };

        Console.WriteLine("Which AI coding assistant will you use?");
        Console.WriteLine();

        int index = 1;
        var indexMap = new Dictionary<int, string>();

        Console.WriteLine("  Terminal agents (CLI-based):");
        foreach (var name in terminal)
        {
            indexMap[index] = name;
            Console.WriteLine($"    {index,2}. {name,-10}  {Descriptions.GetValueOrDefault(name, "")}");
            index++;
        }

        Console.WriteLine();
        Console.WriteLine("  IDE / Extension:");
        foreach (var name in ide)
        {
            indexMap[index] = name;
            Console.WriteLine($"    {index,2}. {name,-10}  {Descriptions.GetValueOrDefault(name, "")}");
            index++;
        }

        Console.WriteLine();
        Console.WriteLine($"    {index,2}. all        scaffold all providers at once");
        int allIndex = index;
        Console.WriteLine();

        Console.Write("  Enter number or name [Enter = claude]: ");

        var input = Console.ReadLine()?.Trim() ?? "";

        if (string.IsNullOrEmpty(input))
        {
            Console.WriteLine();
            return "claude";
        }

        // Number input
        if (int.TryParse(input, out var n))
        {
            if (n == allIndex) { Console.WriteLine(); return "all"; }
            if (indexMap.TryGetValue(n, out var byIndex)) { Console.WriteLine(); return byIndex; }
            Console.WriteLine($"\n  Unknown selection '{input}', defaulting to claude.\n");
            return "claude";
        }

        // Name input
        if (input.Equals("all", StringComparison.OrdinalIgnoreCase)) { Console.WriteLine(); return "all"; }
        if (ProviderRegistry.Find(input) is not null) { Console.WriteLine(); return input; }

        Console.WriteLine($"\n  Unknown provider '{input}', defaulting to claude.\n");
        return "claude";
    }
}
