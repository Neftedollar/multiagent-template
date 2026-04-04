using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MultiagentSetup;

internal static class ProcessHelper
{
    internal static bool IsOnPath(string name)
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
        var sep = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';
        string[] exts = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? [".exe", ".cmd", ".bat", ""] : [""];
        return pathVar.Split(sep).Any(dir =>
            exts.Any(ext => File.Exists(Path.Combine(dir, name + ext))));
    }

    internal static async Task<(int exitCode, string stdout, string stderr)> RunAsync(
        string exe, string[] args,
        string? workingDir = null,
        Dictionary<string, string>? env = null,
        bool captureOutput = false,
        bool allowFailure = false)
    {
        var psi = new ProcessStartInfo(exe)
        {
            WorkingDirectory     = workingDir ?? "",
            RedirectStandardOutput = captureOutput,
            RedirectStandardError  = captureOutput,
            UseShellExecute      = false,
        };

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        if (env is not null)
            foreach (var (k, v) in env)
                psi.Environment[k] = v;

        Process? proc;
        try { proc = Process.Start(psi); }
        catch (Exception ex)
        {
            if (!allowFailure) Console.Error.WriteLine($"FAIL: could not start {exe}: {ex.Message}");
            return (1, "", ex.Message);
        }

        if (proc is null) return (1, "", $"Failed to start {exe}");

        var stdoutTask = captureOutput ? proc.StandardOutput.ReadToEndAsync() : Task.FromResult("");
        var stderrTask = captureOutput ? proc.StandardError.ReadToEndAsync()  : Task.FromResult("");

        await proc.WaitForExitAsync();
        return (proc.ExitCode, await stdoutTask, await stderrTask);
    }

    internal static async Task<int> RunInteractiveAsync(string exe, string[] args, string? workingDir = null)
    {
        var psi = new ProcessStartInfo(exe)
        {
            WorkingDirectory = workingDir ?? "",
            UseShellExecute  = false,
        };
        foreach (var arg in args) psi.ArgumentList.Add(arg);

        Process? proc;
        try { proc = Process.Start(psi); }
        catch { return 1; }
        if (proc is null) return 1;

        await proc.WaitForExitAsync();
        return proc.ExitCode;
    }
}
