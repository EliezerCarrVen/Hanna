const http = require('http');
const { CommandRouter } = require('../cli/commandRouter');
const { status } = require('../core/status');
const { DoctorService } = require('../services/doctorService');
const { getModules } = require('../core/moduleRegistry');
const { SafeLogService } = require('../services/safeLogService');

class HannaHttpServerBase {
  constructor(options = {}) {
    this.name = options.name || 'hanna-http';
    this.port = Number(options.port || 0);
    this.host = options.host || '127.0.0.1';
    this.token = options.token || '';
    this.localOnlyIfNoToken = options.localOnlyIfNoToken !== false;
    this.router = options.router || new CommandRouter();
    this.log = options.log || new SafeLogService();
    this.routes = options.routes || [];
  }

  authStatus() { return this.token ? { status: 'configured' } : { status: this.localOnlyIfNoToken ? 'local_only' : 'missing_configuration' }; }
  health() { return { ok: true, service: this.name, auth: this.authStatus(), dry_run: true }; }

  async selfTest() {
    const chat = await this.router.run('hola', { source: this.name, mode: 'human', dryRun: true });
    return { status: 'ok', service: this.name, health: this.health(), chatIncludesHanna: String(chat).toLowerCase().includes('hanna') };
  }

  async route(method, pathname, body, context = {}) {
    if (method === 'GET' && (pathname === '/health' || pathname === '/api/health')) return this.health();
    if (method === 'GET' && (pathname === '/status' || pathname === '/api/status')) return status();
    if (method === 'GET' && pathname === '/doctor') return await new DoctorService().run();
    if (method === 'GET' && pathname === '/modules') return getModules();
    if (method === 'POST' && (pathname === '/chat' || pathname === '/api/chat')) {
      const text = body.text || body.message || body.input || '';
      const mode = body.mode === 'json' ? 'json' : 'human';
      return { response: await this.router.run(text, { ...context, source: this.name, mode, dryRun: true }) };
    }
    return { status: 'not_found' };
  }

  start() {
    const server = http.createServer(async (req, res) => {
      try {
        if (!this.authorized(req)) return writeJson(res, 401, { ok: false, status: 'unauthorized' });
        const body = await readJson(req);
        const url = new URL(req.url, `http://${req.headers.host || this.host}`);
        const result = await this.route(req.method, url.pathname, body, { userId: req.headers['x-hanna-user'] || 'http-local' });
        writeJson(res, result.status === 'not_found' ? 404 : 200, result);
      } catch (error) {
        this.log.write(`${this.name}_error`, { error: error.message });
        writeJson(res, 500, { ok: false, status: 'internal_error' });
      }
    });
    server.listen(this.port, this.host, () => console.log(`${this.name} escuchando en http://${this.host}:${this.port}`));
    return server;
  }

  authorized(req) {
    if (!this.token) return true;
    const header = req.headers.authorization || '';
    return header === `Bearer ${this.token}` || req.headers['x-hanna-token'] === this.token;
  }
}

function readJson(req) {
  return new Promise(resolve => {
    let data = '';
    req.on('data', chunk => { data += chunk; if (data.length > 65536) req.destroy(); });
    req.on('end', () => { try { resolve(data ? JSON.parse(data) : {}); } catch { resolve({}); } });
  });
}
function writeJson(res, code, payload) { res.writeHead(code, { 'Content-Type': 'application/json; charset=utf-8' }); res.end(JSON.stringify(payload, null, 2)); }
module.exports = { HannaHttpServerBase };
