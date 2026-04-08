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
        name.EndsWith("clinerules"); // .clinerules has no dot-extension match above
}
