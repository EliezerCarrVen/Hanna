const { commandExists, run } = require('../utils/processRunner');
class ClamAvService { status() { return commandExists('clamscan') ? { status: 'found' } : { status: 'missing_dependency', dependency: 'clamscan' }; } scan(target) { if (!commandExists('clamscan')) return { status: 'missing_dependency', dependency: 'clamscan' }; const r = run('clamscan', ['--no-summary', target], { timeout: 60000 }); return { status: r.status === 0 ? 'clean_or_no_virus_found' : 'scan_completed_with_findings_or_error', code: r.status, stdout: r.stdout, stderr: r.stderr }; } }
module.exports = { ClamAvService };
