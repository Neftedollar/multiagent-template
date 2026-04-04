using MultiagentSetup;

if (args.Length == 0 || args[0] is "-h" or "--help")
    return PrintUsage();

if (args[0] is "-v" or "--version")
{
    var v = typeof(SetupCommand).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
    Console.WriteLine($"multiagent-setup {v}");
    return 0;
}

var projectName = args[0];
var githubOrg   = args.Length > 1 ? args[1] : null;

return await new SetupCommand(projectName, githubOrg).ExecuteAsync();

static int PrintUsage()
{
    Console.WriteLine("Usage: multiagent-setup <project-name> [github-org]");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  project-name  Name for the new workspace directory");
    Console.WriteLine("  github-org    GitHub org or username (default: your authenticated gh account)");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  -h, --help    Show this help");
    Console.WriteLine("  -v, --version Show version");
    return 0;
}
