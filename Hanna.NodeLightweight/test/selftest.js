const { SelfTestService } = require('../src/services/selfTestService');
(async () => { const result = await new SelfTestService().run(); for (const r of result.results) console.log(`${r.result} ${r.name}${r.detail ? ' - ' + r.detail : ''}`); process.exit(result.criticalFails ? 1 : 0); })().catch(e => { console.error('FAIL selftest', e); process.exit(1); });
