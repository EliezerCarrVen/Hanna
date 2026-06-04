'use strict';

const https = require('https');
const http = require('http');
const { SecretFilterService } = require('./secretFilterService');

class LlmHttpClient {
  constructor() {
    this.filter = new SecretFilterService();
  }

  requestJson(urlString, payload, headers = {}, timeout = 20000, method = 'POST') {
    return new Promise(resolve => {
      let body = '';
      let url;

      try {
        body = method === 'GET' ? '' : JSON.stringify(payload || {});
        url = new URL(urlString);
      } catch (error) {
        resolve({ ok: false, status: 'client_error', error: this.filter.redact(error.message) });
        return;
      }

      const lib = url.protocol === 'http:' ? http : https;
      const safeHeaders = { ...headers };
      const requestHeaders = method === 'GET'
        ? safeHeaders
        : { 'Content-Type': 'application/json', 'Content-Length': Buffer.byteLength(body), ...safeHeaders };

      const req = lib.request(
        {
          method,
          hostname: url.hostname,
          port: url.port || undefined,
          path: url.pathname + url.search,
          timeout,
          headers: requestHeaders
        },
        res => {
          let data = '';
          res.setEncoding('utf8');
          res.on('data', chunk => { data += chunk; });
          res.on('end', () => {
            let json = {};
            try {
              json = data ? JSON.parse(data) : {};
            } catch {
              resolve({
                ok: false,
                status: 'invalid_json',
                statusCode: res.statusCode,
                error: 'invalid_json',
                raw: this.filter.redact(data.slice(0, 300))
              });
              return;
            }

            resolve({
              ok: res.statusCode >= 200 && res.statusCode < 300,
              statusCode: res.statusCode,
              json,
              raw: this.filter.redact(data.slice(0, 300))
            });
          });
        }
      );

      req.on('timeout', () => {
        req.destroy();
        resolve({ ok: false, status: 'service_unavailable', error: 'timeout' });
      });

      req.on('error', error => {
        resolve({ ok: false, status: 'service_unavailable', error: this.filter.redact(error.message) });
      });

      if (body) req.write(body);
      req.end();
    });
  }

  getJson(urlString, headers = {}, timeout = 8000) {
    return this.requestJson(urlString, null, headers, timeout, 'GET');
  }
}

module.exports = { LlmHttpClient };
