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
  systemConfig: path.join(dataRoot, 'runtime', 'config.json'),
  lastSummary: path.join(dataRoot, 'runtime', 'last_summary.md'),
  vaultIndex: path.join(dataRoot, 'indexes', 'vault_index.jsonl'),
  nasIndex: path.join(dataRoot, 'indexes', 'nas_index.jsonl'),
  codeCacheIndex: path.join(dataRoot, 'indexes', 'code_cache_index.jsonl'),
  conversacionesJsonl:      path.join(dataRoot, 'runtime', 'conversaciones.jsonl'),
  mensajesJsonl:            path.join(dataRoot, 'runtime', 'mensajes.jsonl'),
  transcripcionesAudioJsonl: path.join(dataRoot, 'runtime', 'transcripciones_audio.jsonl'),
  analisisPantallaJsonl:    path.join(dataRoot, 'runtime', 'analisis_pantalla.jsonl'),
  accionesAgenteJsonl:      path.join(dataRoot, 'runtime', 'acciones_agente.jsonl'),
  auditLog: path.join(dataRoot, 'logs', 'audit.log'),
  lightweightLog: path.join(dataRoot, 'logs', 'lightweight.log'),
  securityLog: path.join(dataRoot, 'logs', 'security.log')
};

paths.vaultDirs = ['memoria', 'proyectos', 'sistema', 'inventario', 'tareas', 'codigo_cache', 'bovedas', 'perfiles', 'empresa', 'conversaciones', 'graphifyy', 'conocimiento', 'resumenes']
  .reduce((acc, name) => ({ ...acc, [name]: path.join(paths.vault, name) }), {});

module.exports = { paths };
