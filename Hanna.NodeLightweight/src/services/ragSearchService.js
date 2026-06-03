const { ObsidianVaultService } = require('./obsidianVaultService');
class RagSearchService {
  constructor() { this.vault = new ObsidianVaultService(); }
  search(query, limit = 5) { const hits = this.vault.search(query, limit); return { status: 'ok', query, hits, hasContext: hits.length > 0, context: hits.map(h => `- ${h.relative || h.path}: ${h.preview}`).join('\n') }; }
}
module.exports = { RagSearchService };
