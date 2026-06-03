const path = require('path');

const projectRoot = path.resolve(__dirname, '..', '..');
const repoRoot = path.resolve(projectRoot, '..');
const dataRoot = path.resolve(process.env.HANNA_DATA_DIR || path.join(repoRoot, 'HannaData'));

const paths = {
  projectRoot,
  repoRoot,
  dataRoot,
  vault: path.join(dataRoot, 'vault'),
  runtime: path.join(dataRoot, 'runtime'),
  indexes: path.join(dataRoot, 'indexes'),
  logs: path.join(dataRoot, 'logs'),
  shortMemory: path.join(dataRoot, 'runtime', 'short_memory.jsonl'),
  currentSession: path.join(dataRoot, 'runtime', 'current_session.jsonl'),
  lastSummary: path.join(dataRoot, 'runtime', 'last_summary.md'),
  vaultIndex: path.join(dataRoot, 'indexes', 'vault_index.jsonl'),
  nasIndex: path.join(dataRoot, 'indexes', 'nas_index.jsonl'),
  codeCacheIndex: path.join(dataRoot, 'indexes', 'code_cache_index.jsonl'),
  auditLog: path.join(dataRoot, 'logs', 'audit.log'),
  lightweightLog: path.join(dataRoot, 'logs', 'lightweight.log'),
  securityLog: path.join(dataRoot, 'logs', 'security.log')
};

paths.vaultDirs = ['memoria', 'proyectos', 'sistema', 'inventario', 'tareas', 'codigo_cache', 'bovedas', 'perfiles', 'empresa']
  .reduce((acc, name) => ({ ...acc, [name]: path.join(paths.vault, name) }), {});

module.exports = { paths };
