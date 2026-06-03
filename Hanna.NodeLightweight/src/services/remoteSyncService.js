const http = require('http');
const https = require('https');
const { config } = require('../core/config');
const { SecretFilterService } = require('./secretFilterService');

class RemoteSyncService {
  constructor(options = {}) {
    this.syncUrl = options.syncUrl || process.env.HANNA_REMOTE_SYNC_URL || '';
    this.timeout = options.timeout || Number(process.env.HANNA_REMOTE_SYNC_TIMEOUT_MS || 5000);
    this.filter = new SecretFilterService();
  }

  status() {
    return this.syncUrl ? { status: 'configured', target: this.safeTarget() } : { status: 'missing_configuration' };
  }

  async syncPayload(collection, payload) {
    if (!this.syncUrl) return { ok: false, status: 'missing_configuration' };
    return new Promise((resolve) => {
      try {
        const url = new URL(this.syncUrl);
        const lib = url.protocol === 'https:' ? https : http;
        const data = JSON.stringify({ collection, data: this.sanitize(payload), timestamp: new Date().toISOString(), source: 'hanna-node-lightweight' });
        const req = lib.request({
          method: 'POST',
          hostname: url.hostname,
          port: url.port || undefined,
          path: `${url.pathname}${url.search}`,
          headers: { 'Content-Type': 'application/json', 'Content-Length': Buffer.byteLength(data) },
          timeout: this.timeout
        }, (res) => {
          res.resume();
          resolve({ ok: res.statusCode >= 200 && res.statusCode < 300, status: res.statusCode });
        });
        req.on('error', (e) => resolve({ ok: false, status: 'service_unavailable', error: this.filter.redact(e.message) }));
        req.on('timeout', () => { req.destroy(); resolve({ ok: false, status: 'service_unavailable', error: 'timeout' }); });
        req.write(data);
        req.end();
      } catch (e) {
        resolve({ ok: false, status: 'invalid_configuration', error: this.filter.redact(e.message) });
      }
    });
  }

  sanitize(payload) {
    if (typeof payload === 'string') return this.filter.redact(payload);
    return JSON.parse(JSON.stringify(payload || {}, (key, value) => /token|secret|password|api[_-]?key/i.test(key) ? '[REDACTED]' : value));
  }

  safeTarget() {
    try { const url = new URL(this.syncUrl); return `${url.protocol}//${url.host}${url.pathname}`; }
    catch { return 'invalid_url'; }
  }
}

module.exports = { RemoteSyncService };
