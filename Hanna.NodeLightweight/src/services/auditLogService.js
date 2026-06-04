const { paths } = require('../core/paths');
const { config } = require('../core/config');
const { ensureFile, readJsonl, appendJsonl } = require('../utils/fsSafe');
const { sha256, guid } = require('../utils/crypto');
const { nowIso } = require('../utils/text');
const { ZeroLeakSanitizerService } = require('./zeroLeakSanitizerService');
class AuditLogService {
  constructor(file = paths.auditLog) { this.file = file; this.sanitizer = new ZeroLeakSanitizerService(); ensureFile(file); }
  lastHash() { const last = readJsonl(this.file, 1)[0]; return last ? last.current_hash : 'GENESIS'; }
  hash(event) { const copy = { ...event }; delete copy.current_hash; return sha256(JSON.stringify(copy)); }
  record(eventInput = {}) {
    const sanitized = this.sanitizeEvent(eventInput);
    const event = {
      event_id: guid(),
      timestamp: nowIso(),
      actor: sanitized.actor || config.activeUser,
      command: sanitized.command || '',
      module: sanitized.module || 'core',
      result: sanitized.result || 'ok',
      dry_run: sanitized.dry_run !== undefined ? sanitized.dry_run : config.dryRun,
      previous_hash: this.lastHash(),
      ...sanitized
    };
    event.current_hash = this.hash(event); appendJsonl(this.file, event); return event;
  }
  sanitizeEvent(input) {
    const out = {};
    for (const [key, value] of Object.entries(input || {})) {
      if (value === undefined) continue;
      if (/token|secret|password|api[_-]?key/i.test(key)) out[key] = '[REDACTED]';
      else if (typeof value === 'string') out[key] = this.sanitizer.sanitize(value);
      else out[key] = value;
    }
    return out;
  }
  verify() {
    const events = readJsonl(this.file, 100000); let prev = 'GENESIS';
    for (let i = 0; i < events.length; i++) {
      const e = events[i]; const expected = this.hash(e);
      if (e.previous_hash !== prev || e.current_hash !== expected) return { ok: false, status: 'tampered', index: i, event_id: e.event_id };
      prev = e.current_hash;
    }
    return { ok: true, status: 'valid', events: events.length };
  }
}
module.exports = { AuditLogService };
