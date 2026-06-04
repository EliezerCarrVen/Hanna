const fs = require('fs');
const path = require('path');
const { paths } = require('../core/paths');
const { ensureDir } = require('../utils/fsSafe');
const { ObsidianVaultService } = require('./obsidianVaultService');
class KnowledgeIndexService {
  constructor(file = path.join(paths.indexes, 'knowledge_index.jsonl')) { this.file = file; this.vault = new ObsidianVaultService(); }
  index() { ensureDir(path.dirname(this.file)); const notes = this.vault.list().map(file => ({ timestamp: new Date().toISOString(), file, relative: path.relative(this.vault.root, file), bytes: fs.statSync(file).size })); fs.writeFileSync(this.file, notes.map(x => JSON.stringify(x)).join('\n') + (notes.length ? '\n' : '')); return { status: 'ok', file: this.file, notes: notes.length }; }
  status() { return { status: fs.existsSync(this.file) ? 'ok' : 'missing_index', file: this.file, notes: fs.existsSync(this.file) ? fs.readFileSync(this.file, 'utf8').split('\n').filter(Boolean).length : 0 }; }
}
module.exports = { KnowledgeIndexService };
