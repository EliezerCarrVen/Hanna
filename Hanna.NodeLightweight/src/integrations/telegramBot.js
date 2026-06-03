const https = require('https');
const { StartupService } = require('../services/startupService');
const { CommandRouter } = require('../cli/commandRouter');
const { AuditLogService } = require('../services/auditLogService');
const { SafeLogService } = require('../services/safeLogService');
const { TelegramSecurityService } = require('../services/telegramSecurityService');

class TelegramBotIntegration {
  constructor(options = {}) {
    this.token = options.token || process.env.TELEGRAM_BOT_TOKEN || '';
    this.adminId = options.adminId || process.env.TELEGRAM_ADMIN_ID || '';
    this.dryRun = options.dryRun !== undefined ? options.dryRun : process.env.HANNA_TELEGRAM_DRY_RUN === 'true';
    this.router = options.router || new CommandRouter();
    this.audit = options.audit || new AuditLogService();
    this.log = options.log || new SafeLogService();
    this.security = options.security || new TelegramSecurityService(this.adminId);
    this.offset = 0;
    this.stopped = false;
  }

  status() {
    if (!this.token) return { status: 'missing_configuration', service: 'telegram', message: 'TELEGRAM_BOT_TOKEN no está configurado' };
    return { status: this.dryRun ? 'dry_run' : 'configured', service: 'telegram', admin_restricted: Boolean(this.adminId) };
  }

  async start() {
    new StartupService().ensureDataLayout();
    const current = this.status();
    if (current.status === 'missing_configuration') { console.log('Telegram: missing_configuration (TELEGRAM_BOT_TOKEN).'); return current; }
    if (this.dryRun) { console.log('Telegram dry-run activo. No se abrirá long polling.'); return current; }
    console.log('Telegram activo como canal principal de Hanna.NodeLightweight.');
    while (!this.stopped) {
      try { await this.pollOnce(); } catch (error) { this.log.write('telegram_poll_error', { error: error.message }); await sleep(1500); }
    }
    return { status: 'stopped' };
  }

  async pollOnce() {
    const data = await this.api('getUpdates', { timeout: 25, offset: this.offset + 1, allowed_updates: ['message'] });
    for (const update of data.result || []) {
      this.offset = Math.max(this.offset, update.update_id);
      if (update.message && typeof update.message.text === 'string') await this.handleMessage(update.message);
    }
  }

  async handleMessage(message) {
    const chatId = message.chat && message.chat.id;
    const from = message.from || {};
    const text = message.text || '';
    const context = { source: 'telegram', chatId, userId: from.id, username: from.username || from.first_name || '', mode: 'human' };
    const auth = this.security.authorize(text, from.id);
    if (!auth.ok) {
      await this.sendMessage(chatId, 'Ese comando está restringido al administrador configurado.');
      this.audit.record({ ...context, command: text, normalized_command: text, module: 'telegram', result: 'blocked_non_admin', dry_run: true });
      return;
    }
    let response;
    try { response = await this.router.run(text, context); }
    catch { response = 'Tuve un error interno procesando eso. Ya lo registré en auditoría. Prueba con ‘diagnóstico’ o ‘ayuda’.'; }
    await this.sendMessage(chatId, response);
    this.audit.record({ ...context, command: text, normalized_command: text, module: 'telegram', result: 'sent', dry_run: true });
  }

  async sendMessage(chatId, text) {
    if (this.dryRun) return { ok: true, dry_run: true, chatId, text };
    return this.api('sendMessage', { chat_id: chatId, text: String(text || '').slice(0, 3900) });
  }

  api(method, payload) {
    return new Promise((resolve, reject) => {
      const body = JSON.stringify(payload || {});
      const req = https.request({ hostname: 'api.telegram.org', path: `/bot${this.token}/${method}`, method: 'POST', headers: { 'Content-Type': 'application/json', 'Content-Length': Buffer.byteLength(body) }, timeout: 30000 }, res => {
        let data = ''; res.on('data', chunk => data += chunk); res.on('end', () => { try { const json = JSON.parse(data || '{}'); if (!json.ok) reject(new Error(json.description || 'telegram_api_error')); else resolve(json); } catch (e) { reject(e); } });
      });
      req.on('error', reject); req.write(body); req.end();
    });
  }
}
function sleep(ms) { return new Promise(resolve => setTimeout(resolve, ms)); }
async function main() { const dryRun = process.argv.includes('--dry-run'); await new TelegramBotIntegration({ dryRun }).start(); }
if (require.main === module) main().catch(e => { console.error('Telegram integration error:', e.message); process.exit(1); });
module.exports = { TelegramBotIntegration };
