const { StartupService } = require('../services/startupService');
const { DoctorService } = require('../services/doctorService');
const { ObsidianVaultService } = require('../services/obsidianVaultService');
const { EmotionStateService } = require('../services/emotionStateService');
const { LlmRouterService } = require('../services/llmRouterService');
const { AuditLogService } = require('../services/auditLogService');
const { loadEnvFile } = require('../utils/envLoader');

class HannaCore {
  constructor() { loadEnvFile(); this.startup = new StartupService(); this.obsidian = new ObsidianVaultService(); this.emotions = new EmotionStateService(); this.llm = new LlmRouterService(); this.audit = new AuditLogService(); }
  async start(options = {}) { this.startup.ensureDataLayout(); this.obsidian.ensureLayout(); this.emotions.update({ mode: 'core_headless', last_reaction: 'core activo' }); const doctor = await new DoctorService().run(); this.audit.record({ command: 'hanna-core:start', module: 'core', result: doctor.status, dry_run: options.dryRun !== false }); return { status: 'ok', service: 'hanna-core', doctor, llm: this.llm.status(), emotions: this.emotions.getState() }; }
}
async function main() { const core = new HannaCore(); const state = await core.start({ dryRun: process.argv.includes('--dry-run') }); console.log(`hanna-core activo: ${state.status}. Telegram y web deben correr como servicios separados.`); if (process.argv.includes('--once') || process.argv.includes('--dry-run')) return; setInterval(() => {}, 60_000); }
if (require.main === module) main().catch(e => { console.error('hanna-core error:', e.message); process.exit(1); });
module.exports = { HannaCore };
