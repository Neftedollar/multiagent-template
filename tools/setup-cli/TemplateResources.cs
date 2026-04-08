namespace MultiagentSetup;

internal static class TemplateResources
{
    internal static bool IsTextResource(string name) =>
        name.EndsWith(".md")   ||
        name.EndsWith(".mdc")  ||
        name.EndsWith(".json") ||
        name.EndsWith(".toml") ||
        name.EndsWith(".sh")   ||
        name.EndsWith(".zsh")  ||
        name.EndsWith(".ps1");
}
