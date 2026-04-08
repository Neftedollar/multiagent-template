using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MultiagentSetup;

public sealed class InstallMcpsCommand
{
    private readonly string _targetDir;
    private readonly string? _ageConnArg;
    private readonly string? _obrienConnArg;
    private readonly bool?   _dockerMode;

    private const int    AgePort            = 5435;
    private const int    ObrienPort         = 5433;
    private const string AgeConnDocker      = "Host=localhost;Port=5435;Database=agemcp;Username=agemcp;Password=agemcp";
    private const string ObrienDbUrlDocker  = "Host=localhost;Port=5433;Database=obrien;Username=postgres;Password=postgres";
    private const string ObrienContainer    = "o-brien-db";
    private const string AgemcpRepo         = "https://github.com/Neftedollar/age-mcp.git";

    public InstallMcpsCommand(string[] args)
    {
        _targetDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "my-mcps"));

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--target" when i + 1 < args.Length:
                    _targetDir = Path.GetFullPath(args[++i]);
                    break;
                case "--docker":
                    _dockerMode = true;
                    break;
                case "--manual":
                    _dockerMode = false;
                    break;
                case "--age-conn" when i + 1 < args.Length:
                    _ageConnArg = args[++i];
                    _dockerMode ??= false;
                    break;
                case "--obrien-conn" when i + 1 < args.Length:
                    _obrienConnArg = args[++i];
                    _dockerMode ??= false;
                    break;
            }
        }
    }

    public async Task<int> ExecuteAsync()
    {
        Console.WriteLine("================================");
        Console.WriteLine("  my-mcps installer");
        Console.WriteLine($"  Target: {_targetDir}");
        Console.WriteLine("================================");
        Console.WriteLine();

        // Ensure ~/.dotnet/tools on PATH
        var dotnetTools = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet", "tools");
        if (!ProcessHelper.IsOnPath("age-mcp"))
        {
            var sep  = OperatingSystem.IsWindows() ? ";" : ":";
            var path = Environment.GetEnvironmentVariable("PATH") ?? "";
            if (!path.Contains(dotnetTools))
                Environment.SetEnvironmentVariable("PATH", path + sep + dotnetTools);
        }

        var connResult = await ResolveConnectionStringsAsync();
        if (connResult is null) return 0;
        var (ageConn, obrienDbUrl) = connResult.Value;

        var errors = await InstallDotnetToolsAsync();
        if (errors > 0) return 1;

        ConfigureMcp(ageConn, obrienDbUrl);

        Console.WriteLine();
        Console.WriteLine("================================");
        Console.WriteLine("  my-mcps installed!");
        Console.WriteLine("================================");
        Console.WriteLine();
        Console.WriteLine($"  age-mcp connection: {MaskConnString(ageConn)}");
        Console.WriteLine($"  o-brien connection: {MaskConnString(obrienDbUrl)}");
        Console.WriteLine();
        Console.WriteLine("  Start a Claude Code session — both MCPs are ready.");
        Console.WriteLine();
        return 0;
    }

    // ── Connection strings ────────────────────────────────────────────────────

    private async Task<(string ageConn, string obrienDbUrl)?> ResolveConnectionStringsAsync()
    {
        if (_ageConnArg is not null && _obrienConnArg is not null)
            return (_ageConnArg, _obrienConnArg);

        bool useDocker;
        if (_dockerMode.HasValue)
        {
            useDocker = _dockerMode.Value;
        }
        else if (Console.IsInputRedirected)
        {
            useDocker = true;
        }
        else
        {
            Console.WriteLine("Database setup:");
            Console.WriteLine("  [1] Start local Docker containers (default)");
            Console.WriteLine("  [2] Enter connection strings manually (remote server, existing DB)");
            Console.WriteLine();
            Console.Write("Choose [1/2, default=1]: ");
            useDocker = (Console.ReadLine()?.Trim() ?? "") != "2";
        }

        if (!useDocker)
        {
            var age    = _ageConnArg    ?? Prompt($"AGE connection string", AgeConnDocker);
            var obrien = _obrienConnArg ?? Prompt($"O'Brien connection string", ObrienDbUrlDocker);
            Console.WriteLine();
            Console.WriteLine("  OK: using custom connection strings");
            return (age, obrien);
        }

        if (!await SetupDockerAsync()) return null;
        return (AgeConnDocker, ObrienDbUrlDocker);
    }

    private static string Prompt(string label, string defaultValue)
    {
        if (Console.IsInputRedirected) return defaultValue;
        Console.WriteLine($"  {label}:");
        Console.WriteLine($"    Default: {defaultValue}");
        Console.Write("  Value [Enter to use default]: ");
        var input = Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(input) ? defaultValue : input;
    }

    // Mask passwords in connection strings for safe console output
    private static string MaskConnString(string conn)
    {
        // Replace password=... or Password=... values
        return System.Text.RegularExpressions.Regex.Replace(
            conn, @"(?i)(password=)[^;@]+", "$1***");
    }

    // ── Docker ────────────────────────────────────────────────────────────────

    private async Task<bool> SetupDockerAsync()
    {
        var agemcpDir = Path.Combine(_targetDir, "age-mcp");

        if (!ProcessHelper.IsOnPath("docker"))
        {
            Console.WriteLine("  ..  Docker not found, installing...");
            if (OperatingSystem.IsMacOS() && ProcessHelper.IsOnPath("brew"))
            {
                await ProcessHelper.RunAsync("brew", ["install", "--cask", "docker"], allowFailure: true);
                Console.WriteLine("  >>  Docker Desktop installed. Open it from Applications, then re-run.");
            }
            else if (OperatingSystem.IsWindows() && ProcessHelper.IsOnPath("winget"))
            {
                await ProcessHelper.RunAsync("winget",
                    ["install", "--id", "Docker.DockerDesktop", "-e",
                     "--accept-source-agreements", "--accept-package-agreements"],
                    allowFailure: true);
                Console.WriteLine("  >>  Docker Desktop installed. Launch it, then re-run.");
            }
            else
            {
                Console.Error.WriteLine("FAIL: install Docker Desktop — https://docker.com/products/docker-desktop");
            }
            return false;
        }

        var (infoCode, _, _) = await ProcessHelper.RunAsync("docker", ["info"],
            captureOutput: true, allowFailure: true);
        if (infoCode != 0)
        {
            Console.WriteLine("  >>  Docker installed but not running.");
            if (OperatingSystem.IsMacOS())
                await ProcessHelper.RunAsync("open", ["-a", "Docker"], allowFailure: true);
            Console.WriteLine("  >>  Wait for Docker Desktop to start, then re-run.");
            return false;
        }
        Console.WriteLine("  OK: docker");

        // ── age-mcp ───────────────────────────────────────────────────────────
        Console.WriteLine();
        Console.WriteLine("── age-mcp (AGE graph) ──────────────────────────────────");
        Console.WriteLine();

        if (Directory.Exists(agemcpDir))
        {
            Console.WriteLine($"  OK: age-mcp repo at {agemcpDir}");
            await ProcessHelper.RunAsync("git", ["pull", "--ff-only"],
                workingDir: agemcpDir, captureOutput: true, allowFailure: true);
        }
        else
        {
            Console.WriteLine("  ..  Cloning age-mcp (for docker-compose)...");
            Directory.CreateDirectory(Path.GetDirectoryName(agemcpDir)!);
            await ProcessHelper.RunAsync("git", ["clone", AgemcpRepo, agemcpDir],
                captureOutput: true, allowFailure: false);
            Console.WriteLine("  OK: age-mcp cloned");
        }

        var (_, runningOut, _) = await ProcessHelper.RunAsync("docker",
            ["ps", "--format", "{{.Names}}"], captureOutput: true, allowFailure: true);

        if (runningOut.Split('\n').Any(n => n.Contains("age") && n.Contains("db")))
        {
            Console.WriteLine("  OK: AGE database already running");
        }
        else
        {
            var composeFile = Path.Combine(agemcpDir, "docker-compose.yml");
            var composeAlt  = Path.Combine(agemcpDir, "compose.yml");
            if (File.Exists(composeFile) || File.Exists(composeAlt))
            {
                Console.WriteLine("  ..  Starting AGE database...");
                await ProcessHelper.RunAsync("docker", ["compose", "up", "-d"],
                    workingDir: agemcpDir, captureOutput: false, allowFailure: false);
                var ok = await WaitForPortAsync(AgePort);
                Console.WriteLine(ok
                    ? $"  OK: AGE database running on :{AgePort}"
                    : $"  WARN: AGE database not reachable on :{AgePort} yet");
            }
            else
            {
                Console.WriteLine("  WARN: no docker-compose.yml in age-mcp repo, skipping DB start");
            }
        }

        // ── o-brien ───────────────────────────────────────────────────────────
        Console.WriteLine();
        Console.WriteLine("── o-brien (semantic memory) ────────────────────────────");
        Console.WriteLine();

        var (_, allOut, _) = await ProcessHelper.RunAsync("docker",
            ["ps", "-a", "--format", "{{.Names}}"], captureOutput: true, allowFailure: true);
        var allContainers = allOut.Split('\n').Select(n => n.Trim()).ToHashSet();

        if (runningOut.Split('\n').Select(n => n.Trim()).Contains(ObrienContainer))
        {
            Console.WriteLine("  OK: o-brien database already running");
        }
        else if (allContainers.Contains(ObrienContainer))
        {
            Console.WriteLine("  ..  Starting existing o-brien container...");
            await ProcessHelper.RunAsync("docker", ["start", ObrienContainer],
                captureOutput: true, allowFailure: false);
            await WaitForPortAsync(ObrienPort);
            Console.WriteLine($"  OK: o-brien database running on :{ObrienPort}");
        }
        else
        {
            Console.WriteLine("  ..  Creating o-brien postgres container...");
            await ProcessHelper.RunAsync("docker", [
                "run", "-d",
                "--name", ObrienContainer,
                "-e", "POSTGRES_USER=postgres",
                "-e", "POSTGRES_PASSWORD=postgres",
                "-e", "POSTGRES_DB=obrien",
                "-p", $"{ObrienPort}:5432",
                "pgvector/pgvector:pg17"
            ], captureOutput: false, allowFailure: false);
            Console.WriteLine("  ..  Waiting for PostgreSQL...");
            var ok = await WaitForPortAsync(ObrienPort);
            Console.WriteLine(ok
                ? $"  OK: o-brien database running on :{ObrienPort}"
                : $"  WARN: o-brien postgres not reachable on :{ObrienPort} yet");
        }

        return true;
    }

    // ── dotnet tools ─────────────────────────────────────────────────────────

    private static async Task<int> InstallDotnetToolsAsync()
    {
        Console.WriteLine();
        Console.WriteLine("── Installing MCP tools ──────────────────────────────────");
        Console.WriteLine();

        int errors = 0;

        if (ProcessHelper.IsOnPath("age-mcp"))
        {
            Console.WriteLine("  OK: age-mcp already installed");
        }
        else
        {
            Console.WriteLine("  ..  Installing age-mcp (AgeMcp from NuGet)...");
            var (code, _, _) = await ProcessHelper.RunAsync("dotnet",
                ["tool", "install", "--global", "AgeMcp"], captureOutput: true, allowFailure: true);
            if (code != 0)
                (code, _, _) = await ProcessHelper.RunAsync("dotnet",
                    ["tool", "update", "--global", "AgeMcp"], captureOutput: true, allowFailure: true);
            if (code == 0) Console.WriteLine("  OK: age-mcp installed");
            else { Console.Error.WriteLine("  FAIL: could not install AgeMcp"); errors++; }
        }

        if (ProcessHelper.IsOnPath("obrien-mcp"))
        {
            Console.WriteLine("  OK: obrien-mcp already installed");
        }
        else
        {
            Console.WriteLine("  ..  Installing o-brien (OBrienMcp from NuGet)...");
            var (code, _, _) = await ProcessHelper.RunAsync("dotnet",
                ["tool", "install", "--global", "OBrienMcp"], captureOutput: true, allowFailure: true);
            if (code != 0)
                (code, _, _) = await ProcessHelper.RunAsync("dotnet",
                    ["tool", "update", "--global", "OBrienMcp"], captureOutput: true, allowFailure: true);
            if (code == 0) Console.WriteLine("  OK: obrien-mcp installed");
            else Console.WriteLine("  WARN: could not install OBrienMcp");
        }

        return errors;
    }

    // ── MCP config ────────────────────────────────────────────────────────────

    private static void ConfigureMcp(string ageConn, string obrienDbUrl)
    {
        Console.WriteLine();

        string mcpFile, mcpScope;
        if (Directory.Exists(".claude"))
        {
            mcpFile  = Path.Combine(".claude", "mcp.json");
            mcpScope = "project";
        }
        else
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            Directory.CreateDirectory(Path.Combine(home, ".claude"));
            mcpFile  = Path.Combine(home, ".claude", "mcp.json");
            mcpScope = "global";
        }

        JsonObject cfg = File.Exists(mcpFile)
            ? JsonNode.Parse(File.ReadAllText(mcpFile))?.AsObject() ?? new JsonObject()
            : new JsonObject();

        cfg.TryAdd("mcpServers", new JsonObject());
        var servers = cfg["mcpServers"]!.AsObject();
        bool changed = false;

        if (!servers.ContainsKey("age-mcp"))
        {
            servers["age-mcp"] = new JsonObject
            {
                ["type"]    = "stdio",
                ["command"] = "age-mcp",
                ["env"]     = new JsonObject
                {
                    ["AGE_CONNECTION_STRING"] = ageConn,
                    ["TENANT_ID"]             = "default"
                }
            };
            changed = true;
        }

        if (!servers.ContainsKey("o-brien"))
        {
            servers["o-brien"] = new JsonObject
            {
                ["type"]    = "stdio",
                ["command"] = "obrien-mcp",
                ["env"]     = new JsonObject { ["DATABASE_URL"] = obrienDbUrl }
            };
            changed = true;
        }

        var json = cfg.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(mcpFile, json + "\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        Console.WriteLine(changed ? "  OK: MCP config updated" : "  OK: both servers already in MCP config");
        Console.WriteLine($"  MCP scope: {mcpScope} ({mcpFile})");
    }

    // ── Port wait ─────────────────────────────────────────────────────────────

    private static async Task<bool> WaitForPortAsync(int port, int retries = 15)
    {
        for (int i = 0; i < retries; i++)
        {
            try
            {
                using var tcp = new TcpClient();
                await tcp.ConnectAsync("localhost", port);
                return true;
            }
            catch { await Task.Delay(1000); }
        }
        return false;
    }
}
