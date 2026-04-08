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

As of v1.23.0 all routing is table-driven. Adding a 13th provider is three steps:

1. **Add a `ProviderDef` entry** in `tools/setup-cli/ProviderRegistry.cs`:
   ```csharp
   new ProviderDef(
       Name:             "myprovider",
       TemplatePrefix:   "providers/myprovider/",
       Directories:      [".myprovider/rules"],
       Detection:        DetectionHint.ByDir(".myprovider/rules"),
       ToolCheck:        ToolCheckMode.Suggest,
       BinaryName:       "myprovider",
       InstallHint:      "https://example.com/install",
       NextStepTemplate: "  Open MyProvider in {cwd}",
       IncludedInAll:    true
   ),
   ```
   Fields:
   - `TemplatePrefix` — embedded resource prefix that maps to output path (e.g. `"providers/myprovider/"` → strips prefix, writes remainder to workspace root)
   - `Directories` — subdirectories to create at workspace creation time (can be `[]`)
   - `Detection` — `DetectionHint.ByFile("path")`, `ByDir("path")`, or `Never` (for the `update` command)
   - `ToolCheck` — `Suggest` (warns if binary not on PATH), `Info` (prints info line), or `None`
   - `IncludedInAll` — whether `--provider all` includes this provider (`false` for aliases like nessy)

2. **Create template files** in `tools/setup-cli/Templates/providers/myprovider/`:
   - Adapt context/rules files from an existing similar provider (e.g. Roo Code or Cline)
   - Format depends on the agent: MDC rules, YAML config, plain Markdown, TOML, etc.

3. **Register as `EmbeddedResource`** in `MultiagentSetup.csproj`:
   ```xml
   <!-- Provider: MyProvider -->
   <EmbeddedResource Include="Templates/providers/myprovider/.myprovider/rules/workspace.md"
                     LogicalName="providers/myprovider/.myprovider/rules/workspace.md" />
   ```

That's it — `SetupCommand`, `AddProviderCommand`, `UpdateCommand`, `Program.cs`, and the completions scripts all pick up the new provider automatically via `ProviderRegistry.All`.

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
