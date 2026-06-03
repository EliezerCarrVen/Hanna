const { spawnSync } = require('child_process');
function run(command, args = [], options = {}) {
  const result = spawnSync(command, args, { encoding: 'utf8', timeout: options.timeout || 5000, maxBuffer: options.maxBuffer || 1024 * 1024 });
  return { command, args, status: result.status, stdout: result.stdout || '', stderr: result.stderr || '', error: result.error ? result.error.message : '' };
}
function commandExists(command) {
  const result = run('sh', ['-c', `command -v ${command}`], { timeout: 2000 });
  return result.status === 0 ? result.stdout.trim().split('\n')[0] : '';
}
module.exports = { run, commandExists };
