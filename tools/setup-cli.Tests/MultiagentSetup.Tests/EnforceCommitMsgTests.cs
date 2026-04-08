using System.Text.RegularExpressions;

namespace MultiagentSetup.Tests;

/// <summary>
/// Tests for the enforce-commit-msg hook pattern.
/// </summary>
public class EnforceCommitMsgTests
{
    private static readonly Regex ConventionalCommit = new(
        @"^(feat|fix|chore|docs|style|refactor|perf|test|ci|build|revert)(\(.+\))?!?:\s+.+",
        RegexOptions.IgnoreCase);

    private static bool IsValid(string firstLine) => ConventionalCommit.IsMatch(firstLine);

    // ── Valid messages ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("feat: add user authentication")]
    [InlineData("fix: resolve null ref in parser")]
    [InlineData("chore: bump to 1.15.0")]
    [InlineData("docs: update README FAQ")]
    [InlineData("refactor: extract IsTextResource to shared helper")]
    [InlineData("test: add hook pattern tests")]
    [InlineData("ci: update NuGet publish workflow")]
    [InlineData("style: fix trailing whitespace")]
    [InlineData("perf: cache regex compilation")]
    [InlineData("build: upgrade to .NET 10")]
    [InlineData("revert: revert feat/broken-change")]
    public void ValidTypes_ShouldPass(string msg) => Assert.True(IsValid(msg));

    [Theory]
    [InlineData("feat(auth): add JWT support")]
    [InlineData("fix(hooks): tighten force-push pattern")]
    [InlineData("chore(deps): bump xunit to 2.9.3")]
    public void WithScope_ShouldPass(string msg) => Assert.True(IsValid(msg));

    [Theory]
    [InlineData("feat!: breaking change")]
    [InlineData("fix(api)!: remove deprecated endpoint")]
    public void BreakingChange_ShouldPass(string msg) => Assert.True(IsValid(msg));

    // ── Invalid messages ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("add user authentication")]
    [InlineData("Update README")]
    [InlineData("WIP: working on auth")]
    [InlineData("fixed the bug")]
    [InlineData("seo: add robots.txt")]   // 'seo' is not a valid type
    public void InvalidTypes_ShouldFail(string msg) => Assert.False(IsValid(msg));

    [Theory]
    [InlineData("feat:")]
    [InlineData("fix: ")]
    public void EmptyDescription_ShouldFail(string msg) => Assert.False(IsValid(msg));
}
