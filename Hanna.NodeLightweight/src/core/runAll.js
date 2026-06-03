const { spawn } = require('child_process');
const path = require('path');
const { loadEnvFile } = require('../utils/envLoader');
loadEnvFile();
const dry = process.argv.includes('--dry-run');
const root = path.resolve(__dirname, '..', '..');
const entries = [
  ['core', ['src/core/hannaCore.js', dry ? '--dry-run' : '--once']],
  ['telegram', ['src/integrations/telegramBot.js', '--dry-run']],
  ['web', ['src/integrations/webCompactServer.js', '--self-test']]
];
(async () => {
  for (const [name, args] of entries) {
    await new Promise(resolve => {
      const child = spawn(process.execPath, args, { cwd: root, stdio: 'inherit', env: { ...process.env, HANNA_DRY_RUN: dry ? 'true' : process.env.HANNA_DRY_RUN } });
      child.on('exit', code => { console.log(`${name} finalizó con código ${code}`); resolve(); });
    });
  }
})();
