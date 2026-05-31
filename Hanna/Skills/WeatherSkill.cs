using Hanna.Models;
using Hanna.Services;
using Telegram.Bot;

namespace Hanna.Skills;

internal sealed class WeatherSkill : ISkill
{
    private readonly WeatherService weather;

    public WeatherSkill(WeatherService weather)
    {
        this.weather = weather;
    }

    public bool CanHandle(IntentResult intent) => intent.Type == IntentType.Weather;

    public async Task<SkillResult> Handle(long chatId, string originalText, IntentResult intent, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        return SkillResult.Text(await weather.GetWeather(intent.Query, cancellationToken));
    }
}
