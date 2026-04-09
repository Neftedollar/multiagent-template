namespace MultiagentSetup.Tests;

/// <summary>
/// Tests for ProviderRegistry — the single source of truth for all provider metadata.
/// These tests verify the data contract so that adding/removing a provider causes an intentional test failure.
/// </summary>
public class ProviderRegistryTests
{
    // ── Provider count ────────────────────────────────────────────────────────

    [Fact]
    public void All_Has13Providers()
    {
        Assert.Equal(13, ProviderRegistry.All.Count);
    }

    [Fact]
    public void ValidForNew_Includes_All_Keyword()
    {
        Assert.Contains("all", ProviderRegistry.ValidForNew);
    }

    [Fact]
    public void ValidForNew_Has14Entries_13ProvidersPlusAll()
    {
        Assert.Equal(14, ProviderRegistry.ValidForNew.Count);
    }

    [Fact]
    public void ValidForAdd_ExcludesClaude_IncludesAll()
    {
        Assert.DoesNotContain("claude", ProviderRegistry.ValidForAdd);
        Assert.Contains("all", ProviderRegistry.ValidForAdd);
    }

    // nessy excluded from --provider all because it shares .claude/ with claude
    [Fact]
    public void AllExpansion_ExcludesNessy()
    {
        Assert.DoesNotContain("nessy", ProviderRegistry.AllExpansion);
    }

    [Fact]
    public void AllExpansion_Includes12Providers()
    {
        // All 13 minus nessy (which shares .claude/ with claude)
        Assert.Equal(12, ProviderRegistry.AllExpansion.Count);
    }

    // ── Find ──────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("claude")]
    [InlineData("nessy")]
    [InlineData("codex")]
    [InlineData("qwen")]
    [InlineData("cursor")]
    [InlineData("windsurf")]
    [InlineData("copilot")]
    [InlineData("gemini")]
    [InlineData("cline")]
    [InlineData("aider")]
    [InlineData("continue")]
    [InlineData("roo")]
    [InlineData("kiro")]
    public void Find_ReturnsProvider_ForEachValidName(string name)
    {
        var def = ProviderRegistry.Find(name);
        Assert.NotNull(def);
        Assert.Equal(name, def.Name);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData("CLAUDE")]   // case-sensitive
    [InlineData("all")]      // "all" is a keyword, not a provider
    public void Find_ReturnsNull_ForInvalidName(string name)
    {
        Assert.Null(ProviderRegistry.Find(name));
    }

    // ── Provider-specific contracts ───────────────────────────────────────────

    [Fact]
    public void Nessy_HasNullTemplatePrefix_SharesClaudeConfig()
    {
        var nessy = ProviderRegistry.Find("nessy")!;
        Assert.Null(nessy.TemplatePrefix);
        Assert.False(nessy.IncludedInAll);
    }

    [Fact]
    public void Claude_HasDotClaudePrefix()
    {
        var claude = ProviderRegistry.Find("claude")!;
        Assert.Equal(".claude/", claude.TemplatePrefix);
        Assert.True(claude.IncludedInAll);
    }

    [Fact]
    public void Cline_HasEmptyDirectories_WritesRootFile()
    {
        // cline writes .clinerules to workspace root, no subdirectory to create
        var cline = ProviderRegistry.Find("cline")!;
        Assert.Empty(cline.Directories);
    }

    [Fact]
    public void Aider_HasEmptyDirectories_WritesRootFile()
    {
        // aider writes .aider.conf.yml to workspace root, no subdirectory to create
        var aider = ProviderRegistry.Find("aider")!;
        Assert.Empty(aider.Directories);
    }

    [Fact]
    public void AllProviders_HaveNonEmptyNextStepTemplate()
    {
        foreach (var def in ProviderRegistry.All)
            Assert.False(string.IsNullOrWhiteSpace(def.NextStepTemplate),
                $"Provider '{def.Name}' has empty NextStepTemplate");
    }

    [Fact]
    public void AllProviders_HaveNonEmptyInstallHint()
    {
        foreach (var def in ProviderRegistry.All)
            Assert.False(string.IsNullOrWhiteSpace(def.InstallHint),
                $"Provider '{def.Name}' has empty InstallHint");
    }

    [Fact]
    public void ProvidersWithSuggestToolCheck_HaveBinaryName()
    {
        foreach (var def in ProviderRegistry.All.Where(p => p.ToolCheck == ToolCheckMode.Suggest))
            Assert.NotNull(def.BinaryName);
    }
}
