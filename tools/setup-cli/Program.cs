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
    "sync-roles"    => await HandleSyncRoles(args[1..]),
    "install-mcps"  => await new InstallMcpsCommand(args[1..]).ExecuteAsync(),
    _ when !args[0].StartsWith('-') => await HandleNew(args), // backward compat
    _ => PrintUsage(error: $"Unknown command: {args[0]}")
};

async Task<int> HandleNew(string[] a)
{
    var name = a.ElementAtOrDefault(0);
    if (name is null) return PrintUsage(error: "project-name is required");
    return await new SetupCommand(name, a.ElementAtOrDefault(1)).ExecuteAsync();
}

async Task<int> HandleSyncRoles(string[] a)
{
    var action     = a.FirstOrDefault(x => x is "--clone" or "--pull") ?? "";
    string? agDir  = null;
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
    Console.WriteLine("  sync-roles [--clone|--pull]         Sync agent roles to ~/.claude/commands/");
    Console.WriteLine("    --agency-dir <path>               Override agency-agents directory");
    Console.WriteLine("  install-mcps [options]              Install age-mcp and o-brien MCP servers");
    Console.WriteLine("    --docker                          Use local Docker (default, interactive)");
    Console.WriteLine("    --manual                          Enter connection strings manually");
    Console.WriteLine("    --age-conn <str>                  AGE connection string");
    Console.WriteLine("    --obrien-conn <str>               O'Brien connection string");
    Console.WriteLine("    --target <dir>                    Target dir for age-mcp clone");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  -h, --help                          Show this help");
    Console.WriteLine("  -v, --version                       Show version");
    Console.WriteLine();
    Console.WriteLine("Shorthand: multiagent-setup <project-name>  (same as 'new')");
    return error is null ? 0 : 1;
}
