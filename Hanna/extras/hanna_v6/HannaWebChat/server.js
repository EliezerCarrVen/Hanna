const http = require('http');
const fs = require('fs');
const path = require('path');
const { URL } = require('url');

const root = __dirname;
const publicDir = path.join(root, 'public');
const configPath = path.join(root, 'config', 'config.json');

function loadConfig() {
  const raw = fs.readFileSync(configPath, 'utf8');
  return JSON.parse(raw);
}

const config = loadConfig();

function sendJson(res, code, data) {
  const body = JSON.stringify(data, null, 2);
  res.writeHead(code, {
    'Content-Type': 'application/json; charset=utf-8',
    'Cache-Control': 'no-store'
  });
  res.end(body);
}

function sendText(res, code, text, type = 'text/plain; charset=utf-8') {
  res.writeHead(code, { 'Content-Type': type, 'Cache-Control': 'no-store' });
  res.end(text);
}

function getBody(req) {
  return new Promise((resolve, reject) => {
    let data = '';
    req.on('data', chunk => {
      data += chunk;
      if (data.length > 1024 * 1024) {
        req.destroy();
        reject(new Error('Cuerpo demasiado grande'));
      }
    });
    req.on('end', () => {
      if (!data.trim()) return resolve({});
      try { resolve(JSON.parse(data)); }
      catch (err) { reject(new Error('JSON inválido: ' + err.message)); }
    });
    req.on('error', reject);
  });
}

function requestJson(targetUrl, payload, timeoutMs) {
  return new Promise((resolve, reject) => {
    const u = new URL(targetUrl);
    const body = JSON.stringify(payload || {});
    const options = {
      hostname: u.hostname,
      port: u.port || (u.protocol === 'https:' ? 443 : 80),
      path: u.pathname + u.search,
      method: 'POST',
      headers: {
        'Content-Type': 'application/json; charset=utf-8',
        'Content-Length': Buffer.byteLength(body)
      },
      timeout: timeoutMs || 60000
    };

    const client = u.protocol === 'https:' ? require('https') : require('http');
    const req = client.request(options, res => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        let parsed = data;
        try { parsed = data ? JSON.parse(data) : {}; } catch (_) {}
        if (res.statusCode >= 200 && res.statusCode < 300) {
          resolve({ ok: true, status: res.statusCode, data: parsed, url: targetUrl });
        } else {
          reject(new Error(`HTTP ${res.statusCode} en ${targetUrl}: ${typeof parsed === 'string' ? parsed : JSON.stringify(parsed)}`));
        }
      });
    });
    req.on('error', reject);
    req.on('timeout', () => {
      req.destroy(new Error('Timeout consultando ' + targetUrl));
    });
    req.write(body);
    req.end();
  });
}

function requestGet(targetUrl, timeoutMs) {
  return new Promise((resolve, reject) => {
    const u = new URL(targetUrl);
    const options = {
      hostname: u.hostname,
      port: u.port || (u.protocol === 'https:' ? 443 : 80),
      path: u.pathname + u.search,
      method: 'GET',
      timeout: timeoutMs || 8000
    };
    const client = u.protocol === 'https:' ? require('https') : require('http');
    const req = client.request(options, res => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        let parsed = data;
        try { parsed = data ? JSON.parse(data) : {}; } catch (_) {}
        if (res.statusCode >= 200 && res.statusCode < 300) resolve({ ok: true, status: res.statusCode, data: parsed, url: targetUrl });
        else reject(new Error(`HTTP ${res.statusCode} en ${targetUrl}`));
      });
    });
    req.on('error', reject);
    req.on('timeout', () => req.destroy(new Error('Timeout consultando ' + targetUrl)));
    req.end();
  });
}

async function tryPostEngineEndpoints(paths, payload) {
  const errors = [];
  for (const endpoint of paths) {
    const base = endpoint.startsWith('/api/mobile') ? config.mobileApiBase : (config.adminApiBase || config.mobileApiBase);
    const target = base.replace(/\/$/, '') + endpoint;
    try {
      return await requestJson(target, payload, config.timeoutMs);
    } catch (err) {
      errors.push(err.message);
    }
  }
  throw new Error(errors.join('\n'));
}

async function tryPostPhaseEndpoints(paths, payload) {
  const errors = [];
  for (const endpoint of (paths || [])) {
    const base = endpoint.startsWith('/api/mobile') ? config.mobileApiBase : (config.adminApiBase || config.mobileApiBase);
    const target = base.replace(/\/$/, '') + endpoint;
    try {
      return await requestJson(target, payload, config.timeoutMs);
    } catch (err) {
      errors.push(err.message);
    }
  }
  throw new Error(errors.join('\n'));
}

async function tryPostEndpoints(paths, payload) {
  const errors = [];
  for (const endpoint of paths) {
    const target = config.mobileApiBase.replace(/\/$/, '') + endpoint;
    try {
      return await requestJson(target, payload, config.timeoutMs);
    } catch (err) {
      errors.push(err.message);
    }
  }
  throw new Error(errors.join('\n'));
}

