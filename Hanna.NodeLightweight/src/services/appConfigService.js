const { config } = require('../core/config');
const { paths } = require('../core/paths');

class AppConfigService {
  getSnapshot() {
    return {
      dataRoot: paths.dataRoot,
      dryRun: config.dryRun,
      activeUser: config.activeUser,
      telegram: {
        status: process.env.TELEGRAM_BOT_TOKEN ? 'configured' : 'missing_configuration',
        adminRestricted: Boolean(process.env.TELEGRAM_ADMIN_ID)
      },
      llm: {
        groq: process.env.GROQ_API_KEY ? 'configured' : 'missing_configuration',
        gemini: process.env.GEMINI_API_KEY ? 'configured' : 'missing_configuration',
        openRouter: process.env.OPENROUTER_API_KEY ? 'configured' : 'missing_configuration',
        ollama: process.env.OLLAMA_BASE_URL ? 'configured' : 'missing_configuration'
      },
      limits: { maxTextBytes: config.maxTextBytes, maxReadEntries: config.maxReadEntries, maxFileBytes: config.maxFileBytes }
    };
  }
}
module.exports = { AppConfigService };
