namespace MultiagentSetup;

/// <summary>How the pre-flight check surfaces a provider's agent tool.</summary>
internal enum ToolCheckMode
{
    /// <summary>Check PATH via Suggest(); warn if not found.</summary>
    Suggest,
    /// <summary>Print an INFO line only; do not check PATH.</summary>
    Info,
    /// <summary>No tool check (nessy is handled via claude's entry).</summary>
    None,
}

/// <summary>How UpdateCommand detects a provider in an existing workspace.</summary>
internal record DetectionHint(string? DetectFile, string? DetectDir)
{
    internal static DetectionHint ByFile(string path) => new(path, null);
    internal static DetectionHint ByDir(string path)  => new(null, path);
    internal static readonly DetectionHint Never       = new(null, null);
}

/// <summary>
/// All per-provider configuration in one place.
/// Adding a new provider = one record here + EmbeddedResource in .csproj + template files.
/// </summary>
internal sealed record ProviderDef(
    /// <summary>Canonical CLI name ("claude", "roo", …).</summary>
    string Name,

    /// <summary>
    /// Resource name prefix for this provider's templates, e.g. "providers/roo/".
    /// null = shares another provider's prefix (nessy reuses ".claude/").
    /// </summary>
    string? TemplatePrefix,

    /// <summary>Directories to create under the workspace root. Empty = none (cline, aider).</summary>
    string[] Directories,

    /// <summary>How UpdateCommand detects this provider in an existing workspace.</summary>
    DetectionHint Detection,

    /// <summary>How to surface the pre-flight tool check.</summary>
    ToolCheckMode ToolCheck,

    /// <summary>Binary name passed to Suggest() / IsOnPath(). null when ToolCheck != Suggest.</summary>
    string? BinaryName,

    /// <summary>Install URL (for Suggest) or full INFO text (for Info mode).</summary>
    string InstallHint,

    /// <summary>One-line "next steps" hint; {cwd} is substituted with the workspace path.</summary>
    string NextStepTemplate,

    /// <summary>True when included in --provider all expansion. nessy = false (shares .claude/ with claude).</summary>
    bool IncludedInAll
);

