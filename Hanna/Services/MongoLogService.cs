using Hanna.Core;
using MongoDB.Bson;
using MongoDB.Driver;
using Telegram.Bot.Types;

namespace Hanna.Services;

internal sealed class MongoLogService
{
    private readonly AppConfig config;
    private readonly IMongoDatabase? database;

    private IMongoCollection<BsonDocument>? Usuarios => database?.GetCollection<BsonDocument>("usuarios");
    private IMongoCollection<BsonDocument>? Conexiones => database?.GetCollection<BsonDocument>("conexiones");
    private IMongoCollection<BsonDocument>? Mensajes => database?.GetCollection<BsonDocument>("mensajes");
    private IMongoCollection<BsonDocument>? TokensDiarios => database?.GetCollection<BsonDocument>("tokens_diarios");
    private IMongoCollection<BsonDocument>? Errores => database?.GetCollection<BsonDocument>("errores");

    public MongoLogService(AppConfig config)
    {
        this.config = config;

        if (!config.MongoEnabled || string.IsNullOrWhiteSpace(config.MongoUri))
            return;

        try
        {
            var client = new MongoClient(config.MongoUri);
            database = client.GetDatabase(config.MongoDatabase);
            Console.WriteLine($"[MongoDB] Conectado a {config.MongoDatabase}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MongoDB Error] No se pudo conectar: {ex.Message}");
        }
    }

    public async Task Initialize(CancellationToken cancellationToken)
    {
        if (!config.MongoEnabled || database == null)
            return;

        try
        {
            if (Usuarios != null)
            {
                var index = new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("chatId"),
                    new CreateIndexOptions { Unique = true });

                await Usuarios.Indexes.CreateOneAsync(index, cancellationToken: cancellationToken);
            }

            if (Mensajes != null)
            {
                var index = new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys
                        .Ascending("chatId")
                        .Descending("createdAt"));

                await Mensajes.Indexes.CreateOneAsync(index, cancellationToken: cancellationToken);
            }

            if (Conexiones != null)
            {
                var index = new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys
                        .Ascending("chatId")
                        .Descending("createdAt"));

