const { HannaHttpServerBase } = require('./httpServerBase');
class WebChatServer extends HannaHttpServerBase {
  constructor(options = {}) { super({ name: 'webchat', port: process.env.HANNA_WEBCHAT_PORT || 8789, token: process.env.HANNA_WEBCHAT_TOKEN || '', ...options }); }
  async route(method, pathname, body, context = {}) {
    if (method === 'GET' && pathname === '/') return { html: '<!doctype html><meta charset="utf-8"><title>Hanna NodeLightweight</title><h1>Hanna NodeLightweight</h1><p>POST /chat con {"text":"hola"}</p>' };
    return super.route(method, pathname, body, context);
  }
}
async function main() { const server = new WebChatServer(); if (process.argv.includes('--self-test')) { console.log(JSON.stringify(await server.selfTest(), null, 2)); return; } server.start(); }
if (require.main === module) main().catch(e => { console.error('WebChat error:', e.message); process.exit(1); });
module.exports = { WebChatServer };
