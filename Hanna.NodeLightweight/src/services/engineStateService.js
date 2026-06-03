const { config } = require('../core/config');
class EngineStateService {
  current() {
    return {
      current: process.env.HANNA_ENGINE || 'local-node',
      status: 'ok',
      external_llm: this.externalStatus(),
      dry_run: config.dryRun,
      note: 'Modo compatible HP Mini: responde localmente sin LLM externo; Groq/OpenRouter/Ollama requieren configuración.'
    };
  }
  externalStatus() {
    if (process.env.OPENROUTER_API_KEY || process.env.GROQ_API_KEY || process.env.OLLAMA_BASE_URL) return 'configured';
    return 'missing_configuration';
  }
}
module.exports = { EngineStateService };
