using Hanna.Models;

namespace Hanna.Services;

internal sealed class VisionService
{
    private readonly GroqService groq;
    private readonly GeminiService gemini;

    public VisionService(GroqService groq, GeminiService gemini)
    {
        this.groq = groq;
        this.gemini = gemini;
    }

    public async Task<string> AnalyzeWithGroq(string prompt, string base64Image, HannaContext context, CancellationToken cancellationToken)
    {
        return await groq.AnalyzeImage(prompt, base64Image, context, cancellationToken);
    }
}
