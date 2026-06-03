const { paths } = require('./paths');
const { config } = require('./config');
const { getModules } = require('./moduleRegistry');
function status() {
  const llmConfigured = Boolean(process.env.GROQ_API_KEY || process.env.GEMINI_API_KEY || process.env.OPENROUTER_API_KEY || process.env.OLLAMA_BASE_URL);
  return {
    name: 'Hanna.NodeLightweight',
    runtime: 'node',
    mode: 'lightweight/i386',
    node: process.version,
    arch: process.arch,
    dataRoot: paths.dataRoot,
    dry_run: config.dryRun,
    modules: getModules().length,
    telegram: process.env.TELEGRAM_BOT_TOKEN ? 'configured' : 'missing_configuration',
    llm: llmConfigured ? 'configured' : 'missing_configuration'
  };
}
module.exports = { status };
