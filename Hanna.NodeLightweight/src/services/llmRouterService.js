'use strict';

const { GroqAdapterService } = require('./groqAdapterService');
const { GeminiAdapterService } = require('./geminiAdapterService');
const { OpenRouterAdapterService } = require('./openRouterAdapterService');
const { OllamaAdapterService } = require('./ollamaAdapterService');
const { EmotionStateService } = require('./emotionStateService');
const { PersonalityService } = require('./personalityService');

class LlmRouterService {
  constructor() {
    this.emotions = new EmotionStateService();
    this.personality = new PersonalityService();
  }

  adapters() {
    return {
      groq: new GroqAdapterService(),
      gemini: new GeminiAdapterService(),
      openrouter: new OpenRouterAdapterService(),
      ollama: new OllamaAdapterService()
    };
  }

  statuses() {
    return Object.values(this.adapters()).map(adapter => adapter.status());
  }

  status() {
    const providers = this.statuses();
    const preferred = process.env.HANNA_LLM_PROVIDER || '';
    const active = providers.find(provider => provider.provider === preferred && provider.status === 'configured') || providers.find(provider => provider.status === 'configured');

    return active
      ? { status: 'configured', active: active.provider, providers }
      : { status: 'missing_configuration', active: 'local_fallback', providers };
  }

  providerOrder() {
    const preferred = process.env.HANNA_LLM_PROVIDER || '';
    const base = ['ollama', 'groq', 'gemini', 'openrouter'];
    if (!preferred || !base.includes(preferred)) return base;
    return [preferred, ...base.filter(provider => provider !== preferred)];
  }

  buildPrompt(input, context = {}) {
    const emotion = this.emotions.getState();
    const systemPrompt = this.personality.buildSystemPrompt({
      emotion,
      taskContext: context.taskContext || '',
      userDisplayName: context.username || context.actor || process.env.HANNA_OWNER || undefined
    });

    return [
      systemPrompt,
      '',
      'Entrada del usuario:',
      String(input || '').trim()
    ].join('\n');
  }

  async generate(prompt, context = {}) {
    const adapters = this.adapters();
    const finalPrompt = this.buildPrompt(prompt, context);
    const errors = [];

    for (const provider of this.providerOrder()) {
      const adapter = adapters[provider];

      if (!adapter || !adapter.isConfigured()) {
        errors.push({ provider, status: 'missing_configuration' });
        continue;
      }

      const result = await adapter.generate(finalPrompt, context);

      if (result && result.status === 'ok' && result.text) {
        this.emotions.recordReaction('task_success', `Respuesta generada con ${provider}`);
        return { status: 'ok', provider, text: result.text, errors };
      }

      errors.push({
        provider,
        status: result && result.status ? result.status : 'error',
        error: result && result.error ? result.error : 'unknown_error'
      });
    }

    this.emotions.recordReaction('task_failed', 'No hubo proveedor LLM disponible');
    return { status: 'local_fallback', provider: 'local', text: this.respondLocal(prompt), errors };
  }

  respondLocal(text) {
    return `No tengo un motor de IA disponible en este momento. Puedo ayudarte con comandos como /status, /doctor, /deps, /voz estado, /ia estado, /memoria buscar o /codigo buscar. Texto recibido: “${String(text || '').slice(0, 160)}”.`;
  }
}

module.exports = { LlmRouterService };
