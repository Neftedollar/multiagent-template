# Example: Open Source Maintainer

Maintaining an open source library? Use multi-agent to handle issues, reviews, and releases.

## Setup

```bash
multiagent-setup new MyLib my-github-org
cd MyLib
git clone https://github.com/my-github-org/my-library code/MyLib
claude
```

## Handling a bug report

```
/orchestrator Fix issue #42: deserialization fails when input contains Unicode surrogate pairs.
Reproduce: see issue for test case. Target: patch release 2.1.1.
```

**What happens:**
1. `/engineering-backend-architect` investigates the bug, proposes fix
2. `/engineering-code-reviewer` validates the fix doesn't break anything
3. PR created with `fixes #42` in the description

## Weekly maintenance session

```
/orchestrator Review open PRs and triage new issues. 
Label issues by type (bug/feature/question). 
Draft responses for issues that need clarification.
```

The `/specialized-workflow-architect` manages the triage; `/engineering-technical-writer` drafts responses.

## Preparing a release

```
/orchestrator Prepare release 2.2.0. 
Changes since 2.1.x: issues #38, #40, #42, #45. 
Update CHANGELOG, bump version, tag.
```

1. `/engineering-technical-writer` drafts CHANGELOG from commit history
2. `/engineering-devops-automator` bumps versions and creates the tag PR
3. Human reviews and merges → CI publishes to NuGet/npm/PyPI

## Keeping docs current

```
/orchestrator The API changed in PR #38. Update docs/api-reference.md 
and add migration guide for users coming from 2.0.x.
```

`/engineering-technical-writer` handles this entirely — no developer time needed.

---

## Tips for library maintainers

- Add your library's API surface to `CLAUDE.md` under `## API contract` — prevents breaking changes
- Use `content` pipeline type for docs/blog posts (requires your review before publishing)
- Set up O'Brien memory for cross-session issue context — agents remember prior decisions
