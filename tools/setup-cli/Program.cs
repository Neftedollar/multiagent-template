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
    "new"              => await HandleNew(args[1..]),
    "init"             => await HandleInit(args[1..]),
    "add-provider"     => await HandleAddProvider(args[1..]),
    "remove-provider"  => await HandleRemoveProvider(args[1..]),
    "list-providers"   => await new ListProvidersCommand().ExecuteAsync(),
    "update"           => await HandleUpdate(args[1..]),
    "sync-roles"       => await HandleSyncRoles(args[1..]),
    "install-mcps"     => await HandleInstallMcps(args[1..]),
    "hook"             => await HandleHook(args[1..]),
    "doctor"           => await HandleDoctor(args[1..]),
    _ when !args[0].StartsWith('-') => await HandleNew(args), // backward compat
    _ => PrintUsage(error: $"Unknown command: {args[0]}")
};

async Task<int> HandleNew(string[] a)
{
    var name = a.ElementAtOrDefault(0);
    if (name is null || name.StartsWith('-')) return PrintUsage(error: "project-name is required");

    string? org      = null;
    string? provider = null;  // null = not specified by user
    string? template = null;

    for (int i = 1; i < a.Length; i++)
    {
        if (a[i] == "--provider" && i + 1 < a.Length)
            provider = a[++i];
        else if (a[i] == "--template" && i + 1 < a.Length)
            template = a[++i];
        else if (!a[i].StartsWith('-'))
            org ??= a[i];
    }

    template ??= "default";
    if (template is not ("default" or "saas" or "oss" or "internal"))
        return PrintUsage(error: $"Unknown template '{template}'. Valid: default, saas, oss, internal");

    // Interactive provider picker when not specified and stdin is a terminal
    if (provider is null && !Console.IsInputRedirected)
        provider = ProviderPicker.Ask();

    provider ??= "claude";  // non-interactive fallback (CI / piped input)

    if (provider != "all" && ProviderRegistry.Find(provider) is null)
        return PrintUsage(error: $"Unknown provider '{provider}'. Valid: {string.Join(", ", ProviderRegistry.ValidForNew)}");

    return await new SetupCommand(name, org, provider, template).ExecuteAsync();
}

async Task<int> HandleInit(string[] a)
{
    string? dir      = null;
    string? org      = null;
    string? provider = null;
    string? template = null;
    bool    force    = false;

    for (int i = 0; i < a.Length; i++)
    {
        if (a[i] == "--provider" && i + 1 < a.Length)
            provider = a[++i];
        else if (a[i] == "--template" && i + 1 < a.Length)
            template = a[++i];
        else if (a[i] == "--force")
            force = true;
        else if (!a[i].StartsWith('-'))
            dir ??= a[i];
    }

    template ??= "default";
    if (template is not ("default" or "saas" or "oss" or "internal"))
        return PrintUsage(error: $"Unknown template '{template}'. Valid: default, saas, oss, internal");

    dir = Path.GetFullPath(dir ?? Directory.GetCurrentDirectory());

    if (!Directory.Exists(dir))
        return PrintUsage(error: $"Directory does not exist: {dir}");

    // Interactive provider picker when not specified and stdin is a terminal
    if (provider is null && !Console.IsInputRedirected)
        provider = ProviderPicker.Ask();

    provider ??= "claude";  // non-interactive fallback (CI / piped input)

    if (provider != "all" && ProviderRegistry.Find(provider) is null)
        return PrintUsage(error: $"Unknown provider '{provider}'. Valid: {string.Join(", ", ProviderRegistry.ValidForNew)}");

    return await new InitCommand(dir, org, provider, force, template).ExecuteAsync();
}

async Task<int> HandleRemoveProvider(string[] a)
{
    var provider = a.ElementAtOrDefault(0);
    if (provider is null || provider.StartsWith('-'))
        return PrintUsage(error: "provider name is required");

    bool force = a.Contains("--force");

    if (ProviderRegistry.Find(provider) is null && provider != "claude")
        return PrintUsage(error: $"Unknown provider '{provider}'. Valid: {string.Join(", ", ProviderRegistry.ValidForAdd)}");

    return await new RemoveProviderCommand(provider, force).ExecuteAsync();
}

async Task<int> HandleAddProvider(string[] a)
{
    var provider = a.ElementAtOrDefault(0);
    if (provider is null || provider.StartsWith('-'))
        return PrintUsage(error: "provider name is required");

    bool force = a.Contains("--force");

    if (provider == "all")
    {
        var errors = new List<string>();
        foreach (var p in ProviderRegistry.ValidForAdd.Where(n => n != "all"))
        {
            var r = await new AddProviderCommand(p, force).ExecuteAsync();
            if (r != 0) errors.Add(p);
        }
        if (errors.Count > 0)
        {
            Console.Error.WriteLine($"  WARN: Failed providers: {string.Join(", ", errors)}");
            return 1;
        }
        return 0;
    }

    if (provider == "claude")
        return PrintUsage(error: "Provider 'claude' is included in 'new' and cannot be added separately.\n       Use: multiagent-setup add-provider <other-provider>");

    if (ProviderRegistry.Find(provider) is null)
        return PrintUsage(error: $"Unknown provider '{provider}'. Valid: {string.Join(", ", ProviderRegistry.ValidForAdd)}");

    return await new AddProviderCommand(provider, force).ExecuteAsync();
}

