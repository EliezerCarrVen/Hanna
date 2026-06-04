const { RagSearchService } = require('./ragSearchService');
const { LlmRouterService } = require('./llmRouterService');
const { SecretFilterService } = require('./secretFilterService');

class GeneralQaService {
  constructor() { this.rag = new RagSearchService(); this.llm = new LlmRouterService(); this.filter = new SecretFilterService(); }
  normalizeQuestion(text) { return String(text || '').replace(/^(busca|buscar|investiga|explicame|explícame|dime|que es|qué es)\s+/i, '').trim(); }
  isGeneralQa(text) { return /^(busca|buscar|investiga|explicame|explícame|dime|que es|qué es|que sabes de|qué sabes de)\b/i.test(String(text || '').trim()); }
  async answer(text, context = {}) {
    const question = this.filter.redact(this.normalizeQuestion(text)).slice(0, 500);
    const local = this.rag.search(question || text, 5);
    if (local.hasContext) {
      const generated = await this.llm.generate(`Responde de forma breve usando este contexto local de Hanna/Obsidian.\nPregunta: ${question}\nContexto:\n${local.context}`, context);
      if (generated.status === 'ok' && generated.text) return { type: 'general_qa', status: 'ok', source: 'obsidian+llm', question, answer: generated.text, hits: local.hits };
      return { type: 'general_qa', status: 'ok', source: 'obsidian', question, answer: `Encontré contexto local en Obsidian/memoria sobre “${question}”.\n${local.context}`, hits: local.hits };
    }
    const llm = await this.llm.generate(question || text, context);
    if (llm.status === 'ok' && llm.text) return { type: 'general_qa', status: 'ok', source: llm.provider || 'llm', question, answer: llm.text };
    return { type: 'general_qa', status: 'missing_configuration', source: 'ai', question, message: 'Puedo responder eso cuando configures un motor IA. Falta configurar GROQ_API_KEY, GEMINI_API_KEY, OPENROUTER_API_KEY u OLLAMA_BASE_URL.' };
  }
}
module.exports = { GeneralQaService };
