const { HannaHttpServerBase } = require('./httpServerBase');
class MobileApiServer extends HannaHttpServerBase {
  constructor(options = {}) { super({ name: 'mobile-api', port: process.env.HANNA_MOBILE_API_PORT || 8790, token: process.env.HANNA_MOBILE_API_TOKEN || process.env.HANNA_JWT_SECRET || '', ...options }); }
}
async function main() { const server = new MobileApiServer(); if (process.argv.includes('--self-test')) { console.log(JSON.stringify(await server.selfTest(), null, 2)); return; } server.start(); }
if (require.main === module) main().catch(e => { console.error('Mobile API error:', e.message); process.exit(1); });
module.exports = { MobileApiServer };
