namespace Hanna.Models;

internal sealed class DynamicSkillDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public bool Enabled { get; set; } = true;
    public string Name { get; set; } = "Nueva skill";
    public string Description { get; set; } = "";
    public string[] Triggers { get; set; } = Array.Empty<string>();
    public string ActionType { get; set; } = "reply"; // reply, open_app, open_url, generate_code, notebook
    public string Payload { get; set; } = "";
    public bool RequiresConfirmation { get; set; } = false;
}
