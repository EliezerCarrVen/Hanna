const fs = require('fs'); const path = require('path');
const { paths } = require('../core/paths'); const { config } = require('../core/config'); const { ensureDir } = require('../utils/fsSafe');
const roles = ['root', 'admin', 'senior_dev', 'junior_dev', 'guest'];
class RbacService {
  active() { return { user: config.activeUser || 'local-root', role: (config.activeUser ? 'admin' : 'root'), fallback: !config.activeUser }; }
  createProfile(name, role = 'guest') { if (!roles.includes(role)) return { ok: false, status: 'invalid_role' }; ensureDir(paths.vaultDirs.perfiles); const file = path.join(paths.vaultDirs.perfiles, `${name}.json`); fs.writeFileSync(file, JSON.stringify({ name, role, created: new Date().toISOString() }, null, 2)); return { ok: true, file }; }
  list() { ensureDir(paths.vaultDirs.perfiles); return fs.readdirSync(paths.vaultDirs.perfiles).filter(x => x.endsWith('.json')); }
}
module.exports = { RbacService, roles };
