const fs = require('fs');
const path = require('path');
const { paths } = require('../core/paths');

function loadEnvFile(file = path.join(paths.projectRoot, '.env')) {
  if (!fs.existsSync(file)) return { loaded: false, file };
  const text = fs.readFileSync(file, 'utf8');
  for (const raw of text.split(/\r?\n/)) {
    const line = raw.trim();
    if (!line || line.startsWith('#') || !line.includes('=')) continue;
    const idx = line.indexOf('=');
    const key = line.slice(0, idx).trim();
    let value = line.slice(idx + 1).trim();
    if ((value.startsWith('"') && value.endsWith('"')) || (value.startsWith("'") && value.endsWith("'"))) value = value.slice(1, -1);
    if (key && process.env[key] === undefined) process.env[key] = value;
  }
  return { loaded: true, file };
}
module.exports = { loadEnvFile };
