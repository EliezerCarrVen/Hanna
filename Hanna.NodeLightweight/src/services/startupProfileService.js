const { AppConfigService } = require('./appConfigService');
class StartupProfileService {
  buildPlan() {
    const cfg = new AppConfigService().getSnapshot();
    return [
      { name: 'Node runtime', status: 'start', reason: 'Node.js activo sin .NET' },
      { name: 'Telegram', status: cfg.telegram.status === 'configured' ? 'start' : 'blocked_by_configuration', reason: cfg.telegram.status },
      { name: 'LLM externo', status: Object.values(cfg.llm).some(x => x === 'configured') ? 'start' : 'blocked_by_configuration', reason: 'Groq/Gemini/OpenRouter/Ollama opcionales' },
      { name: 'Dangerous modules', status: 'dry_run', reason: 'HP Mini i386 usa dry_run por defecto' }
    ];
  }
}
module.exports = { StartupProfileService };
