const { ConversationService } = require('./conversationService');
const { FlatFileMemoryService } = require('./flatFileMemoryService');
const { MarkdownVaultService } = require('./markdownVaultService');
const { paths } = require('../core/paths');
class MemoryService {
  save(text, actor = 'local-root') { return new ConversationService().saveMemory(text, actor); }
  search(query) { return new ConversationService().searchMemory(query); }
  recent(limit = 10) { return { type: 'memory_search', items: new FlatFileMemoryService().recent(limit) }; }
  status() { const vault = new MarkdownVaultService().status(); return { status: 'ok', shortMemory: paths.shortMemory, markdownVault: vault.root, notes: vault.notes }; }
}
module.exports = { MemoryService };
