const fs = require('fs');
const path = require('path');
function ensureDir(dir) { fs.mkdirSync(dir, { recursive: true }); }
function ensureFile(file, initial = '') { ensureDir(path.dirname(file)); if (!fs.existsSync(file)) fs.writeFileSync(file, initial); }
function appendJsonl(file, object) { ensureFile(file); fs.appendFileSync(file, JSON.stringify(object) + '\n'); }
function readJsonl(file, limit = 50) {
  if (!fs.existsSync(file)) return [];
  const lines = fs.readFileSync(file, 'utf8').split(/\r?\n/).filter(Boolean);
  return lines.slice(Math.max(0, lines.length - limit)).map(line => { try { return JSON.parse(line); } catch { return { parse_error: true, raw: line }; } });
}
function walkFiles(root, options = {}) {
  const maxBytes = options.maxBytes || 1024 * 1024;
  const out = [];
  function visit(dir) {
    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
      const full = path.join(dir, entry.name);
      if (entry.isDirectory()) visit(full);
      else if (entry.isFile()) {
        const st = fs.statSync(full);
        if (st.size <= maxBytes) out.push(full);
      }
    }
  }
  if (fs.existsSync(root)) visit(root);
  return out;
}
module.exports = { ensureDir, ensureFile, appendJsonl, readJsonl, walkFiles };