Task<int> HandleUpdate(string[] a)
{
    bool force = a.Contains("--force");
    return new UpdateCommand(force).ExecuteAsync();
}

Task<int> HandleDoctor(string[] a)
{
    string? forCmd = null;
    for (int i = 0; i < a.Length - 1; i++)
        if (a[i] == "--for") forCmd = a[i + 1];
    return new DoctorCommand(forCommand: forCmd).ExecuteAsync();
}

async Task<int> HandleHook(string[] a)
{
    var name = a.ElementAtOrDefault(0);
    if (name is null || name is "--help" or "-h") return PrintUsage(name is null ? "hook name is required" : null);
    return await new HooksCommand(name).ExecuteAsync();
}

async Task<int> HandleInstallMcps(string[] a)
{
    if (a.Contains("--help") || a.Contains("-h")) return PrintUsage();
    return await new InstallMcpsCommand(a).ExecuteAsync();
}

async Task<int> HandleSyncRoles(string[] a)
{
    if (a.Contains("--help") || a.Contains("-h")) return PrintUsage();
    var action  = a.FirstOrDefault(x => x is "--clone" or "--pull") ?? "--pull";
    bool global = a.Contains("--global");
    string? agDir = null;
    for (int i = 0; i < a.Length - 1; i++)
        if (a[i] == "--agency-dir") agDir = a[i + 1];
    return await new SyncRolesCommand(action, agDir, globalSync: global).ExecuteAsync();
}

static int PrintUsage(string? error = null)
{
    if (error is not null) Console.Error.WriteLine($"Error: {error}\n");
    var allNames  = string.Join(", ", ProviderRegistry.ValidForNew);
    var addNames  = string.Join(", ", ProviderRegistry.ValidForAdd);
    Console.WriteLine("Usage: multiagent-setup <command> [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  new <project-name> [github-org]    Create a new multi-agent workspace");
    Console.WriteLine($"    --provider <name>                 Provider: claude (default), or:");
    Console.WriteLine($"                                       {allNames}");
    Console.WriteLine("    --template <name>                 Workspace template: default, saas, oss, internal");
    Console.WriteLine("  init [dir]                          Add workspace files to an existing git repo (does not touch your code)");
    Console.WriteLine("    dir                               Target directory — must already be a git repo (default: current)");
    Console.WriteLine($"    --provider <name>                 Provider: claude (default), or:");
    Console.WriteLine($"                                       {allNames}");
    Console.WriteLine("    --template <name>                 Workspace template: default, saas, oss, internal");
    Console.WriteLine("    --force                           Overwrite existing files");
    Console.WriteLine("  add-provider <name>                 Add a provider to an existing workspace");
    Console.WriteLine($"    <name>: {addNames}");
    Console.WriteLine("    --force                           Overwrite existing provider config");
    Console.WriteLine("  remove-provider <name>              Remove a provider from an existing workspace");
    Console.WriteLine($"    <name>: {addNames}");
    Console.WriteLine("    --force                           Skip confirmation prompt");
    Console.WriteLine("  list-providers                      List providers configured in current workspace");
    Console.WriteLine("  update                              Update workspace templates to latest version");
    Console.WriteLine("    --force                           Overwrite all files (CLAUDE.md preserved by default)");
    Console.WriteLine("  sync-roles [--clone|--pull]         Sync agent roles to local .claude/commands/");
    Console.WriteLine("    --global                          Also sync to ~/.claude/commands/ globally");
    Console.WriteLine("    --agency-dir <path>               Override agency-agents directory");
    Console.WriteLine("  install-mcps [options]              Install age-mcp and o-brien MCP servers");
    Console.WriteLine("    --docker                          Use local Docker (default, interactive)");
    Console.WriteLine("    --manual                          Enter connection strings manually");
    Console.WriteLine("    --age-conn <str>                  AGE connection string");
    Console.WriteLine("    --obrien-conn <str>               O'Brien connection string");
    Console.WriteLine("    --target <dir>                    Target dir for age-mcp clone");
    Console.WriteLine("  hook <name>                         Run a hook (cross-platform)");
    Console.WriteLine("    block-dangerous | enforce-commit-msg | auto-lint | log-agent | stop-guard | research-reminder");
    Console.WriteLine("  doctor                              Check workspace health (tools, files, hooks)");
    Console.WriteLine("    --for <command>                   Pre-flight check for a specific command (sync-roles, init, update)");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  -h, --help                          Show this help");
    Console.WriteLine("  -v, --version                       Show version");
    Console.WriteLine();
    Console.WriteLine("Shorthand: multiagent-setup <project-name>  (same as 'new')");
    return error is null ? 0 : 1;
}
