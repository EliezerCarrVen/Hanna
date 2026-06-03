const { MemoryService } = require('./memoryService');
const { RollingSummaryService } = require('./rollingSummaryService');
class TieredMemoryService {
  search(query, limit = 10) { const result = new MemoryService().search(query); return { ...result, items: (result.items || []).slice(0, limit) }; }
  buildContext(query) { const hits = this.search(query, 3).items || []; return hits.map(x => x.text || x.preview || '').filter(Boolean).join('\n'); }
  summary() { return new RollingSummaryService().read(); }
}
module.exports = { TieredMemoryService };
