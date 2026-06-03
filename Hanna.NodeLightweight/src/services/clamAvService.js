const { commandExists, runAsync } = require('../utils/processRunner');
class ClamAvService {
  status() { return commandExists('clamdscan') ? { status: 'found' } : { status: 'missing_dependency', dependency: 'clamdscan (clamav-daemon)' }; }
  async scan(target) {
    if (!commandExists('clamdscan')) return { status: 'missing_dependency', dependency: 'clamdscan' };
    const r = await runAsync('clamdscan', ['--no-summary', '--fdpass', target], { timeout: 120000 });
    return { status: r.status === 0 ? 'clean' : 'findings_or_error', code: r.status, stdout: r.stdout.slice(0, 500) };
  }
}
module.exports = { ClamAvService };
