using Hanna.Core;

namespace Hanna.Services;

internal sealed class ShadowModeService
{
    private readonly AppConfig config;
    private static readonly HashSet<long> activeChats = new();
    private static readonly object gate = new();

    public ShadowModeService(AppConfig config)
    {
        this.config = config;
    }

    public bool IsActive(long chatId)
    {
        lock (gate)
            return activeChats.Contains(chatId);
    }

    public static bool IsShadowActive(long chatId)
    {
        lock (gate)
            return activeChats.Contains(chatId);
    }

    public string Activate(long chatId)
    {
        lock (gate)
            activeChats.Add(chatId);

        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(10));
            await DeleteConversationRecords(chatId);
        });

        return "🌑 Modo shadow activado por 10 minutos. Durante ese tiempo no mostraré la conversación en CMD y después borraré registros/contexto/memoria del chat.";
    }

    public async Task DeleteConversationRecords(long chatId)
    {
        lock (gate)
            activeChats.Remove(chatId);

        await Task.Run(() =>
        {
            DeleteMatchingFiles(config.LogsDirectory, $"*{chatId}*");
            DeleteMatchingFiles(config.ContextDirectory, $"chat_{chatId}.txt");
            DeleteMatchingFiles(config.MemoryDirectory, $"model_memory_{chatId}.txt");
            DeleteMatchingFiles(config.SettingsDirectory, $"preferences_{chatId}.json");
            DeleteMatchingFiles(config.SettingsDirectory, $"routines_{chatId}.json");
            DeleteMatchingFiles(config.LogsDirectory, "*.txt");
        });
    }

    private static void DeleteMatchingFiles(string directory, string pattern)
    {
        if (!Directory.Exists(directory))
            return;

        foreach (string file in Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly))
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
            }
        }
    }
}