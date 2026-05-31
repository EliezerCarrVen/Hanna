using Hanna.Models;
using Hanna.Services;

namespace Hanna.Core;

internal sealed class ModelOrchestrator
{
    private readonly AppConfig config;
    private readonly ContextService context;
    private readonly GroqService groq;
    private readonly GeminiService gemini;
    private readonly OllamaService ollama;
    private readonly OpenRouterService openRouter;
    private readonly ModelModeService modelMode;
    private readonly MongoLogService mongoLogs;
    private readonly TokenUsageLedgerService tokenLedger;
    private readonly HannaPersonaService personaService;

    public ModelOrchestrator(
        MongoLogService mongoLogs,
        AppConfig config,
        ContextService context,
        GroqService groq,
        GeminiService gemini,
        OllamaService ollama,
        OpenRouterService openRouter,
        ModelModeService modelMode,
        TokenUsageLedgerService tokenLedger,
        HannaPersonaService personaService)
    {
        this.config = config;
        this.mongoLogs = mongoLogs;
        this.context = context;
        this.groq = groq;
        this.gemini = gemini;
        this.ollama = ollama;
        this.openRouter = openRouter;
        this.modelMode = modelMode;
        this.tokenLedger = tokenLedger;
        this.personaService = personaService;
    }

    public async Task<SkillResult> AnswerGeneral(
        long chatId,
        string userText,
        bool forceVerification,
        CancellationToken cancellationToken)
    {
        HannaContext hannaContext = await context.BuildContext(chatId, cancellationToken);
        EngineMode mode = modelMode.GetMode();

        if (mode == EngineMode.GroqOnly)
            return await AnswerWithGroqOnly(chatId, userText, hannaContext, cancellationToken);

        if (mode == EngineMode.GeminiOnly)
            return await AnswerWithGeminiOnly(chatId, userText, hannaContext, cancellationToken);

        if (mode == EngineMode.Hybrid)
            return await AnswerHybrid(chatId, userText, hannaContext, cancellationToken);

        if (mode == EngineMode.OllamaLocal)
            return await AnswerWithOllamaOnly(chatId, userText, hannaContext, cancellationToken);

        if (mode == EngineMode.OpenRouter)
            return await AnswerWithOpenRouterOnly(chatId, userText, hannaContext, cancellationToken);

        return await AnswerOriginal(chatId, userText, hannaContext, forceVerification, cancellationToken);
    }

    private async Task<SkillResult> AnswerOriginal(
        long chatId,
        string userText,
        HannaContext hannaContext,
        bool forceVerification,
        CancellationToken cancellationToken)
    {
        string groqDraft;

        try
        {
            groqDraft = await groq.GenerateChat(userText, hannaContext, cancellationToken);

            await RegisterTokens(
                chatId,
                "Groq",
                config.GroqChatModel,
                userText,
                groqDraft,
                cancellationToken);

            await context.AppendModelMemory(chatId, "GROQ", userText, groqDraft, cancellationToken);
        }
        catch (Exception ex) when (IsGroqRateLimit(ex))
        {
            if (!config.AllowCrossEngineFallback)
                return SkillResult.Text("Groq llegó al límite y el respaldo cruzado está desactivado. No usé Gemini/OpenRouter/Ollama sin permiso.", true);

            return await FallbackToGemini(
                chatId,
                userText,
                hannaContext,
                "Groq llegó al límite de tokens. Usé Gemini para continuar sin cortar la conversación.",
                cancellationToken);
        }

        bool mustVerify = forceVerification || VerificationPolicy.RequiresWebVerification(userText);

        if (!mustVerify || string.IsNullOrWhiteSpace(config.GeminiApiKey) || !config.GeminiWebVerify)
            return SkillResult.Text(groqDraft);

        string verified;

        try
        {
            verified = await gemini.VerifyWithWeb(userText, groqDraft, hannaContext, cancellationToken);

            await RegisterTokens(
                chatId,
                "Gemini",
                config.GeminiModel,
                userText + "\n\nBORRADOR GROQ:\n" + groqDraft,
                verified,
                cancellationToken);
        }
        catch (Exception ex) when (IsGeminiRateLimit(ex))
        {
            Console.WriteLine("[Gemini Verify] Gemini llegó al límite. Se usará el borrador de Groq.");
            return SkillResult.Text(groqDraft);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Gemini Verify Error]: {ex.Message}");
            return SkillResult.Text(groqDraft);
        }

