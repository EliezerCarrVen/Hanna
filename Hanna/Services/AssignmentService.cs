using Hanna.Core;
using Hanna.Models;

namespace Hanna.Services;

internal sealed class AssignmentService : IDisposable
{
    private readonly AppConfig config;
    private readonly RuntimeSettingsService runtime;
    private readonly TelegramMirrorService mirror;
    private readonly CourseNotebookService notebooks;
    private readonly object sync = new();
    private CancellationTokenSource? cts;
    private Task? loopTask;

    public AssignmentService(AppConfig config, RuntimeSettingsService runtime, TelegramMirrorService mirror, CourseNotebookService notebooks)
    {
        this.config = config;
        this.runtime = runtime;
        this.mirror = mirror;
        this.notebooks = notebooks;
        EnsureFile();
    }

    public void Start()
    {
        if (!config.AssignmentsEnabled)
            return;
        cts = new CancellationTokenSource();
        loopTask = Task.Run(() => Loop(cts.Token));
    }

    public List<AssignmentItem> List()
    {
        lock (sync)
            return LoadUnsafe();
    }

    public void SaveAll(IEnumerable<AssignmentItem> items)
    {
        lock (sync)
            SaveUnsafe(items.Select(Normalize).ToList());
    }

    public AssignmentItem Upsert(AssignmentItem item)
    {
        lock (sync)
        {
            List<AssignmentItem> all = LoadUnsafe();
            item = Normalize(item);
            int idx = all.FindIndex(x => x.Id.Equals(item.Id, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) all[idx] = item; else all.Add(item);
            SaveUnsafe(all);
            return item;
        }
    }

    private async Task Loop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try { await CheckAndNotify(token); }
            catch (Exception ex) { Console.WriteLine("[Tareas Error]: " + ex.Message); }

            int minutes = Math.Clamp(config.AssignmentPollingMinutes, 1, 120);
            try { await Task.Delay(TimeSpan.FromMinutes(minutes), token); }
            catch (OperationCanceledException) { break; }
        }
    }

    public async Task CheckAndNotify(CancellationToken token)
    {
        List<int> reminders = ParseReminderHours();
        List<AssignmentItem> items;
        lock (sync) items = LoadUnsafe();

        bool changed = false;
        DateTime now = DateTime.Now;

        foreach (var item in items.Where(x => x.Enabled && !x.Completed))
        {
            if (!item.NotebookCreated)
            {
                string folder = notebooks.CreateOrUpdateNotebook(item.Subject, item.Source, $"# {item.Title}\n\nEntrega: {item.DueAt:yyyy-MM-dd HH:mm}\n\n{item.Description}\n\n{item.Url}");
                await mirror.MirrorSystem($"Nueva tarea registrada: {item.Subject} - {item.Title}\nEntrega: {item.DueAt:yyyy-MM-dd HH:mm}\nCuaderno: {folder}", token);
                item.NotebookCreated = true;
                changed = true;
            }

            double hours = (item.DueAt - now).TotalHours;
            foreach (int reminder in reminders)
            {
                if (hours <= reminder && hours > reminder - 0.25 && !item.NotifiedHours.Contains(reminder))
                {
                    await mirror.MirrorSystem($"Recordatorio de tarea: faltan ~{reminder} horas\nMateria: {item.Subject}\nTarea: {item.Title}\nEntrega: {item.DueAt:yyyy-MM-dd HH:mm}", token);
                    item.NotifiedHours.Add(reminder);
                    changed = true;
                }
            }
        }

        if (changed)
        {
            lock (sync) SaveUnsafe(items);
        }
    }

    private List<int> ParseReminderHours()
    {
        return (config.AssignmentReminderHours ?? "24,12,6,3,2,1")
            .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x, out int h) ? h : 0)
            .Where(x => x > 0)
            .Distinct()
            .OrderByDescending(x => x)
            .ToList();
    }

    private string PathFile => config.AssignmentsPath;

    private void EnsureFile()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PathFile) ?? config.SettingsDirectory);
        if (!File.Exists(PathFile))
            File.WriteAllText(PathFile, "[]", Encoding.UTF8);
    }

    private List<AssignmentItem> LoadUnsafe()
    {
        EnsureFile();
        string json = File.ReadAllText(PathFile, Encoding.UTF8);
        return JsonSerializer.Deserialize<List<AssignmentItem>>(json, Options())?.Select(Normalize).ToList() ?? new List<AssignmentItem>();
    }

    private void SaveUnsafe(List<AssignmentItem> items)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PathFile) ?? config.SettingsDirectory);
        File.WriteAllText(PathFile, JsonSerializer.Serialize(items, Options()), Encoding.UTF8);
    }

    private static AssignmentItem Normalize(AssignmentItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Id)) item.Id = Guid.NewGuid().ToString("N");
        if (string.IsNullOrWhiteSpace(item.Subject)) item.Subject = "Materia";
        if (string.IsNullOrWhiteSpace(item.Title)) item.Title = "Tarea";
        if (item.DueAt == default) item.DueAt = DateTime.Now.AddDays(1);
        item.NotifiedHours ??= new List<int>();
        return item;
    }

    private static JsonSerializerOptions Options() => new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public void Dispose()
    {
        try { cts?.Cancel(); } catch { }
        try { cts?.Dispose(); } catch { }
    }
}
