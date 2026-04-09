using System.Runtime.InteropServices;

namespace MultiagentSetup;

internal static class TemplateResources
{
    internal static bool IsTextResource(string name) =>
        name.EndsWith(".md")    ||
        name.EndsWith(".mdc")   ||
        name.EndsWith(".json")  ||
        name.EndsWith(".toml")  ||
        name.EndsWith(".sh")    ||
        name.EndsWith(".zsh")   ||
        name.EndsWith(".ps1")   ||
        name.EndsWith(".yml")   ||
        name.EndsWith(".yaml")  ||
        name.EndsWith(".clinerules");

    /// <summary>
    /// Returns the absolute path of the running binary so that hook commands in
    /// settings.json point to the real executable — whether installed via
    /// dotnet tool, Homebrew, or a self-contained binary download.
    /// </summary>
    internal static string ResolveHookExec()
    {
        var self = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrEmpty(self) &&
            Path.GetFileNameWithoutExtension(self)
                .Equals("multiagent-setup", StringComparison.OrdinalIgnoreCase))
            return self;

        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? @"$env:USERPROFILE\.dotnet\tools\multiagent-setup.exe"
            : "$HOME/.dotnet/tools/multiagent-setup";
    }
}
