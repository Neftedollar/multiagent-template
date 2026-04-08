using System.Text.RegularExpressions;

namespace MultiagentSetup.Tests;

/// <summary>
/// Tests for the block-dangerous hook patterns.
/// Patterns are duplicated here to document expected behavior and catch regressions.
/// </summary>
public class BlockDangerousTests
{
    // Mirrors HooksCommand.DangerousPatterns
    private static readonly (Regex re, string label)[] Patterns =
    [
        (new(@"rm\s+-rf\s+/",                                RegexOptions.IgnoreCase), "rm -rf /"),
        (new(@"rm\s+-rf\s+\.",                               RegexOptions.IgnoreCase), "rm -rf ."),
        (new(@"rm\s+-rf\s+\*",                               RegexOptions.IgnoreCase), "rm -rf *"),
        (new(@"git\s+push\s+(?:(?:--force|-f)\s+(?:origin\s+)?|origin\s+(?:--force|-f)\s+)(main|master)(?:\s|$)", RegexOptions.IgnoreCase), "force push to main/master"),
        (new(@"git\s+push\s+(--force|-f)\s*(?:2>|$)",        RegexOptions.IgnoreCase), "force push (no branch specified)"),
        (new(@"git\s+reset\s+--hard\s+origin/(main|master)", RegexOptions.IgnoreCase), "git reset --hard origin/main"),
        (new(@"git\s+clean\s+-fd",                           RegexOptions.IgnoreCase), "git clean -fd"),
        (new(@"DROP\s+(TABLE|DATABASE)",                     RegexOptions.IgnoreCase), "DROP TABLE/DATABASE"),
        (new(@"TRUNCATE\s+TABLE",                            RegexOptions.IgnoreCase), "TRUNCATE TABLE"),
        (new(@"mkfs\.",                                      RegexOptions.IgnoreCase), "mkfs"),
        (new(@"dd\s+if=.*of=/dev/",                         RegexOptions.IgnoreCase), "dd to device"),
        (new(@"chmod\s+-R\s+777\s+/",                       RegexOptions.IgnoreCase), "chmod -R 777 /"),
        (new(@"chown\s+-R.*\s+/",                           RegexOptions.IgnoreCase), "chown -R /"),
    ];

    private static bool IsBlocked(string cmd) =>
        Patterns.Any(p => p.re.IsMatch(cmd));

    // ── Should BLOCK ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("rm -rf /")]
    [InlineData("rm -rf /home")]
    [InlineData("rm -rf .")]
    [InlineData("rm -rf ./build")]
    [InlineData("rm -rf *")]
    public void RmRf_ShouldBlock(string cmd) => Assert.True(IsBlocked(cmd));

    [Theory]
    [InlineData("git push --force main")]
    [InlineData("git push -f main")]
    [InlineData("git push --force origin main")]
    [InlineData("git push -f origin main")]
    [InlineData("git push origin --force main")]
    [InlineData("git push origin -f main")]
    [InlineData("git push --force master")]
    [InlineData("git push -f master")]
    public void ForcePushToMain_ShouldBlock(string cmd) => Assert.True(IsBlocked(cmd));

    [Theory]
    [InlineData("git push --force 2>&1")]
    [InlineData("git push -f 2>&1")]
    [InlineData("git push --force")]
    [InlineData("git push -f")]
    public void NakedForcePush_ShouldBlock(string cmd) => Assert.True(IsBlocked(cmd));

    [Theory]
    [InlineData("git reset --hard origin/main")]
    [InlineData("git reset --hard origin/master")]
    public void ResetHardMain_ShouldBlock(string cmd) => Assert.True(IsBlocked(cmd));

    [Theory]
    [InlineData("DROP TABLE users")]
    [InlineData("DROP DATABASE mydb")]
    [InlineData("TRUNCATE TABLE sessions")]
    public void SqlDestructive_ShouldBlock(string cmd) => Assert.True(IsBlocked(cmd));

    // ── Should ALLOW ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("git push origin feat/my-feature")]
    [InlineData("git push -u origin feat/my-branch")]
    [InlineData("git push origin HEAD:gh-pages")]
    public void NormalPush_ShouldAllow(string cmd) => Assert.False(IsBlocked(cmd));

    [Theory]
    [InlineData("git push --force origin feat/my-branch")]
    [InlineData("git push -f origin my-feature")]
    public void ForcePushToFeatureBranch_ShouldAllow(string cmd) => Assert.False(IsBlocked(cmd));

    [Theory]
    [InlineData("gh pr create --body \"Force-push to feature branch (non main/master) should be allowed\"")]
    [InlineData("gh pr create --title \"fix force-push\" --body \"existing main/master patterns still apply\"")]
    public void PrBodyMentioningForcePush_ShouldAllow(string cmd) => Assert.False(IsBlocked(cmd));

    [Theory]
    [InlineData("git clean -f")]
    [InlineData("git clean -n")]
    public void SafeGitClean_ShouldAllow(string cmd) => Assert.False(IsBlocked(cmd));

    [Theory]
    [InlineData("git reset HEAD~1")]
    [InlineData("git reset --soft HEAD~1")]
    [InlineData("git reset --hard HEAD~1")]
    public void LocalReset_ShouldAllow(string cmd) => Assert.False(IsBlocked(cmd));
}
