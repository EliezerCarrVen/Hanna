const http = require('http');
const { CommandRouter } = require('../cli/commandRouter');
const { status } = require('../core/status');
const { DoctorService } = require('../services/doctorService');
const { getModules } = require('../core/moduleRegistry');
const { ObsidianVaultService } = require('../services/obsidianVaultService');
const { EmotionStateService } = require('../services/emotionStateService');
const { LlmRouterService } = require('../services/llmRouterService');
const { SpotifyService } = require('../services/spotifyService');
const { AuditLogService } = require('../services/auditLogService');
const { SafeLogService } = require('../services/safeLogService');
const { loadEnvFile } = require('../utils/envLoader');

class WebCompactServer {
  constructor(options = {}) { loadEnvFile(); this.port = Number(options.port || process.env.HANNA_WEB_PORT || 8787); this.host = options.host || process.env.HANNA_WEB_HOST || '0.0.0.0'; this.router = options.router || new CommandRouter(); this.log = new SafeLogService(); }
  status() { return { status: 'configured', service: 'hanna-web', port: this.port, pages: this.pages().map(p => p.path) }; }
  pages() { return [ { path: '/', title: 'Dashboard' }, { path: '/chat', title: 'Chat' }, { path: '/status', title: 'Status' }, { path: '/doctor', title: 'Doctor' }, { path: '/memory', title: 'Memoria' }, { path: '/obsidian', title: 'Obsidian' }, { path: '/emotions', title: 'Emotions' }, { path: '/modules', title: 'Modules' }, { path: '/ai', title: 'AI' }, { path: '/spotify', title: 'Spotify' }, { path: '/telegram', title: 'Telegram' }, { path: '/logs', title: 'Logs' }, { path: '/settings', title: 'Settings' } ]; }
  async selfTest() { const chat = await this.router.run('busca que es un llm', { source: 'web', mode: 'human', dryRun: true }); return { status: 'ok', service: 'hanna-web', port: this.port, chatMentionsAiConfig: String(chat).includes('motor IA'), pages: this.pages().length }; }
  async data(pathname, body = {}) {
    if (pathname === '/api/health') return { ok: true, service: 'hanna-web' };
    if (pathname === '/api/status') return status();
    if (pathname === '/api/doctor') return await new DoctorService().run();
    if (pathname === '/api/modules') return getModules();
    if (pathname === '/api/emotions') return new EmotionStateService().getState();
    if (pathname === '/api/obsidian/status') return new ObsidianVaultService().status();
    if (pathname === '/api/chat' || pathname === '/api/command') return { response: await this.router.run(body.text || body.command || '', { source: 'web', mode: body.mode === 'json' ? 'json' : 'human' }) };
    if (pathname === '/api/memory/search') return await this.router.executeCommand(`/memoria buscar ${body.text || body.query || ''}`, { source: 'web' });
    if (pathname === '/api/memory/save') return await this.router.executeCommand(`/memoria guardar ${body.text || ''}`, { source: 'web' });
    return null;
  }
  async page(pathname) {
    const nav = this.pages().map(p => `<a href="${p.path}">${p.title}</a>`).join('');
    let title = this.pages().find(p => p.path === pathname)?.title || 'Dashboard';
    let content = '';
    if (pathname === '/' || pathname === '/status') content = `<pre>${escapeHtml(await this.router.run('/status', { source: 'web' }))}</pre>`;
    else if (pathname === '/chat') content = `<form method="post" action="/chat"><input name="text" autofocus placeholder="habla con Hanna"><button>Enviar</button></form>`;
    else if (pathname === '/doctor') content = `<pre>${escapeHtml(await this.router.run('/doctor', { source: 'web' }))}</pre>`;
    else if (pathname === '/memory') content = `<pre>${escapeHtml(await this.router.run('/memoria estado', { source: 'web' }))}</pre>`;
    else if (pathname === '/obsidian') content = `<pre>${escapeHtml(await this.router.run('/obsidian estado', { source: 'web' }))}</pre>`;
    else if (pathname === '/emotions') content = `<pre>${escapeHtml(await this.router.run('/emocion estado', { source: 'web' }))}</pre>`;
    else if (pathname === '/modules') content = `<pre>${escapeHtml(await this.router.run('/modulos', { source: 'web' }))}</pre>`;
    else if (pathname === '/ai') content = `<pre>${escapeHtml(await this.router.run('/ia estado', { source: 'web' }))}</pre>`;
    else if (pathname === '/spotify') content = `<pre>${escapeHtml(await this.router.run('/spotify estado', { source: 'web' }))}</pre>`;
    else if (pathname === '/telegram') content = `<pre>${escapeHtml(await this.router.run('/telegram estado', { source: 'web' }))}</pre>`;
    else if (pathname === '/logs') content = `<pre>${escapeHtml(JSON.stringify(new AuditLogService().verify(), null, 2))}</pre>`;
    else if (pathname === '/settings') content = `<pre>Configuración segura: secretos ocultos. HANNA_WEB_PORT=${this.port}</pre>`;
    else return null;
    return `<!doctype html><html><head><meta charset="utf-8"><title>Hanna ${title}</title><style>body{font-family:system-ui;margin:0;background:#10131a;color:#e8eefc}nav{display:flex;gap:8px;flex-wrap:wrap;padding:10px;background:#1d2433}a{color:#8fd3ff}.wrap{padding:14px;max-width:980px}input{width:70%;padding:10px}button{padding:10px}pre{white-space:pre-wrap;background:#151b27;padding:12px;border-radius:8px}</style></head><body><nav>${nav}</nav><main class="wrap"><h1>${title}</h1>${content}</main></body></html>`;
  }
  start() { const server = http.createServer(async (req, res) => { try { const url = new URL(req.url, `http://${req.headers.host}`); const body = await readBody(req); if (req.method === 'POST' && url.pathname === '/chat') { const text = new URLSearchParams(body).get('text') || ''; const response = await this.router.run(text, { source: 'web', mode: 'human' }); return writeHtml(res, await this.page('/chat') + `<pre>${escapeHtml(response)}</pre>`); } if (url.pathname.startsWith('/api/')) { const data = await this.data(url.pathname, parseJson(body)); return data ? writeJson(res, data) : writeJson(res, { status: 'not_found' }, 404); } const page = await this.page(url.pathname); return page ? writeHtml(res, page) : writeHtml(res, '<h1>404</h1>', 404); } catch (e) { this.log.write('web_compact_error', { error: e.message }); writeJson(res, { status: 'internal_error' }, 500); } }); server.listen(this.port, this.host, () => console.log(`hanna-web compacto en http://${this.host}:${this.port}`)); return server; }
}
function readBody(req) { return new Promise(resolve => { let data=''; req.on('data', c => data += c); req.on('end', () => resolve(data)); }); }
function parseJson(text) { try { return JSON.parse(text || '{}'); } catch { return {}; } }
function writeJson(res, obj, code = 200) { res.writeHead(code, { 'Content-Type': 'application/json; charset=utf-8' }); res.end(JSON.stringify(obj, null, 2)); }
function writeHtml(res, html, code = 200) { res.writeHead(code, { 'Content-Type': 'text/html; charset=utf-8' }); res.end(html); }
function escapeHtml(s) { return String(s || '').replace(/[&<>]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;'}[c])); }
async function main() { const server = new WebCompactServer(); if (process.argv.includes('--self-test')) { console.log(JSON.stringify(await server.selfTest(), null, 2)); return; } server.start(); }
if (require.main === module) main().catch(e => { console.error('web compact error:', e.message); process.exit(1); });
module.exports = { WebCompactServer };
