const fs = require('fs');
const { paths } = require('../core/paths');
const { config } = require('../core/config');
const { ensureFile, readJsonl, appendJsonl } = require('../utils/fsSafe');
const { sha256, guid } = require('../utils/crypto');
const { nowIso } = require('../utils/text');
class AuditLogService {
  constructor(file = paths.auditLog) { this.file = file; ensureFile(file); }
  lastHash() { const last = readJsonl(this.file, 1)[0]; return last ? last.current_hash : 'GENESIS'; }
  hash(event) { const copy = { ...event }; delete copy.current_hash; return sha256(JSON.stringify(copy)); }
  record({ actor = config.activeUser, command = '', module = 'core', result = 'ok', dry_run = config.dryRun } = {}) {
    const event = { event_id: guid(), timestamp: nowIso(), actor, command, module, result, dry_run, previous_hash: this.lastHash() };
    event.current_hash = this.hash(event); appendJsonl(this.file, event); return event;
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
