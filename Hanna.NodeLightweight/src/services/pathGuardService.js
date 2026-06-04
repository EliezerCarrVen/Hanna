const path = require('path');
const { paths } = require('../core/paths');

class PathGuardService {
  constructor(allowlist = []) {
    this.allowlist = [paths.dataRoot, ...allowlist].map(p => path.resolve(p));
    this.blockedName = /(\.env$|HannaEnv|appsettings.*(secret|local|Development)?\.json$|google_client_secret|token|password|secret)/i;
  }
  validate(target, action = 'write') {
    const raw = String(target || '');
    if (!raw) return { ok: false, code: 'missing_path' };
    if (raw.includes('..')) return { ok: false, code: 'path_traversal_blocked' };
    if (this.blockedName.test(raw)) return { ok: false, code: 'sensitive_path_blocked' };
    const resolved = path.resolve(raw);
    const ok = this.allowlist.some(base => resolved === base || resolved.startsWith(base + path.sep));
    return ok ? { ok: true, path: resolved, action } : { ok: false, code: 'outside_allowlist', path: resolved };
  }
  assert(target, action = 'write') {
    const check = this.validate(target, action);
    if (!check.ok) throw new Error(`PathGuard ${check.code}: ${target}`);
    return check.path;
  }
}
module.exports = { PathGuardService };
