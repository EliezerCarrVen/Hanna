using Hanna.Core;
using Hanna.Models;

namespace Hanna.Services;

internal sealed class DynamicSkillService
{
    private readonly AppConfig config;
    private readonly RuntimeSettingsService runtime;
    private readonly CodeOutputService codeOutput;
    private readonly AppLauncherService appLauncher;
    private readonly BrowserService browser;
    private readonly CourseNotebookService notebooks;
    private readonly object sync = new();
    private List<DynamicSkillDefinition> cache = new();
    private DateTime lastLoad = DateTime.MinValue;

    public DynamicSkillService(
        AppConfig config,
        RuntimeSettingsService runtime,
        CodeOutputService codeOutput,
        AppLauncherService appLauncher,
        BrowserService browser,
        CourseNotebookService notebooks)
    {
        this.config = config;
        this.runtime = runtime;
        this.codeOutput = codeOutput;
        this.appLauncher = appLauncher;
        this.browser = browser;
        this.notebooks = notebooks;
        EnsureFile();
    }

    public List<DynamicSkillDefinition> List()
    {
        LoadIfNeeded();
        lock (sync)
            return cache.Select(Clone).ToList();
    }

    public void SaveAll(IEnumerable<DynamicSkillDefinition> skills)
    {
        lock (sync)
        {
            cache = skills.Select(Normalize).ToList();
            SaveUnsafe();
            lastLoad = DateTime.UtcNow;
        }
    }

    public DynamicSkillDefinition Upsert(DynamicSkillDefinition skill)
    {
        lock (sync)
        {
            LoadIfNeededUnsafe();
            skill = Normalize(skill);
            int index = cache.FindIndex(x => x.Id.Equals(skill.Id, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
                cache[index] = skill;
            else
                cache.Add(skill);
            SaveUnsafe();
            return Clone(skill);
        }
    }

    public async Task<string?> TryExecute(long chatId, string text, CancellationToken cancellationToken)
    {
        if (!config.DynamicSkillsEnabled)
            return null;

        LoadIfNeeded();
        string normalized = Utilities.TextTools.Normalize(text);
        DynamicSkillDefinition? skill;
        lock (sync)
        {
            skill = cache.FirstOrDefault(s => s.Enabled && s.Triggers.Any(t => !string.IsNullOrWhiteSpace(t) && normalized.Contains(Utilities.TextTools.Normalize(t))));
        }

        if (skill == null)
            return null;

        string payload = ReplaceVariables(skill.Payload, text, chatId);
        switch ((skill.ActionType ?? "reply").Trim().ToLowerInvariant())
        {
            case "open_app":
                return appLauncher.Open(payload);
            case "open_url":
                return browser.OpenUrlOrSearch(payload, false);
            case "generate_code":
                string path = await codeOutput.SaveCode(text, payload, cancellationToken);
                return "Ejecuté la skill " + skill.Name + ". Archivo generado: " + path;
            case "notebook":
                string folder = notebooks.CreateOrUpdateNotebook(skill.Name, "Skill dinámica", payload);
                return "Actualicé el cuaderno: " + folder;
            default:
                return string.IsNullOrWhiteSpace(payload) ? "Ejecuté la skill " + skill.Name + "." : payload;
        }
    }

    private string PathFile => config.DynamicSkillsPath;

    private void EnsureFile()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PathFile) ?? config.SettingsDirectory);
        if (!File.Exists(PathFile))
        {
            var seed = new[]
            {
                new DynamicSkillDefinition
                {
                    Name = "Saludo de diagnóstico",
                    Description = "Ejemplo de skill editable desde la web.",
                    Triggers = new[] { "diagnóstico de hanna", "estado de hanna" },
                    ActionType = "reply",
                    Payload = "Estoy activa. Puedes editar esta skill desde el panel web."
                }
            };
            File.WriteAllText(PathFile, JsonSerializer.Serialize(seed, Options()), Encoding.UTF8);
        }
    }

    private void LoadIfNeeded()
    {
        lock (sync)
            LoadIfNeededUnsafe();
    }

    private void LoadIfNeededUnsafe()
    {
        EnsureFile();
        DateTime write = File.GetLastWriteTimeUtc(PathFile);
        if (cache.Count > 0 && write <= lastLoad)
            return;
        string json = File.ReadAllText(PathFile, Encoding.UTF8);
        cache = JsonSerializer.Deserialize<List<DynamicSkillDefinition>>(json, Options()) ?? new List<DynamicSkillDefinition>();
        cache = cache.Select(Normalize).ToList();
        lastLoad = write;
    }

    private void SaveUnsafe()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PathFile) ?? config.SettingsDirectory);
        File.WriteAllText(PathFile, JsonSerializer.Serialize(cache, Options()), Encoding.UTF8);
    }

    private static DynamicSkillDefinition Normalize(DynamicSkillDefinition skill)
    {
        if (string.IsNullOrWhiteSpace(skill.Id)) skill.Id = Guid.NewGuid().ToString("N");
        if (string.IsNullOrWhiteSpace(skill.Name)) skill.Name = "Skill " + skill.Id[..6];
        skill.Triggers = skill.Triggers?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? Array.Empty<string>();
        skill.ActionType = string.IsNullOrWhiteSpace(skill.ActionType) ? "reply" : skill.ActionType.Trim();
        skill.Payload ??= "";
        skill.Description ??= "";
        return skill;
    }

    private static DynamicSkillDefinition Clone(DynamicSkillDefinition source) => new()
    {
        Id = source.Id,
        Enabled = source.Enabled,
        Name = source.Name,
        Description = source.Description,
        Triggers = source.Triggers.ToArray(),
        ActionType = source.ActionType,
        Payload = source.Payload,
        RequiresConfirmation = source.RequiresConfirmation
    };

    private static string ReplaceVariables(string payload, string text, long chatId)
    {
        return (payload ?? "")
            .Replace("{{text}}", text ?? "", StringComparison.OrdinalIgnoreCase)
            .Replace("{{chatId}}", chatId.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{{now}}", DateTime.Now.ToString("yyyy-MM-dd HH:mm"), StringComparison.OrdinalIgnoreCase);
    }

    private static JsonSerializerOptions Options() => new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
}