internal static class ProviderRegistry
{
    internal static readonly IReadOnlyList<ProviderDef> All = new[]
    {
        new ProviderDef(
            Name:             "claude",
            TemplatePrefix:   ".claude/",
            Directories:      [".claude/commands", ".claude/hooks", ".github/workflows"],
            Detection:        DetectionHint.ByDir(".claude"),
            ToolCheck:        ToolCheckMode.Suggest,
            BinaryName:       "claude",
            InstallHint:      "https://docs.anthropic.com/en/docs/claude-code",
            NextStepTemplate: "       claude        → /orchestrator <task>",
            IncludedInAll:    true
        ),
        new ProviderDef(
            Name:             "nessy",
            TemplatePrefix:   null,        // shares .claude/ with claude
            Directories:      [".claude/commands", ".claude/hooks"],
            Detection:        DetectionHint.Never,
            ToolCheck:        ToolCheckMode.Suggest,
            BinaryName:       "nessy",
            InstallHint:      "https://nessy.ai",
            NextStepTemplate: "       nessy         → /orchestrator <task>",
            IncludedInAll:    false
        ),
        new ProviderDef(
            Name:             "codex",
            TemplatePrefix:   "providers/codex/",
            Directories:      [".codex/skills"],
            Detection:        DetectionHint.ByDir(".codex"),
            ToolCheck:        ToolCheckMode.Suggest,
            BinaryName:       "codex",
            InstallHint:      "https://github.com/openai/codex",
            NextStepTemplate: "       codex         → /orchestrator <task>",
            IncludedInAll:    true
        ),
        new ProviderDef(
            Name:             "qwen",
            TemplatePrefix:   "providers/qwen/",
            Directories:      [".qwen"],
            Detection:        DetectionHint.ByDir(".qwen"),
            ToolCheck:        ToolCheckMode.Suggest,
            BinaryName:       "qwen-code",
            InstallHint:      "https://github.com/QwenLM/qwen-code",
            NextStepTemplate: "       qwen-code     → /orchestrator <task>",
            IncludedInAll:    true
        ),
        new ProviderDef(
            Name:             "cursor",
            TemplatePrefix:   "providers/cursor/",
            Directories:      [".cursor/rules"],
            Detection:        DetectionHint.ByDir(".cursor/rules"),
            ToolCheck:        ToolCheckMode.Info,
            BinaryName:       null,
            InstallHint:      "cursor — IDE tool, install from https://cursor.com",
            NextStepTemplate: "       cursor        → open {cwd}, rules load automatically",
            IncludedInAll:    true
        ),
        new ProviderDef(
            Name:             "windsurf",
            TemplatePrefix:   "providers/windsurf/",
            Directories:      [".windsurf/rules"],
            Detection:        DetectionHint.ByDir(".windsurf/rules"),
            ToolCheck:        ToolCheckMode.Info,
            BinaryName:       null,
            InstallHint:      "windsurf — IDE tool, install from https://windsurf.com",
            NextStepTemplate: "       windsurf      → open {cwd}, rules load automatically",
            IncludedInAll:    true
        ),
        new ProviderDef(
            Name:             "copilot",
            TemplatePrefix:   "providers/copilot/",
            Directories:      [".github"],
            Detection:        DetectionHint.ByFile(".github/copilot-instructions.md"),
            ToolCheck:        ToolCheckMode.Info,
            BinaryName:       null,
            InstallHint:      "copilot — GitHub Copilot, install VS Code extension",
            NextStepTemplate: "       copilot       → open {cwd} in VS Code, reads .github/copilot-instructions.md",
            IncludedInAll:    true
        ),
        new ProviderDef(
            Name:             "gemini",
            TemplatePrefix:   "providers/gemini/",
            Directories:      [".gemini"],
            Detection:        DetectionHint.ByDir(".gemini"),
            ToolCheck:        ToolCheckMode.Suggest,
            BinaryName:       "gemini",
            InstallHint:      "https://ai.google.dev/gemini-api/docs/gemini-cli",
            NextStepTemplate: "       gemini        → /orchestrator <task>",
            IncludedInAll:    true
        ),
        new ProviderDef(
            Name:             "cline",
            TemplatePrefix:   "providers/cline/",
            Directories:      [],           // writes .clinerules to workspace root, no subdirectory
            Detection:        DetectionHint.ByFile(".clinerules"),
            ToolCheck:        ToolCheckMode.Info,
            BinaryName:       null,
            InstallHint:      "cline — VS Code extension, install from marketplace",
            NextStepTemplate: "       cline         → open {cwd} in VS Code with Cline extension, .clinerules loads automatically",
            IncludedInAll:    true
        ),
        new ProviderDef(
            Name:             "aider",
            TemplatePrefix:   "providers/aider/",
            Directories:      [],           // writes .aider.conf.yml to workspace root, no subdirectory
            Detection:        DetectionHint.ByFile(".aider.conf.yml"),
            ToolCheck:        ToolCheckMode.Suggest,
            BinaryName:       "aider",
            InstallHint:      "https://aider.chat",
            NextStepTemplate: "       aider         → run 'aider' from {cwd}, CLAUDE.md loaded automatically",
            IncludedInAll:    true
        ),
        new ProviderDef(
            Name:             "continue",
            TemplatePrefix:   "providers/continue/",
            Directories:      [".continue"],
            Detection:        DetectionHint.ByDir(".continue"),
            ToolCheck:        ToolCheckMode.Info,
            BinaryName:       null,
            InstallHint:      "continue — VS Code/JetBrains extension, install from https://continue.dev",
            NextStepTemplate: "       continue      → open {cwd} in VS Code/JetBrains, rules load from .continue/config.yaml",
            IncludedInAll:    true
        ),
        new ProviderDef(
            Name:             "roo",
            TemplatePrefix:   "providers/roo/",
            Directories:      [".roo/rules"],
            Detection:        DetectionHint.ByDir(".roo/rules"),
            ToolCheck:        ToolCheckMode.Info,
            BinaryName:       null,
            InstallHint:      "roo — Roo Code VS Code extension, install from marketplace",
            NextStepTemplate: "       roo           → open {cwd} in VS Code with Roo Code extension, .roo/rules/ loads automatically",
            IncludedInAll:    true
        ),
    };

    /// <summary>All provider names valid for --provider flag (including "all").</summary>
    internal static readonly IReadOnlyList<string> ValidForNew =
        All.Select(p => p.Name).Append("all").ToList();

    /// <summary>Provider names valid for add-provider (excludes claude, includes all).</summary>
    internal static readonly IReadOnlyList<string> ValidForAdd =
        All.Where(p => p.Name != "claude").Select(p => p.Name).Append("all").ToList();

    /// <summary>Expansion of --provider all in SetupCommand (excludes nessy; claude covers it).</summary>
    internal static readonly IReadOnlyList<string> AllExpansion =
        All.Where(p => p.IncludedInAll).Select(p => p.Name).ToList();

    internal static ProviderDef? Find(string name) =>
        All.FirstOrDefault(p => p.Name == name);
}