        if (string.IsNullOrWhiteSpace(verified))
            return SkillResult.Text(groqDraft);

        await context.AppendModelMemory(chatId, "GEMINI_VERIFY", userText, verified, cancellationToken);

        try
        {
            string finalAnswer = await groq.RefineWithVerifiedContext(
                userText,
                groqDraft,
                verified,
                hannaContext,
                cancellationToken);

            await RegisterTokens(
                chatId,
                "Groq",
                config.GroqChatModel,
                userText + "\n\nBORRADOR:\n" + groqDraft + "\n\nVERIFICACIÓN:\n" + verified,
                finalAnswer,
                cancellationToken);

            await context.AppendModelMemory(chatId, "GROQ_FINAL", userText, finalAnswer, cancellationToken);

            return SkillResult.Text(finalAnswer);
        }
        catch (Exception ex) when (IsGroqRateLimit(ex))
        {
            await context.AppendModelMemory(chatId, "GEMINI_FINAL_RATE_LIMIT", userText, verified, cancellationToken);
            return SkillResult.Text(verified);
        }
    }

    private async Task<SkillResult> AnswerHybrid(
        long chatId,
        string userText,
        HannaContext hannaContext,
        CancellationToken cancellationToken)
    {
        string groqDraft;

        try
        {
            groqDraft = await groq.GenerateChat(userText, hannaContext, cancellationToken);

            await RegisterTokens(
                chatId,
                "Groq",
                config.GroqChatModel,
                userText,
                groqDraft,
                cancellationToken);

            await context.AppendModelMemory(chatId, "GROQ_HYBRID_DRAFT", userText, groqDraft, cancellationToken);
        }
        catch (Exception ex) when (IsGroqRateLimit(ex))
        {
            if (!config.AllowCrossEngineFallback)
                return SkillResult.Text("Groq llegó al límite y el respaldo cruzado está desactivado. No usé Gemini/OpenRouter/Ollama sin permiso.", true);

            return await FallbackToGemini(
                chatId,
                userText,
                hannaContext,
                "Groq llegó al límite durante modo híbrido. Gemini tomó la respuesta para mantener continuidad.",
                cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(config.GeminiApiKey))
            return SkillResult.Text(groqDraft);

        string geminiFeedback;

        try
        {
            geminiFeedback = await gemini.VerifyWithWeb(userText, groqDraft, hannaContext, cancellationToken);

            if (string.IsNullOrWhiteSpace(geminiFeedback))
            {
                string prompt =
                    "Revisa este borrador de Groq. Mantén la personalidad de Hanna, conserva el contexto y corrige solo si hace falta:\n\n" +
                    groqDraft;

                geminiFeedback = await gemini.GenerateWithContext(
                    prompt,
                    hannaContext,
                    cancellationToken);

                await RegisterTokens(
                    chatId,
                    "Gemini",
                    config.GeminiModel,
                    prompt,
                    geminiFeedback,
                    cancellationToken);
            }
            else
            {
                await RegisterTokens(
                    chatId,
                    "Gemini",
                    config.GeminiModel,
                    userText + "\n\nBORRADOR GROQ:\n" + groqDraft,
                    geminiFeedback,
                    cancellationToken);
            }

            await context.AppendModelMemory(chatId, "GEMINI_HYBRID_FEEDBACK", userText, geminiFeedback, cancellationToken);
        }
        catch (Exception ex) when (IsGeminiRateLimit(ex))
        {
            Console.WriteLine("[Gemini Hybrid] Gemini llegó al límite. Se usará el borrador de Groq.");
            return SkillResult.Text(groqDraft);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Gemini Hybrid Error]: {ex.Message}");
            return SkillResult.Text(groqDraft);
        }

        try
        {
            string finalAnswer = await groq.RefineWithVerifiedContext(
                userText,
                groqDraft,
                geminiFeedback,
                hannaContext,
                cancellationToken);

            await RegisterTokens(
                chatId,
                "Groq",
                config.GroqChatModel,
                userText + "\n\nBORRADOR:\n" + groqDraft + "\n\nFEEDBACK GEMINI:\n" + geminiFeedback,
                finalAnswer,
                cancellationToken);

            await context.AppendModelMemory(chatId, "GROQ_HYBRID_FINAL", userText, finalAnswer, cancellationToken);

            return SkillResult.Text(finalAnswer);
        }
        catch (Exception ex) when (IsGroqRateLimit(ex))
        {
            await context.AppendModelMemory(chatId, "GEMINI_HYBRID_FINAL_RATE_LIMIT", userText, geminiFeedback, cancellationToken);
            return SkillResult.Text(geminiFeedback);
        }
    }


    private async Task<SkillResult> AnswerWithOpenRouterOnly(
        long chatId,
        string userText,
        HannaContext hannaContext,
        CancellationToken cancellationToken)
    {
        try
        {
            string answer = await openRouter.GenerateChat(chatId, userText, hannaContext, cancellationToken);
            await context.AppendModelMemory(chatId, "OPENROUTER_" + personaService.GetActivePersona().Id.ToUpperInvariant(), userText, answer, cancellationToken);
            return SkillResult.Text(answer);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[OpenRouter Error]: " + ex.Message);

            if (!config.AllowCrossEngineFallback)
                return SkillResult.Text("OpenRouter falló y el respaldo cruzado está desactivado. No usé Ollama/Groq/Gemini para evitar gastar tokens o cambiar de motor sin permiso.", true);

            try
            {
                string fallback = await ollama.GenerateChat(userText, hannaContext, cancellationToken);
                await context.AppendModelMemory(chatId, "OLLAMA_FALLBACK_OPENROUTER", userText, fallback, cancellationToken);
                return SkillResult.Text("OpenRouter no respondió o fue bloqueado por presupuesto. Usé Ollama local para mantener continuidad.\n\n" + fallback);
            }
            catch (Exception ollamaEx)
            {
                Console.WriteLine("[Ollama fallback OpenRouter Error]: " + ollamaEx.Message);
                return SkillResult.Text("OpenRouter falló y Ollama local tampoco respondió. Revisa OPENROUTER_API_KEY, presupuesto diario y que Ollama esté activo.", true);
            }
        }
    }

    private async Task<SkillResult> AnswerWithOllamaOnly(
        long chatId,
        string userText,
        HannaContext hannaContext,
        CancellationToken cancellationToken)
    {
        try
        {
            string answer = await ollama.GenerateChat(userText, hannaContext, cancellationToken);

            await RegisterTokens(
                chatId,
                "Ollama",
                config.OllamaModel,
                userText,
                answer,
                cancellationToken);

            await context.AppendModelMemory(chatId, "OLLAMA_LOCAL", userText, answer, cancellationToken);

            return SkillResult.Text(answer);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Ollama Error]: {ex.Message}");

            if (!config.AllowCrossEngineFallback)
                return SkillResult.Text("Ollama local no respondió y el respaldo cruzado está desactivado. Revisa que Ollama esté activo y que el modelo esté descargado: ollama pull " + config.OllamaModel, true);

            try
            {
                string fallback = await groq.GenerateChat(userText, hannaContext, cancellationToken);
                await context.AppendModelMemory(chatId, "GROQ_FALLBACK_OLLAMA", userText, fallback, cancellationToken);
                return SkillResult.Text("Ollama local no respondió, así que usé Groq como respaldo.\n\n" + fallback);
            }
            catch (Exception groqEx)
            {
                Console.WriteLine("[Groq fallback Ollama Error]: " + groqEx.Message);
                return SkillResult.Text(
                    "No pude responder con Ollama local ni con Groq. Revisa que Ollama esté activo y que el modelo esté descargado: ollama pull " + config.OllamaModel,
                    true);
            }
        }
    }

    private async Task<SkillResult> AnswerWithGroqOnly(
        long chatId,
        string userText,
        HannaContext hannaContext,
        CancellationToken cancellationToken)
    {
        try
        {
            string answer = await groq.GenerateChat(userText, hannaContext, cancellationToken);

            await RegisterTokens(
                chatId,
                "Groq",
                config.GroqChatModel,
                userText,
                answer,
                cancellationToken);

            await context.AppendModelMemory(chatId, "GROQ_ONLY", userText, answer, cancellationToken);

            return SkillResult.Text(answer);
        }
        catch (Exception ex) when (IsGroqRateLimit(ex))
        {
            if (!config.AllowCrossEngineFallback)
                return SkillResult.Text("Groq llegó al límite y el respaldo cruzado está desactivado. No usé Gemini/OpenRouter/Ollama sin permiso.", true);

            return await FallbackToGemini(
                chatId,
                userText,
                hannaContext,
                "Groq está en límite de tokens. Aunque estabas en modo Groq, usé Gemini como respaldo.",
                cancellationToken);
        }
    }

    private async Task<SkillResult> AnswerWithGeminiOnly(
        long chatId,
        string userText,
        HannaContext hannaContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.GeminiApiKey))
        {
            return SkillResult.Text(
                "Gemini no está configurado. Agrega GEMINI_API_KEY en HannaEnv.env o cambia a modo original.",
                true);
        }

        try
        {
            string answer = await gemini.GenerateWithContext(userText, hannaContext, cancellationToken);

            if (string.IsNullOrWhiteSpace(answer))
                throw new InvalidOperationException("Gemini no devolvió respuesta útil o está temporalmente no disponible.");

            await RegisterTokens(
                chatId,
                "Gemini",
                config.GeminiModel,
                userText,
                answer,
                cancellationToken);

            await context.AppendModelMemory(chatId, "GEMINI_ONLY", userText, answer, cancellationToken);

            return SkillResult.Text(answer);
        }
        catch (Exception ex) when (IsGeminiRateLimit(ex))
        {
            Console.WriteLine("[Motor] Gemini llegó al límite.");

            if (!config.AllowCrossEngineFallback)
                return SkillResult.Text("Gemini llegó al límite y el respaldo cruzado está desactivado. No usé Groq/OpenRouter/Ollama sin permiso.", true);

            Console.WriteLine("[Motor] Respaldo cruzado permitido. Cambiando temporalmente a Groq.");

            try
            {
                string groqAnswer = await groq.GenerateChat(userText, hannaContext, cancellationToken);

                await RegisterTokens(
                    chatId,
                    "Groq",
                    config.GroqChatModel,
                    userText,
                    groqAnswer,
                    cancellationToken);

                await context.AppendModelMemory(chatId, "GROQ_FALLBACK_GEMINI_QUOTA", userText, groqAnswer, cancellationToken);

                return SkillResult.Text(groqAnswer);
            }
            catch (Exception groqEx)
            {
                Console.WriteLine("[Groq Fallback Error]: " + groqEx.Message);

                return SkillResult.Text(
                    "Gemini llegó al límite y Groq también falló. Espera un momento y vuelve a intentar.",
                    true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Gemini Error] Gemini no está disponible: " + ex.Message);

            if (!config.AllowCrossEngineFallback)
                return SkillResult.Text("Gemini no está disponible y el respaldo cruzado está desactivado. No usé Groq/OpenRouter/Ollama sin permiso.", true);

            Console.WriteLine("[Gemini Error] Respaldo cruzado permitido. Intentando Groq.");

            try
            {
                string groqAnswer = await groq.GenerateChat(userText, hannaContext, cancellationToken);
                await context.AppendModelMemory(chatId, "GROQ_FALLBACK_GEMINI_UNAVAILABLE", userText, groqAnswer, cancellationToken);
                return SkillResult.Text(groqAnswer);
            }
            catch (Exception groqEx)
            {
                Console.WriteLine("[Groq fallback Gemini unavailable Error]: " + groqEx.Message);
                return SkillResult.Text("Gemini no está disponible y Groq también falló. Intenta de nuevo en unos minutos.", true);
            }
        }
    }

    private async Task<SkillResult> FallbackToGemini(
        long chatId,
        string userText,
        HannaContext hannaContext,
        string reason,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.GeminiApiKey))
        {
            return SkillResult.Text(
                "Groq llegó al límite de tokens y Gemini no está configurado. Espera unos minutos o agrega GEMINI_API_KEY en HannaEnv.env.",
                true);
        }

        try
        {
            string geminiAnswer = await gemini.GenerateWithContext(userText, hannaContext, cancellationToken);

            await RegisterTokens(
                chatId,
                "Gemini",
                config.GeminiModel,
                userText,
                geminiAnswer,
                cancellationToken);

            await context.AppendModelMemory(
                chatId,
                "GEMINI_FALLBACK_RATE_LIMIT",
                userText,
                geminiAnswer,
                cancellationToken);

            Console.WriteLine($"[Motor] {reason}");

            return SkillResult.Text(geminiAnswer);
        }
        catch (Exception ex) when (IsGeminiRateLimit(ex))
        {
            Console.WriteLine("[Gemini Fallback Error]: Gemini llegó al límite.");

            return SkillResult.Text(
                "Groq llegó al límite y Gemini también llegó al límite. Espera un momento y vuelve a intentarlo.",
                true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Gemini Fallback Error]: {ex.Message}");

            return SkillResult.Text(
                "Groq llegó al límite y Gemini también falló. Espera un momento y vuelve a intentarlo.",
                true);
        }
    }

    private async Task RegisterTokens(
        long chatId,
        string provider,
        string model,
        string input,
        string output,
        CancellationToken cancellationToken)
    {
        if (ShadowModeService.IsShadowActive(chatId))
            return;

        await mongoLogs.RegisterTokenUsage(
            chatId,
            provider,
            model,
            input,
            output,
            cancellationToken);

        await tokenLedger.RegisterAsync(
            chatId,
            provider,
            model,
            personaService.GetActivePersona().Id,
            input,
            output,
            cancellationToken);
    }

    private static bool IsGroqRateLimit(Exception ex)
    {
        string message = ex.ToString();

        return message.Contains("rate_limit", StringComparison.OrdinalIgnoreCase)
            || message.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
            || message.Contains("tokens per day", StringComparison.OrdinalIgnoreCase)
            || message.Contains("TPD", StringComparison.OrdinalIgnoreCase)
            || message.Contains("rate_limit_exceeded", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGeminiRateLimit(Exception ex)
    {
        string message = ex.ToString();

        return message.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Quota exceeded", StringComparison.OrdinalIgnoreCase)
            || message.Contains("429", StringComparison.OrdinalIgnoreCase)
            || message.Contains("rate-limit", StringComparison.OrdinalIgnoreCase)
            || message.Contains("503", StringComparison.OrdinalIgnoreCase)
            || message.Contains("UNAVAILABLE", StringComparison.OrdinalIgnoreCase)
            || message.Contains("high demand", StringComparison.OrdinalIgnoreCase)
            || message.Contains("currently experiencing", StringComparison.OrdinalIgnoreCase)
            || message.Contains("GenerateRequestsPerDayPerProjectPerModel", StringComparison.OrdinalIgnoreCase);
    }
}