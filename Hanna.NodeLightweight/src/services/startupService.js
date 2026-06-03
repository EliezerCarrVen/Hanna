const { paths } = require('../core/paths');
const { ensureDir, ensureFile } = require('../utils/fsSafe');
class StartupService {
  ensureDataLayout() {
    ensureDir(paths.dataRoot); ensureDir(paths.vault); ensureDir(paths.runtime); ensureDir(paths.indexes); ensureDir(paths.logs);
    Object.values(paths.vaultDirs).forEach(ensureDir);
    ensureFile(paths.shortMemory); ensureFile(paths.currentSession); ensureFile(paths.lastSummary, '# Hanna rolling summary\n');
    ensureFile(paths.vaultIndex); ensureFile(paths.nasIndex); ensureFile(paths.codeCacheIndex);
    ensureFile(paths.lightweightLog); ensureFile(paths.auditLog); ensureFile(paths.securityLog);
    return { ok: true, dataRoot: paths.dataRoot };
  }
}
module.exports = { StartupService };
