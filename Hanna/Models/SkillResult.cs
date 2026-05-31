namespace Hanna.Models;

internal sealed record SkillResult(bool Handled, string ResponseText, bool ForceText = false, bool SkipResponse = false)
{
    public static SkillResult NotHandled() => new(false, "");
    public static SkillResult Text(string text, bool forceText = false) => new(true, text, forceText);
    public static SkillResult Silent() => new(true, "", false, true);
}
