#!/usr/bin/env dotnet-script
// Install age-mcp + o-brien MCP servers (cross-platform)
// Usage: dotnet-script tools/install-mcps.csx [--docker|--manual] [--age-conn <str>] [--obrien-conn <str>]

using System.Diagnostics;

EnsureTool("multiagent-setup");

var p = Process.Start(new ProcessStartInfo(
    "multiagent-setup", ["install-mcps", ..Args])
{
    UseShellExecute = false,
});
p?.WaitForExit();
Environment.Exit(p?.ExitCode ?? 1);

static void EnsureTool(string name)
{
    if (IsOnPath(name)) return;
    Console.WriteLine($"  ..  Installing {name}...");
    Run("dotnet", ["tool", "install", "-g", name]);
    if (!IsOnPath(name)) Run("dotnet", ["tool", "update", "-g", name]);
}

static bool IsOnPath(string name)
{
    try
    {
        var check = OperatingSystem.IsWindows() ? "where" : "which";
        using var p = Process.Start(new ProcessStartInfo(check, name)
        {
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
        });
        p?.WaitForExit();
        return p?.ExitCode == 0;
    }
    catch { return false; }
}

static void Run(string exe, string[] args)
{
    using var p = Process.Start(new ProcessStartInfo(exe, args) { UseShellExecute = false });
    p?.WaitForExit();
}
