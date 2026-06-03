const fs = require('fs');
const { run, commandExists } = require('../utils/processRunner');
const { walkFiles } = require('../utils/fsSafe');
class RipgrepSearchService {
  search(root, query, options = {}) {
    const q = String(query || '');
    if (!q) return [];
    if (commandExists('rg')) {
      const r = run('rg', ['--json', '--max-count', String(options.maxCount || 20), q, root], { timeout: 7000 });
      if (r.status === 0 || r.stdout) {
        return r.stdout.split('\n').filter(Boolean).flatMap(line => {
          try {
            const item = JSON.parse(line);
            if (item.type !== 'match') return [];
            return [{ type: 'match', path: item.data.path.text, line: item.data.line_number, preview: item.data.lines.text.trim() }];
          } catch { return []; }
        });
      }
    }
    return walkFiles(root, { maxBytes: options.maxFileBytes || 1024 * 1024 }).flatMap(file => {
      const text = fs.readFileSync(file, 'utf8');
      return text.toLowerCase().includes(q.toLowerCase()) ? [{ type: 'match', path: file, preview: text.slice(0, 240) }] : [];
    }).slice(0, options.maxCount || 20);
  }
}
module.exports = { RipgrepSearchService };
