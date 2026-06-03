const { PersonaService } = require('./personaService');
const { FlatFileMemoryService } = require('./flatFileMemoryService');
const { MarkdownVaultService } = require('./markdownVaultService');
const { RollingSummaryService } = require('./rollingSummaryService');
const { GeneralQaService } = require('./generalQaService');
const { ReactionService } = require('./reactionService');

class ConversationService {
  constructor() { this.persona = new PersonaService(); this.reactions = new ReactionService(); }
  async respond(action, text, context = {}) {
    if (action === 'greeting') return { human: this.reactions.greeting(), data: { type: 'greeting' } };
    if (action === 'capabilities') return { human: this.persona.capabilities(), data: { type: 'capabilities' } };
    if (action === 'general_qa') return await new GeneralQaService().answer(text, context);
    return { human: this.persona.fallback(text), data: { type: 'fallback', text, source: context.source || 'cli' } };
  }
  saveMemory(text, actor) {
    const memory = new FlatFileMemoryService().add(text, actor);
    const note = new MarkdownVaultService().createNote('memoria', 'memoria', text);
    new RollingSummaryService().regenerate();
    return { type: 'memory_saved', ...memory, file: note.file };
  }
  searchMemory(query) {
    const short = new FlatFileMemoryService().search(query, 10);
    const markdown = new MarkdownVaultService().search(query).slice(0, 10).map(x => ({ preview: x.preview || x.raw || JSON.stringify(x).slice(0, 200), file: x.path }));
    return { type: 'memory_search', query, items: [...short, ...markdown].slice(0, 10) };
  }
}
module.exports = { ConversationService };
