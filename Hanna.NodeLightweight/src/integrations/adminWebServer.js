const { HannaHttpServerBase } = require('./httpServerBase');
class AdminWebServer extends HannaHttpServerBase {
  constructor(options = {}) { super({ name: 'admin-web', port: process.env.HANNA_ADMIN_WEB_PORT || 8787, token: process.env.HANNA_ADMIN_TOKEN || '', ...options }); }
}
async function main() { const server = new AdminWebServer(); if (process.argv.includes('--self-test')) { console.log(JSON.stringify(await server.selfTest(), null, 2)); return; } server.start(); }
if (require.main === module) main().catch(e => { console.error('Admin Web error:', e.message); process.exit(1); });
module.exports = { AdminWebServer };
