const https = require('https');
const http = require('http');
const { SecretFilterService } = require('./secretFilterService');
class LlmHttpClient {
  constructor() { this.filter = new SecretFilterService(); }
  requestJson(urlString, payload, headers = {}, timeout = 20000) {
    return new Promise(resolve => {
      const body = JSON.stringify(payload || {});
      const url = new URL(urlString);
      const lib = url.protocol === 'http:' ? http : https;
      const safeHeaders = { ...headers };
      const req = lib.request({ method: 'POST', hostname: url.hostname, port: url.port || undefined, path: url.pathname + url.search, timeout, headers: { 'Content-Type': 'application/json', 'Content-Length': Buffer.byteLength(body), ...safeHeaders } }, res => {
        let data = ''; res.on('data', c => data += c); res.on('end', () => { try { resolve({ ok: res.statusCode >= 200 && res.statusCode < 300, statusCode: res.statusCode, json: JSON.parse(data || '{}') }); } catch { resolve({ ok: false, statusCode: res.statusCode, error: 'invalid_json' }); } });
      });
      req.on('timeout', () => { req.destroy(); resolve({ ok: false, status: 'service_unavailable', error: 'timeout' }); });
      req.on('error', e => resolve({ ok: false, status: 'service_unavailable', error: this.filter.redact(e.message) }));
      req.write(body); req.end();
    });
  }
}
module.exports = { LlmHttpClient };
