const fs = require('fs'); const path = require('path');
const { StartupService } = require('./startupService'); const { PathGuardService } = require('./pathGuardService'); const { SecretFilterService } = require('./secretFilterService'); const { FlatFileMemoryService } = require('./flatFileMemoryService'); const { MarkdownVaultService } = require('./markdownVaultService'); const { AuditLogService } = require('./auditLogService'); const { VaultEncryptionService } = require('./vaultEncryptionService'); const { IntentRouterService } = require('./intentRouterService'); const { WakeOnLanService } = require('./wakeOnLanService'); const { DependencyCheckerService } = require('./dependencyCheckerService'); const { SpotifyService } = require('./spotifyService'); const { ObsidianVaultService } = require('./obsidianVaultService'); const { GeneralQaService } = require('./generalQaService'); const { EmotionStateService } = require('./emotionStateService');
class SelfTestService {
  async run() { const results = []; const add = (name, ok, detail = '') => results.push({ name, result: ok ? 'PASS' : 'FAIL', detail }); const warn = (name, detail = '') => results.push({ name, result: 'WARN', detail });
    new StartupService().ensureDataLayout(); add('crear HannaData', true);
    const guard = new PathGuardService(); add('PathGuard bloquea traversal', !guard.validate('../.env').ok); add('PathGuard permite HannaData', guard.validate(path.join(require('../core/paths').paths.dataRoot, 'runtime', 'x.tmp')).ok);
    add('SecretFilter redacta', new SecretFilterService().redact('api_key=abc password=def').includes('[REDACTED]'));
    const mem = new FlatFileMemoryService(); mem.add('selftest memoria prueba token=abc1234567890123456789012345678901234567890'); add('JSONL memoria', mem.search('selftest').length > 0);
    const vault = new MarkdownVaultService(); vault.createNote('memoria', 'selftest prueba', 'contenido de búsqueda selftest'); add('Markdown vault', vault.search('selftest').length > 0);
    const audit = new AuditLogService(); audit.record({ command: '/selftest', module: 'selftest', result: 'ok', dry_run: true }); add('Audit hash-chain', audit.verify().ok);
    const enc = new VaultEncryptionService(); const payload = enc.encryptText('secreto temporal', 'temporal-password'); add('Vault AES-256-GCM', payload.ok && enc.decryptText(payload, 'temporal-password') === 'secreto temporal');
    add('Intent router', new IntentRouterService().classify('buscar en memoria') === 'memoria'); add('WOL dry-run', (await new WakeOnLanService().send('00:11:22:33:44:55')).status === 'dry_run');
    const obsidian = new ObsidianVaultService(); obsidian.createNote('selftest', 'contenido llm selftest', { area: 'conocimiento' }); add('Obsidian/RAG', obsidian.search('selftest').length > 0);
    const qa = await new GeneralQaService().answer('busca que es un llm', { source: 'selftest' }); add('General QA pipeline', ['ok','missing_configuration'].includes(qa.status));
    add('Emotion state', new EmotionStateService().getState().status === 'ok');
    const spotify = new SpotifyService().status(); warn('Spotify adapter', `${spotify.status}; dry_run=${spotify.dry_run}`);
    const deps = new DependencyCheckerService().checkAll(); warn('Dependencias revisadas', `${deps.filter(d => d.found).length}/${deps.length} encontradas`);
    const criticalFails = results.filter(r => r.result === 'FAIL').length; return { status: criticalFails ? 'fail' : 'ok', results, criticalFails }; }
}
module.exports = { SelfTestService };
