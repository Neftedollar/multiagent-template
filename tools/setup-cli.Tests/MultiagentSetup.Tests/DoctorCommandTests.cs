namespace MultiagentSetup.Tests;

/// <summary>
/// Tests for DoctorCommand.ExecuteAsync — workspace health checker.
/// Uses temp directories to avoid requiring a real multiagent workspace on the test machine.
/// </summary>
public class DoctorCommandTests : IDisposable
{
    private readonly string _root;
    private readonly string _home;

    public DoctorCommandTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"doctor-test-{Guid.NewGuid():N}");
        _home = Path.Combine(Path.GetTempPath(), $"doctor-home-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(_home);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        if (Directory.Exists(_home)) Directory.Delete(_home, recursive: true);
    }

    // ── Exit codes ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task EmptyDir_ReturnError_MissingRequiredFiles()
    {
        var exitCode = await new DoctorCommand(_root, _home).ExecuteAsync();
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task MinimalValidWorkspace_ReturnsZero()
    {
        CreateMinimalWorkspace();
        var exitCode = await new DoctorCommand(_root, _home).ExecuteAsync();
        Assert.Equal(0, exitCode);
    }

    // ── Required file checks ──────────────────────────────────────────────────

    [Fact]
    public async Task MissingClaudeMd_ReturnsError()
    {
        Directory.CreateDirectory(Path.Combine(_root, "docs"));
        File.WriteAllText(Path.Combine(_root, "docs", "process.md"), "process");

        var exitCode = await new DoctorCommand(_root, _home).ExecuteAsync();
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task MissingProcessMd_ReturnsError()
    {
        File.WriteAllText(Path.Combine(_root, "CLAUDE.md"), "# Workspace");

        var exitCode = await new DoctorCommand(_root, _home).ExecuteAsync();
        Assert.Equal(1, exitCode);
    }

    // ── Warnings don't become errors ──────────────────────────────────────────

    [Fact]
    public async Task MinimalWorkspace_WithoutOptionalFiles_StillReturnsZero()
    {
        // Only required files — no .claude/, no docs/role-capabilities.md
        CreateMinimalWorkspace();

        // Ensure optional files are absent
        var claudeDir = Path.Combine(_root, ".claude");
        if (Directory.Exists(claudeDir)) Directory.Delete(claudeDir, recursive: true);

        var exitCode = await new DoctorCommand(_root, _home).ExecuteAsync();
        Assert.Equal(0, exitCode);
    }

    // ── Hook configuration ────────────────────────────────────────────────────

    [Fact]
    public async Task UnresolvedHookExec_ReturnsZeroButWarns()
    {
        // settings.json with unresolved placeholder is a warning, not an error
        CreateMinimalWorkspace();
        var claudeDir = Path.Combine(_root, ".claude");
        Directory.CreateDirectory(claudeDir);
        File.WriteAllText(Path.Combine(claudeDir, "settings.json"),
            "{\"hooks\": {\"PostToolUse\": [{\"command\": \"{{HOOK_EXEC}} hook auto-lint\"}]}}");

        var exitCode = await new DoctorCommand(_root, _home).ExecuteAsync();
        Assert.Equal(0, exitCode); // warning only, not error
    }

    [Fact]
    public async Task ValidSettingsJson_NoHookWarning()
    {
        CreateMinimalWorkspace();
        var claudeDir = Path.Combine(_root, ".claude");
        Directory.CreateDirectory(claudeDir);
        File.WriteAllText(Path.Combine(claudeDir, "settings.json"),
            "{\"hooks\": {\"PostToolUse\": [{\"command\": \"/usr/local/bin/multiagent-setup hook auto-lint\"}]}}");

        var exitCode = await new DoctorCommand(_root, _home).ExecuteAsync();
        Assert.Equal(0, exitCode);
    }

    // ── Agent roles ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GlobalRolesPresent_ReturnsZero()
    {
        CreateMinimalWorkspace();
        var commandsDir = Path.Combine(_home, ".claude", "commands");
        Directory.CreateDirectory(commandsDir);
        File.WriteAllText(Path.Combine(commandsDir, "orchestrator.md"), "# Orchestrator");

        var exitCode = await new DoctorCommand(_root, _home).ExecuteAsync();
        Assert.Equal(0, exitCode);
    }

    // ── IDE provider detection ────────────────────────────────────────────────

    [Fact]
    public async Task CursorRulesPresent_CountsAsAgentProvider()
    {
        CreateMinimalWorkspace();
        var cursorDir = Path.Combine(_root, ".cursor", "rules");
        Directory.CreateDirectory(cursorDir);
        File.WriteAllText(Path.Combine(cursorDir, "workspace.mdc"), "# Rules");

        var exitCode = await new DoctorCommand(_root, _home).ExecuteAsync();
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task ClinerulesPresentDetectedAsIdeProvider()
    {
        CreateMinimalWorkspace();
        File.WriteAllText(Path.Combine(_root, ".clinerules"), "# Rules");

        var exitCode = await new DoctorCommand(_root, _home).ExecuteAsync();
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task AiderConfPresentDetectedAsIdeProvider()
    {
        CreateMinimalWorkspace();
        File.WriteAllText(Path.Combine(_root, ".aider.conf.yml"), "model: gpt-4");

        var exitCode = await new DoctorCommand(_root, _home).ExecuteAsync();
        Assert.Equal(0, exitCode);
    }

    // ── MCP config ───────────────────────────────────────────────────────────

    [Fact]
    public async Task McpJsonWithAgeMcp_ParsedWithoutError()
    {
        CreateMinimalWorkspace();
        var claudeDir = Path.Combine(_root, ".claude");
        Directory.CreateDirectory(claudeDir);
        File.WriteAllText(Path.Combine(claudeDir, "mcp.json"),
            "{\"mcpServers\": {\"age-mcp\": {\"command\": \"uvx\"}}}");

        var exitCode = await new DoctorCommand(_root, _home).ExecuteAsync();
        Assert.Equal(0, exitCode);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void CreateMinimalWorkspace()
    {
        File.WriteAllText(Path.Combine(_root, "CLAUDE.md"), "# Workspace");
        Directory.CreateDirectory(Path.Combine(_root, "docs"));
        File.WriteAllText(Path.Combine(_root, "docs", "process.md"), "# Process");
    }
}
