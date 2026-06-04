const fs = require('fs');
const path = require('path');
const { paths } = require('../core/paths');
const { ensureDir } = require('../utils/fsSafe');
const { slugify, nowIso, truncateText } = require('../utils/text');
const { SecretFilterService } = require('./secretFilterService');
const { RipgrepSearchService } = require('./ripgrepSearchService');

class ObsidianVaultService {
  constructor(options = {}) {
    this.root = path.resolve(options.root || process.env.HANNA_OBSIDIAN_VAULT_PATH || paths.vault);
    this.filter = new SecretFilterService();
    this.searcher = new RipgrepSearchService();
    this.areas = ['memoria', 'proyectos', 'sistema', 'conversaciones', 'graphifyy', 'conocimiento', 'resumenes'];
  }
  ensureLayout() { ensureDir(this.root); for (const area of this.areas) ensureDir(path.join(this.root, area)); return this.status(); }
  status() { this.ensureDirsOnly(); return { status: 'ok', root: this.root, areas: this.areas, notes: this.list().length, source: process.env.HANNA_OBSIDIAN_VAULT_PATH ? 'HANNA_OBSIDIAN_VAULT_PATH' : 'HannaData/vault' }; }
  ensureDirsOnly() { ensureDir(this.root); for (const area of this.areas) ensureDir(path.join(this.root, area)); }
  safeArea(area) { return this.areas.includes(area) ? area : 'conocimiento'; }
  createNote(title, body, options = {}) {
    this.ensureLayout();
    const area = this.safeArea(options.area || 'conocimiento');
    const cleanTitle = this.filter.redact(String(title || 'nota').slice(0, 120));
    const cleanBody = this.filter.redact(truncateText(String(body || ''), 12000));
    const tags = (options.tags || ['hanna', area]).map(t => String(t).replace(/[^a-zA-Z0-9_-]/g, '')).filter(Boolean);
    const file = path.join(this.root, area, `${Date.now()}-${slugify(cleanTitle)}.md`);
    const frontmatter = ['---', `title: ${cleanTitle}`, `created: ${nowIso()}`, `area: ${area}`, `tags: [${tags.join(', ')}]`, `summary: ${cleanBody.replace(/\s+/g, ' ').slice(0, 180)}`, '---', ''].join('\n');
    fs.writeFileSync(file, `${frontmatter}\n${cleanBody}\n`);
    return { status: 'ok', file, title: cleanTitle, area, tags, summary: cleanBody.slice(0, 180) };
  }
  readNote(file) { const resolved = path.resolve(file); if (!resolved.startsWith(this.root)) return { status: 'blocked', message: 'path_outside_obsidian' }; return { status: 'ok', file: resolved, text: fs.readFileSync(resolved, 'utf8') }; }
  list() { const out = []; const visit = dir => { if (!fs.existsSync(dir)) return; for (const ent of fs.readdirSync(dir, { withFileTypes: true })) { const f = path.join(dir, ent.name); if (ent.isDirectory()) visit(f); else if (ent.isFile() && ent.name.endsWith('.md')) out.push(f); } }; visit(this.root); return out; }
  search(query, limit = 10) { this.ensureLayout(); return this.searcher.search(this.root, query, { maxCount: limit }).map(item => ({ ...item, source: 'obsidian', relative: path.relative(this.root, item.path || '') })).slice(0, limit); }
}
module.exports = { ObsidianVaultService };