async function tryGetEndpoints(paths) {
  const errors = [];
  for (const endpoint of paths) {
    const target = config.mobileApiBase.replace(/\/$/, '') + endpoint;
    try {
      return await requestGet(target, 8000);
    } catch (err) {
      errors.push(err.message);
    }
  }
  throw new Error(errors.join('\n'));
}

function contentType(filePath) {
  const ext = path.extname(filePath).toLowerCase();
  if (ext === '.html') return 'text/html; charset=utf-8';
  if (ext === '.css') return 'text/css; charset=utf-8';
  if (ext === '.js') return 'application/javascript; charset=utf-8';
  if (ext === '.json') return 'application/json; charset=utf-8';
  return 'application/octet-stream';
}

function serveStatic(req, res, pathname) {
  let safePath = pathname === '/' ? '/index.html' : pathname;
  safePath = safePath.replace(/\.\./g, '');
  const filePath = path.join(publicDir, safePath);
  if (!filePath.startsWith(publicDir)) return sendText(res, 403, 'Prohibido');
  if (!fs.existsSync(filePath) || fs.statSync(filePath).isDirectory()) return sendText(res, 404, 'No encontrado');
  const data = fs.readFileSync(filePath);
  res.writeHead(200, { 'Content-Type': contentType(filePath), 'Cache-Control': 'no-store' });
  res.end(data);
}

const server = http.createServer(async (req, res) => {
  const url = new URL(req.url, `http://${req.headers.host}`);
  try {
    if (req.method === 'GET' && url.pathname === '/api/config') {
      return sendJson(res, 200, {
        mobileApiBase: config.mobileApiBase,
        engines: config.engines,
        phases: config.phases,
        version: config.webchatVersion || "6.1"
      });
    }

    if (req.method === 'GET' && url.pathname === '/api/state') {
      try {
        const state = await tryGetEndpoints(config.stateEndpoints);
        return sendJson(res, 200, state);
      } catch (err) {
        return sendJson(res, 200, { ok: false, warning: 'No se pudo leer estado de Hanna', detail: err.message });
      }
    }

    if (req.method === 'POST' && url.pathname === '/api/set-engine') {
      const body = await getBody(req);
      const engine = body.engine || body.motor || body.mode;
      if (!engine) return sendJson(res, 400, { ok: false, error: 'Falta engine' });
      const pairingToken = body.pairingToken || body.pairing_token || body.token || '';
      const payloads = [
        { mode: engine, engine, motor: engine, pairingToken, token: pairingToken },
        { message: `Hanna usa ${engine}`, text: `Hanna usa ${engine}`, pairingToken, token: pairingToken }
      ];
      const errors = [];
      for (const payload of payloads) {
        try {
          const result = await tryPostEngineEndpoints(config.engineEndpoints, payload);
          return sendJson(res, 200, { ok: true, result });
        } catch (err) {
          errors.push(err.message);
        }
      }
      return sendJson(res, 502, { ok: false, error: 'No se pudo cambiar motor', detail: errors.join('\n') });
    }

    if (req.method === 'POST' && url.pathname === '/api/set-phase') {
      const body = await getBody(req);
      const phase = body.phase || body.fase;
      if (!phase) return sendJson(res, 400, { ok: false, error: 'Falta phase' });
      const pairingToken = body.pairingToken || body.pairing_token || body.token || '';
      const payload = { phase, fase: phase, pairingToken, token: pairingToken };
      const result = await tryPostPhaseEndpoints(config.phaseEndpoints, payload);
      return sendJson(res, 200, { ok: true, result });
    }

    if (req.method === 'POST' && url.pathname === '/api/chat') {
      const body = await getBody(req);
      const message = (body.message || '').trim();
      if (!message) return sendJson(res, 400, { ok: false, error: 'Mensaje vacío' });

      const payload = {
        chatId: body.chatId || body.chat_id || '',
        chat_id: body.chatId || body.chat_id || '',
        pairingToken: body.pairingToken || body.pairing_token || '',
        pairing_token: body.pairingToken || body.pairing_token || '',
        message,
        text: message,
        engine: body.engine || 'ollama',
        motor: body.engine || 'ollama',
        phase: body.phase || 'local',
        fase: body.phase || 'local',
        source: 'webchat-v6.2'
      };

      const result = await tryPostEndpoints(config.chatEndpoints, payload);
      return sendJson(res, 200, { ok: true, result });
    }

    if (req.method === 'GET') return serveStatic(req, res, url.pathname);

    sendText(res, 405, 'Método no permitido');
  } catch (err) {
    sendJson(res, 500, { ok: false, error: err.message });
  }
});

server.listen(config.port, config.host, () => {
  console.log(`[Hanna WebChat V6.2] Activo en http://${config.host}:${config.port}/`);
  console.log(`[Hanna WebChat V6.2] API móvil configurada: ${config.mobileApiBase}`);
});