                await Conexiones.Indexes.CreateOneAsync(index, cancellationToken: cancellationToken);
            }

            if (TokensDiarios != null)
            {
                var index = new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys
                        .Ascending("date")
                        .Ascending("chatId")
                        .Ascending("provider")
                        .Ascending("model"),
                    new CreateIndexOptions { Unique = true });

                await TokensDiarios.Indexes.CreateOneAsync(index, cancellationToken: cancellationToken);
            }

            if (Errores != null)
            {
                var index = new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys
                        .Ascending("chatId")
                        .Descending("createdAt"));

                await Errores.Indexes.CreateOneAsync(index, cancellationToken: cancellationToken);
            }

            Console.WriteLine("[MongoDB] Índices inicializados.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MongoDB Error] No se pudieron crear índices: {ex.Message}");
        }
    }

    public async Task UpsertUser(long chatId, User? user, bool authorized, CancellationToken cancellationToken)
    {
        if (!IsReady(Usuarios))
            return;

        try
        {
            DateTime now = DateTime.UtcNow;

            var filter = Builders<BsonDocument>.Filter.Eq("chatId", chatId);

            var update = Builders<BsonDocument>.Update
                .SetOnInsert("firstSeenAt", now)
                .Set("lastSeenAt", now)
                .Set("chatId", chatId)
                .Set("username", user?.Username ?? "")
                .Set("firstName", user?.FirstName ?? "")
                .Set("lastName", user?.LastName ?? "")
                .Set("authorized", authorized)
                .Set("platform", "Telegram");

            await Usuarios!.UpdateOneAsync(
                filter,
                update,
                new UpdateOptions { IsUpsert = true },
                cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MongoDB Error] UpsertUser: {ex.Message}");
        }
    }

    public async Task RegisterConnection(
        long chatId,
        string eventType,
        string status,
        string detail,
        CancellationToken cancellationToken)
    {
        if (!IsReady(Conexiones))
            return;

        try
        {
            var doc = new BsonDocument
            {
                { "chatId", chatId },
                { "eventType", eventType ?? "" },
                { "platform", "Telegram" },
                { "status", status ?? "" },
                { "detail", detail ?? "" },
                { "createdAt", DateTime.UtcNow }
            };

            await Conexiones!.InsertOneAsync(doc, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MongoDB Error] RegisterConnection: {ex.Message}");
        }
    }

    public async Task RegisterMessage(
        long chatId,
        string author,
        string messageType,
        string text,
        string engine,
        string responseMode,
        bool shadowActive,
        CancellationToken cancellationToken)
    {
        if (!IsReady(Mensajes))
            return;

        if (shadowActive)
            return;

        try
        {
            var doc = new BsonDocument
            {
                { "chatId", chatId },
                { "author", author ?? "" },
                { "messageType", messageType ?? "" },
                { "text", SecretSanitizer.Sanitize(text) },
                { "engine", engine ?? "" },
                { "responseMode", responseMode ?? "" },
                { "shadowActive", false },
                { "createdAt", DateTime.UtcNow }
            };

            await Mensajes!.InsertOneAsync(doc, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MongoDB Error] RegisterMessage: {ex.Message}");
        }
    }

    public async Task RegisterTokenUsage(
        long chatId,
        string provider,
        string model,
        string input,
        string output,
        CancellationToken cancellationToken,
        long? promptTokens = null,
        long? completionTokens = null,
        long? totalTokens = null)
    {
        if (!IsReady(TokensDiarios))
            return;

        try
        {
            bool isEstimated = !promptTokens.HasValue || !completionTokens.HasValue || !totalTokens.HasValue;

            long finalPromptTokens = promptTokens ?? EstimateTokens(input);
            long finalCompletionTokens = completionTokens ?? EstimateTokens(output);
            long finalTotalTokens = totalTokens ?? finalPromptTokens + finalCompletionTokens;

            string finalProvider = provider ?? "";
            string finalModel = model ?? "";
            string date = DateTime.UtcNow.ToString("yyyy-MM-dd");

            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("date", date),
                Builders<BsonDocument>.Filter.Eq("chatId", chatId),
                Builders<BsonDocument>.Filter.Eq("provider", finalProvider),
                Builders<BsonDocument>.Filter.Eq("model", finalModel)
            );

            var update = Builders<BsonDocument>.Update
                .SetOnInsert("date", date)
                .SetOnInsert("chatId", chatId)
                .SetOnInsert("provider", finalProvider)
                .SetOnInsert("model", finalModel)
                .Inc("requests", 1)
                .Inc("promptTokens", finalPromptTokens)
                .Inc("completionTokens", finalCompletionTokens)
                .Inc("totalTokens", finalTotalTokens)
                .Inc("estimatedRequests", isEstimated ? 1 : 0)
                .Inc("exactRequests", isEstimated ? 0 : 1)
                .Set("lastInputPreview", Clip(SecretSanitizer.Sanitize(input), 300))
                .Set("lastOutputPreview", Clip(SecretSanitizer.Sanitize(output), 300))
                .Set("updatedAt", DateTime.UtcNow);

            await TokensDiarios!.UpdateOneAsync(
                filter,
                update,
                new UpdateOptions { IsUpsert = true },
                cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MongoDB Error] RegisterTokenUsage: {ex.Message}");
        }
    }

    public async Task RegisterTokenUsage(
        long chatId,
        string provider,
        string model,
        long promptTokens,
        long completionTokens,
        long totalTokens,
        CancellationToken cancellationToken)
    {
        await RegisterTokenUsage(
            chatId,
            provider,
            model,
            "",
            "",
            cancellationToken,
            promptTokens,
            completionTokens,
            totalTokens);
    }

    public async Task RegisterEstimatedTokenUsage(
        long chatId,
        string provider,
        string model,
        string input,
        string output,
        CancellationToken cancellationToken)
    {
        await RegisterTokenUsage(
            chatId,
            provider,
            model,
            input,
            output,
            cancellationToken);
    }

    public async Task RegisterError(
        long chatId,
        string source,
        Exception ex,
        CancellationToken cancellationToken)
    {
        if (!IsReady(Errores))
            return;

        try
        {
            var doc = new BsonDocument
            {
                { "chatId", chatId },
                { "source", source ?? "" },
                { "message", SecretSanitizer.Sanitize(ex.Message) },
                { "stackTrace", SecretSanitizer.Sanitize(ex.ToString()) },
                { "createdAt", DateTime.UtcNow }
            };

            await Errores!.InsertOneAsync(doc, cancellationToken: cancellationToken);
        }
        catch (Exception mongoEx)
        {
            Console.WriteLine($"[MongoDB Error] RegisterError: {mongoEx.Message}");
        }
    }

    public bool IsAvailable => config.MongoEnabled && database != null;

    private bool IsReady(IMongoCollection<BsonDocument>? collection)
    {
        return config.MongoEnabled && database != null && collection != null;
    }

    private static long EstimateTokens(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return Math.Max(1, text.Length / 4);
    }

    private static string Clip(string text, int max)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        text = text.Trim();

        return text.Length <= max ? text : text[..max];
    }
}