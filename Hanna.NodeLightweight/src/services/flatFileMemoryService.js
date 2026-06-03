const { paths } = require('../core/paths');
const { config } = require('../core/config');
const { JsonlStoreService } = require('./jsonlStoreService');
const { SecretFilterService } = require('./secretFilterService');
const { truncateText, nowIso } = require('../utils/text');
class FlatFileMemoryService {
  constructor(file = paths.shortMemory) { this.store = new JsonlStoreService(file); this.filter = new SecretFilterService(); }
  add(text, actor = config.activeUser) {
    const entry = { timestamp: nowIso(), actor, text: this.filter.redact(truncateText(text, config.maxTextBytes)) };
    return this.store.append(entry);
  }
  search(query, limit = config.maxReadEntries) {
    const q = String(query || '').toLowerCase();
    return this.store.read(limit * 5).filter(x => String(x.text || '').toLowerCase().includes(q)).slice(-limit);
  }
  recent(limit = config.maxReadEntries) { return this.store.read(limit); }
}
module.exports = { FlatFileMemoryService };
