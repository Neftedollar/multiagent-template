# Contributing to multiagent-template

Thanks for your interest in contributing! This document covers how to get set up and how contributions work.

## Reporting issues

Use [GitHub Issues](https://github.com/Neftedollar/multiagent-template/issues). Choose **Bug report** or **Feature request** from the templates.

Before filing a new issue: search existing issues to avoid duplicates.

## Submitting changes

1. Fork the repo and create a branch: `git checkout -b feat/my-change`
2. Make your changes (see [Development](#development) below)
3. Commit following conventional commits: `feat:`, `fix:`, `docs:`, `chore:`, etc.
4. Open a pull request. Fill in the PR template.

Keep PRs focused — one logical change per PR is easier to review and merge.

## Development

### Prerequisites

- [.NET SDK 10+](https://dotnet.microsoft.com/download)
- `git`, `jq`, `gh` on PATH

### Build

```bash
cd tools/setup-cli
dotnet build
```

### Test a template change locally

```bash
dotnet run -- new TestProject --provider claude
ls ../TestProject   # inspect generated workspace
```

### Adding a new provider

1. Create `tools/setup-cli/Templates/providers/<name>/` with:
   - `<NAME>.md` — workspace context file (adapt from `CLAUDE.md`)
   - `.<name>/settings.json` — hook configuration
   - `.<name>/commands/orchestrator.md` — orchestrator skill (adapt from Codex/Qwen)
2. Register the template files as `EmbeddedResource` in `MultiagentSetup.csproj`
3. Add the provider to `validProviders` in `Program.cs` (both `HandleNew` and `HandleAddProvider`)
4. Handle directory creation in `SetupCommand.CreateDirectories`
5. Handle template extraction in `SetupCommand.ResolveOutputPath`
6. Add pre-flight check in `SetupCommand.CheckTools`
7. Handle directory creation in `AddProviderCommand.CreateProviderDirectories`
8. Handle template extraction in `AddProviderCommand.ResolveProviderOutputPath`
9. Add provider detection in `UpdateCommand.DetectProviders`
10. Handle template extraction in `UpdateCommand.ResolveOutputPath`
11. Update zsh and PowerShell completions in `Templates/tools/`

### Templates

All scaffolded files live in `tools/setup-cli/Templates/`. They are embedded in the binary at build time. Template variables (`{{PROJECT_NAME}}`, `{{GITHUB_ORG}}`, etc.) are substituted at workspace-creation time.

To add a shared template, place the file under `Templates/docs/` or `Templates/tools/` and add it to `MultiagentSetup.csproj`.

## Code style

- C# follows the existing conventions in the repo (no specific config file)
- Keep methods short and focused
- Prefer early returns over nested `if` blocks
- New hooks go in `HooksCommand.cs`

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
