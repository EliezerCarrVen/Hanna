using Hanna.Core;
using Hanna.Models;
using Telegram.Bot;

namespace Hanna.Services;

internal sealed class ScreenAgentService
{
    private readonly AppConfig config;
    private readonly ScreenCaptureService screenCapture;
    private readonly VisionService vision;
    private readonly ContextService context;
    private readonly AgentCodingService coding;
    private readonly OverlayNotificationService overlay;
    private readonly TelegramMirrorService mirror;
    private readonly ConversationLogService logs;
    private readonly MongoLogService mongoLogs;
    private readonly TelegramBotClient botClient;
    private readonly RuntimeSettingsService? runtime;
    private readonly SemaphoreSlim gate = new(1, 1);

    public ScreenAgentService(
        AppConfig config,
        ScreenCaptureService screenCapture,
        VisionService vision,
        ContextService context,
        AgentCodingService coding,
        OverlayNotificationService overlay,
        TelegramMirrorService mirror,
        ConversationLogService logs,
        MongoLogService mongoLogs,
        TelegramBotClient botClient,
        RuntimeSettingsService? runtime = null)
    {
        this.config = config;
        this.screenCapture = screenCapture;
        this.vision = vision;
        this.context = context;
        this.coding = coding;
        this.overlay = overlay;
        this.mirror = mirror;
        this.logs = logs;
        this.mongoLogs = mongoLogs;
        this.botClient = botClient;
        this.runtime = runtime;
    }

    public async Task AnalyzeScreenAsync(CancellationToken cancellationToken)
    {
        if (!(runtime?.Snapshot().ScreenAnalysisEnabled ?? config.ScreenAnalysisEnabled))
            return;

        if (!await gate.WaitAsync(0, cancellationToken))
        {
            Console.WriteLine("[Pantalla] Ya estoy analizando la pantalla.");
            return;
        }

        try
        {
            await overlay.ShowAsync("Hanna", "Analizando pantalla...", cancellationToken);
            Console.WriteLine("[Pantalla] Capturando pantalla.");

            string base64 = screenCapture.CapturePrimaryScreenToBase64();
            long chatId = config.LocalChatId;
            HannaContext hannaContext = await context.BuildContext(chatId, cancellationToken);

            string prompt =
                "Analiza la pantalla del usuario. Responde en español. " +
                "Si ves una consigna, error, IDE, base de datos, código o solicitud de programación, resume qué necesita hacer y escribe al final una línea exacta con este formato: NECESITA_CODIGO: si. " +
                "Si no hace falta código, escribe: NECESITA_CODIGO: no. " +
                "No inventes texto que no se vea claramente.";

            string analysis = await vision.AnalyzeWithGroq(prompt, base64, hannaContext, cancellationToken);
            analysis = string.IsNullOrWhiteSpace(analysis) ? "No pude analizar la pantalla con claridad." : analysis.Trim();

            Console.WriteLine("🤖 Hanna pantalla → " + analysis);
            await logs.RegisterMessage(chatId, "HANNA_PANTALLA", analysis, cancellationToken);
            await mirror.MirrorSystem("Análisis de pantalla:\n" + analysis, cancellationToken);

            bool needsCode = Regex.IsMatch(analysis, @"NECESITA_CODIGO\s*:\s*si", RegexOptions.IgnoreCase) || CodeOutputService.LooksLikeCodeRequest(analysis);

            if (needsCode)
            {
                string request = "Con base en este análisis de pantalla, genera el código o SQL solicitado. Análisis:\n" + analysis;
                var generated = await coding.GenerateCodeFromRequest(chatId, request, cancellationToken);

                string msg = "Detecté que la pantalla probablemente pide código. Ya generé un archivo de salida.";
                if (!string.IsNullOrWhiteSpace(generated.FilePath))
                    msg += "\nArchivo: " + generated.FilePath;

                await overlay.ShowAsync("Hanna generó código", msg, cancellationToken);
                await mirror.MirrorSystem(msg + "\n\n" + generated.Response, cancellationToken);
            }
            else
            {
                await overlay.ShowAsync("Hanna pantalla", analysis, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Pantalla Error]: " + ex.Message);
            await overlay.ShowAsync("Hanna", "No pude analizar la pantalla: " + ex.Message, CancellationToken.None);
        }
        finally
        {
            gate.Release();
        }
    }
}
