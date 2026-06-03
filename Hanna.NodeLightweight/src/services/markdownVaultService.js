const fs = require('fs'); const path = require('path');
const { paths } = require('../core/paths');
const { PathGuardService } = require('./pathGuardService');
const { RipgrepSearchService } = require('./ripgrepSearchService');
const { SecretFilterService } = require('./secretFilterService');
const { ensureDir } = require('../utils/fsSafe');
const { slugify, nowIso, truncateText } = require('../utils/text');
class MarkdownVaultService {
  constructor() { this.guard = new PathGuardService(); this.searcher = new RipgrepSearchService(); this.filter = new SecretFilterService(); }
  createNote(area, title, body = '') {
    const dir = paths.vaultDirs[area] || path.join(paths.vault, slugify(area)); ensureDir(dir);
    const file = this.guard.assert(path.join(dir, `${Date.now()}-${slugify(title)}.md`));
    const content = `---\ntitle: ${this.filter.redact(title)}\ncreated: ${nowIso()}\narea: ${area}\n---\n\n${this.filter.redact(truncateText(body, 8192))}\n`;
    fs.writeFileSync(file, content); return { ok: true, file };
  }
  list(area = '') {
    const dir = area && paths.vaultDirs[area] ? paths.vaultDirs[area] : paths.vault;
    const out = [];
    function visit(d) { if (!fs.existsSync(d)) return; for (const e of fs.readdirSync(d, { withFileTypes: true })) { const f = path.join(d, e.name); if (e.isDirectory()) visit(f); else if (e.isFile() && e.name.endsWith('.md')) out.push(f); } }
    visit(dir); return out;
  }
  search(query) { return this.searcher.search(paths.vault, query); }
  status() { return { root: paths.vault, notes: this.list().length, areas: Object.keys(paths.vaultDirs) }; }
}
module.exports = { MarkdownVaultService };
