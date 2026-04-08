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

    string[] validProviders = ["claude", "codex", "qwen", "nessy", "gemini", "all"];
    if (!validProviders.Contains(provider))
        return PrintUsage(error: $"Unknown provider '{provider}'. Valid: claude, codex, qwen, nessy, gemini, all");

    return await new SetupCommand(name, org, provider).ExecuteAsync();
}

async Task<int> HandleAddProvider(string[] a)
{
    var provider = a.FirstOrDefault(x => !x.StartsWith('-')) ?? "";
    if (string.IsNullOrEmpty(provider))
        return PrintUsage(error: "add-provider requires a provider name: claude, nessy, gemini, codex, qwen, all");

    string[] validProviders = ["claude", "codex", "qwen", "nessy", "gemini", "all"];
    if (!validProviders.Contains(provider))
        return PrintUsage(error: $"Unknown provider '{provider}'. Valid: claude, codex, qwen, nessy, gemini, all");

    string? wsDir = null;
    bool force = false;
    for (int i = 0; i < a.Length; i++)
    {
        if (a[i] == "--workspace-dir" && i + 1 < a.Length) wsDir = a[++i];
        if (a[i] == "--force") force = true;
    }
    return await new AddProviderCommand(provider, wsDir, force).ExecuteAsync();
}

async Task<int> HandleHook(string[] a)
{
    var name = a.ElementAtOrDefault(0);
    if (name is null) return PrintUsage(error: "hook name is required");
    return await new HooksCommand(name).ExecuteAsync();
}

async Task<int> HandleSyncRoles(string[] a)
{
    var action        = a.FirstOrDefault(x => x is "--clone" or "--pull") ?? "";
    string? agDir     = null;
    string? wsRoot    = null;
    for (int i = 0; i < a.Length - 1; i++)
    {
        if (a[i] == "--agency-dir")     agDir  = a[i + 1];
        if (a[i] == "--workspace-root") wsRoot = a[i + 1];
    }
    return await new SyncRolesCommand(action, agDir, wsRoot).ExecuteAsync();
}

static int PrintUsage(string? error = null)
{
    if (error is not null) Console.Error.WriteLine($"Error: {error}\n");
    Console.WriteLine("Usage: multiagent-setup <command> [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  new <project-name> [github-org]    Create a new multi-agent workspace");
    Console.WriteLine("    --provider <name>                 Provider: claude (default), codex, qwen, nessy, gemini, all");
    Console.WriteLine("  add-provider <provider>            Add a provider to an existing workspace");
    Console.WriteLine("    --workspace-dir <path>            Workspace root (default: cwd)");
    Console.WriteLine("    --force                           Overwrite existing provider config");
    Console.WriteLine("  sync-roles [--clone|--pull]         Sync agent roles to .claude/commands/ (project-local)");
    Console.WriteLine("    --agency-dir <path>               Override agency-agents directory");
    Console.WriteLine("    --workspace-root <path>           Target project root (default: cwd)");
    Console.WriteLine("  install-mcps [options]              Install age-mcp and o-brien MCP servers");
    Console.WriteLine("    --docker                          Use local Docker (default, interactive)");
    Console.WriteLine("    --manual                          Enter connection strings manually");
    Console.WriteLine("    --age-conn <str>                  AGE connection string");
    Console.WriteLine("    --obrien-conn <str>               O'Brien connection string");
    Console.WriteLine("    --target <dir>                    Target dir for age-mcp clone");
    Console.WriteLine("  hook <name>                         Run a Claude Code hook (cross-platform)");
    Console.WriteLine("    block-dangerous | enforce-commit-msg | auto-lint | log-agent | stop-guard");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  -h, --help                          Show this help");
    Console.WriteLine("  -v, --version                       Show version");
    Console.WriteLine();
    Console.WriteLine("Shorthand: multiagent-setup <project-name>  (same as 'new')");
    return error is null ? 0 : 1;
}
