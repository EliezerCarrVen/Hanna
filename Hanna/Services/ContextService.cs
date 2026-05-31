using Hanna.Core;
using Hanna.Models;

namespace Hanna.Services;

internal sealed class ContextService
{
    private readonly AppConfig config;
    private readonly PersonalityService personality;
    private readonly ConversationLogService logs;
    private readonly FileStorageService storage;
    private readonly PromptPackService? promptPack;

    public ContextService(
        AppConfig config,
        PersonalityService personality,
        ConversationLogService logs,
        FileStorageService storage,
        PromptPackService? promptPack = null)
    {
        this.config = config;
        this.personality = personality;
        this.logs = logs;
        this.storage = storage;
        this.promptPack = promptPack;
    }

    public async Task<HannaContext> BuildContext(long chatId, CancellationToken cancellationToken)
    {
        string personalityText = await personality.Load(cancellationToken);

        if (promptPack != null)
        {
            string appendix = await promptPack.BuildPromptAppendix(chatId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(appendix))
                personalityText += "\n\n" + appendix;
        }

        string conversation = await ReadTail(
            storage.GetChatContextPath(chatId),
            2500,
            cancellationToken);

        string modelMemory = await ReadTail(
            storage.GetModelMemoryPath(chatId),
            1200,
            cancellationToken);

        string preferences = BuildPreferences(chatId);

        return new HannaContext(
            personalityText,
            conversation,
            modelMemory,
            preferences,
            storage.GetResponseMode(chatId));
    }

    public async Task AppendModelMemory(
        long chatId,
        string model,
        string userInput,
        string modelOutput,
        CancellationToken cancellationToken)
    {
        string path = storage.GetModelMemoryPath(chatId);

        string line =
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{model}]\n" +
            $"Usuario: {Utilities.TextTools.CleanLog(userInput)}\n" +
            $"Salida: {Utilities.TextTools.CleanLog(modelOutput)}\n\n";

        await File.AppendAllTextAsync(path, line, Encoding.UTF8, cancellationToken);

        await TrimFile(path, 10000, cancellationToken);
    }


    private async Task<string> BuildModularPersonality(long chatId, string basePersonality, CancellationToken cancellationToken)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(basePersonality))
            parts.Add(basePersonality.Trim());

        string promptsRoot = Path.Combine(config.BaseDirectory, "Prompts");
        string hannaRoot = Path.Combine(promptsRoot, "Hanna");
        string chatsRoot = Path.Combine(promptsRoot, "Chats");
        string musicRoot = Path.Combine(promptsRoot, "Musica");
        string sourcesRoot = Path.Combine(promptsRoot, "Fuentes");
        string selfRoot = Path.Combine(config.BaseDirectory, "SelfKnowledge");

        foreach (string file in new[]
        {
            Path.Combine(hannaRoot, "personalidad_base.md"),
            Path.Combine(hannaRoot, "reglas_verdad.md"),
            Path.Combine(hannaRoot, "modismos_mexicanos.md"),
            Path.Combine(musicRoot, "gustos_musicales.md"),
            Path.Combine(musicRoot, "spotify_playlists.md"),
            Path.Combine(sourcesRoot, "trusted_sources.json"),
            Path.Combine(selfRoot, "hanna_funcionamiento.md")
        })
        {
            string text = await ReadOptional(file, 7000, cancellationToken);
            if (!string.IsNullOrWhiteSpace(text))
                parts.Add(text.Trim());
        }

        string specificChat = Path.Combine(chatsRoot, $"chat_{chatId}.md");
        string ownerChat = Path.Combine(chatsRoot, "chat_owner_5112232887.md");
        string templateChat = Path.Combine(chatsRoot, "plantilla_chat_usuario.md");

        string chatProfile = chatId == 5112232887
            ? await ReadOptional(ownerChat, 3000, cancellationToken)
            : await ReadOptional(specificChat, 3000, cancellationToken);

        if (string.IsNullOrWhiteSpace(chatProfile) && chatId != 5112232887)
            chatProfile = await ReadOptional(templateChat, 2500, cancellationToken);

        if (!string.IsNullOrWhiteSpace(chatProfile))
            parts.Add(chatProfile.Trim());

        return string.Join("\n\n---\n\n", parts);
    }

    private static async Task<string> ReadOptional(string path, int maxChars, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
            return "";

        string content = await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);
        if (content.Length <= maxChars)
            return content;

        return content[..maxChars];
    }

    private string BuildPreferences(long chatId)
    {
        string preferredDevice = storage.GetPreferredDevice(chatId);

        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(preferredDevice))
            sb.AppendLine($"Dispositivo Spotify preferido: {preferredDevice}");

        sb.AppendLine($"ChatId: {chatId}");

        return sb.ToString().Trim();
    }

    private static async Task<string> ReadTail(
        string path,
        int maxChars,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
            return "";

        string content = await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);

        if (content.Length <= maxChars)
            return content;

        return content[^maxChars..];
    }

    private static async Task TrimFile(
        string path,
        int maxChars,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
            return;

        string content = await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);

        if (content.Length <= maxChars)
            return;

        await File.WriteAllTextAsync(path, content[^maxChars..], Encoding.UTF8, cancellationToken);
    }
}