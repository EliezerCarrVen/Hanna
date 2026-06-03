const { GroqAdapterService } = require('./groqAdapterService');
const { GeminiAdapterService } = require('./geminiAdapterService');
const { OpenRouterAdapterService } = require('./openRouterAdapterService');
const { OllamaAdapterService } = require('./ollamaAdapterService');
class LlmRouterService {
  adapters() { return { groq: new GroqAdapterService(), gemini: new GeminiAdapterService(), openrouter: new OpenRouterAdapterService(), ollama: new OllamaAdapterService() }; }
  statuses() { return Object.values(this.adapters()).map(a => a.status()); }
  status() { const providers = this.statuses(); const preferred = process.env.HANNA_LLM_PROVIDER || ''; const active = providers.find(p => p.provider === preferred && p.status === 'configured') || providers.find(p => p.status === 'configured'); return active ? { status: 'configured', active: active.provider, providers } : { status: 'missing_configuration', active: 'local_fallback', providers }; }
  async generate(prompt, context = {}) { const adapters = this.adapters(); const provider = process.env.HANNA_LLM_PROVIDER || this.status().active; const adapter = adapters[provider]; if (!adapter || !adapter.isConfigured()) return { status: 'missing_configuration', provider: provider || 'local_fallback', text: await this.respondLocal(prompt) }; return adapter.generate(prompt, context); }
  async respondLocal(text) { return `No estoy segura de lo que necesitas. Puedo ayudarte con diagnóstico, memoria, auditoría, dependencias, motor, fase, vault, NAS, MQTT o Wake-on-LAN. Texto recibido: “${String(text || '').slice(0, 160)}”.`; }
}
module.exports = { LlmRouterService };
