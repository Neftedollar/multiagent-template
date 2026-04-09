namespace MultiagentSetup.Tests;

/// <summary>
/// Tests for TemplateResources.IsTextResource — ensures the right resource types
/// get template variable substitution vs. binary copy.
/// </summary>
public class TemplateResourcesTests
{
    // ── Text resources (get variable substitution) ────────────────────────────

    [Theory]
    [InlineData("CLAUDE.md")]
    [InlineData("docs/process.md")]
    [InlineData(".claude/commands/orchestrator.md")]
    [InlineData("providers/roo/.roo/rules/workspace.md")]
    [InlineData(".cursor/rules/workspace.mdc")]
    [InlineData(".claude/mcp.json")]
    [InlineData(".claude/settings.json")]
    [InlineData("providers/codex/.codex/config.toml")]
    [InlineData("tools/sync-roles.sh")]
    [InlineData("tools/completions.zsh")]
    [InlineData("tools/completions.ps1")]
    [InlineData(".github/workflows/orchestrator.yml")]
    [InlineData("providers/cline/.clinerules")]
    [InlineData(".windsurf/rules/orchestrator.md")]
    [InlineData("providers/continue/.continue/config.yaml")]
    public void TextExtensions_AreText(string name) =>
        Assert.True(TemplateResources.IsTextResource(name), $"Expected '{name}' to be a text resource");

    // ── Binary resources (copied as-is) ───────────────────────────────────────

    [Theory]
    [InlineData("docs/demo.svg")]
    [InlineData("assets/logo.png")]
    [InlineData("tools/somelib.dll")]
    public void BinaryExtensions_AreNotText(string name) =>
        Assert.False(TemplateResources.IsTextResource(name), $"Expected '{name}' to be a binary resource");

    // ── Edge cases ────────────────────────────────────────────────────────────

    [Fact]
    public void ClinerulesMustMatchWithDot_NotBareWord()
    {
        // Regression: was EndsWith("clinerules") — too loose
        Assert.True(TemplateResources.IsTextResource("providers/cline/.clinerules"));
        Assert.False(TemplateResources.IsTextResource("myclinerules"));  // no leading dot
    }
}
