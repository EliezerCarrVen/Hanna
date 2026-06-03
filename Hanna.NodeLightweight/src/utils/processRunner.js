const { spawnSync, spawn } = require('child_process');
function run(command, args = [], options = {}) {
  const result = spawnSync(command, args, { encoding: 'utf8', timeout: options.timeout || 5000, maxBuffer: options.maxBuffer || 1024 * 1024 });
  return { command, args, status: result.status, stdout: result.stdout || '', stderr: result.stderr || '', error: result.error ? result.error.message : '' };
}

function runAsync(command, args = [], options = {}) {
  return new Promise((resolve) => {
    const child = spawn(command, args, { timeout: options.timeout || 60000 });
    let stdout = '';
    let stderr = '';
    child.stdout.on('data', (data) => stdout += data.toString());
    child.stderr.on('data', (data) => stderr += data.toString());
    child.on('close', (code) => resolve({ command, args, status: code, stdout, stderr, error: '' }));
    child.on('error', (err) => resolve({ command, args, status: -1, stdout, stderr, error: err.message }));
  });
}

function commandExists(command) {
  const result = run('sh', ['-c', `command -v ${command}`], { timeout: 2000 });
  return result.status === 0 ? result.stdout.trim().split('\n')[0] : '';
}
module.exports = { run, runAsync, commandExists };
