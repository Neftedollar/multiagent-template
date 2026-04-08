using MultiagentSetup;

if (args.Length == 0 || args[0] is "-h" or "--help")
    return PrintUsage();

if (args[0] is "-v" or "--version")
{
    var v = typeof(SetupCommand).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
    Console.WriteLine($"multiagent-setup {v}");
    return 0;
}

return args[0] switch
{
    "new"           => await HandleNew(args[1..]),
    "add-provider"  => await HandleAddProvider(args[1..]),
    "update"        => await HandleUpdate(args[1..]),
    "sync-roles"    => await HandleSyncRoles(args[1..]),
    "install-mcps"  => await new InstallMcpsCommand(args[1..]).ExecuteAsync(),
    "hook"          => await HandleHook(args[1..]),
    _ when !args[0].StartsWith('-') => await HandleNew(args), // backward compat
    _ => PrintUsage(error: $"Unknown command: {args[0]}")
};

async Task<int> HandleNew(string[] a)
{
    var name = a.ElementAtOrDefault(0);
    if (name is null || name.StartsWith('-')) return PrintUsage(error: "project-name is required");

    string? org      = null;
    string  provider = "claude";

    for (int i = 1; i < a.Length; i++)
    {
        if (a[i] == "--provider" && i + 1 < a.Length)
            provider = a[++i];
        else if (!a[i].StartsWith('-'))
            org ??= a[i];
    }

    string[] validProviders = ["claude", "nessy", "codex", "qwen", "cursor", "windsurf", "copilot", "gemini", "all"];
    if (!validProviders.Contains(provider))
        return PrintUsage(error: $"Unknown provider '{provider}'. Valid: claude, nessy, codex, qwen, cursor, windsurf, copilot, gemini, all");

    return await new SetupCommand(name, org, provider).ExecuteAsync();
}

async Task<int> HandleAddProvider(string[] a)
{
    var provider = a.ElementAtOrDefault(0);
    if (provider is null || provider.StartsWith('-'))
        return PrintUsage(error: "provider name is required");

    bool force = a.Contains("--force");

    string[] validProviders = ["nessy", "codex", "qwen", "cursor", "windsurf", "copilot", "gemini"];

    if (provider == "all")
    {
        foreach (var p in validProviders)
        {
            var r = await new AddProviderCommand(p, force).ExecuteAsync();
            if (r != 0) return r;
        }
        return 0;
    }

    if (!validProviders.Contains(provider))
        return PrintUsage(error: $"Unknown provider '{provider}'. Valid: nessy, codex, qwen, cursor, windsurf, copilot, gemini, all");

    return await new AddProviderCommand(provider, force).ExecuteAsync();
}

Task<int> HandleUpdate(string[] a)
{
    bool force = a.Contains("--force");
    return new UpdateCommand(force).ExecuteAsync();
}

async Task<int> HandleHook(string[] a)
{
    var name = a.ElementAtOrDefault(0);
    if (name is null) return PrintUsage(error: "hook name is required");
    return await new HooksCommand(name).ExecuteAsync();
}

async Task<int> HandleSyncRoles(string[] a)
{
    var action = a.FirstOrDefault(x => x is "--clone" or "--pull") ?? "";
    string? agDir = null;
    for (int i = 0; i < a.Length - 1; i++)
        if (a[i] == "--agency-dir") agDir = a[i + 1];
    return await new SyncRolesCommand(action, agDir).ExecuteAsync();
}

static int PrintUsage(string? error = null)
{
    if (error is not null) Console.Error.WriteLine($"Error: {error}\n");
    Console.WriteLine("Usage: multiagent-setup <command> [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  new <project-name> [github-org]    Create a new multi-agent workspace");
    Console.WriteLine("    --provider <name>                 Provider: claude (default), nessy, codex, qwen,");
    Console.WriteLine("                                               cursor, windsurf, copilot, gemini, all");
    Console.WriteLine("  add-provider <name>                 Add a provider to an existing workspace");
    Console.WriteLine("    <name>: nessy, codex, qwen, cursor, windsurf, copilot, gemini, all");
    Console.WriteLine("    --force                           Overwrite existing provider config");
    Console.WriteLine("  update                              Update workspace templates to latest version");
    Console.WriteLine("    --force                           Overwrite all files (CLAUDE.md preserved by default)");
    Console.WriteLine("  sync-roles [--clone|--pull]         Sync agent roles to ~/.claude/commands/");
    Console.WriteLine("    --agency-dir <path>               Override agency-agents directory");
    Console.WriteLine("  install-mcps [options]              Install age-mcp and o-brien MCP servers");
    Console.WriteLine("    --docker                          Use local Docker (default, interactive)");
    Console.WriteLine("    --manual                          Enter connection strings manually");
    Console.WriteLine("    --age-conn <str>                  AGE connection string");
    Console.WriteLine("    --obrien-conn <str>               O'Brien connection string");
    Console.WriteLine("    --target <dir>                    Target dir for age-mcp clone");
    Console.WriteLine("  hook <name>                         Run a hook (cross-platform)");
    Console.WriteLine("    block-dangerous | enforce-commit-msg | auto-lint | log-agent | stop-guard");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  -h, --help                          Show this help");
    Console.WriteLine("  -v, --version                       Show version");
    Console.WriteLine();
    Console.WriteLine("Shorthand: multiagent-setup <project-name>  (same as 'new')");
    return error is null ? 0 : 1;
}
