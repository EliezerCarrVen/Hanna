const { commands } = require('./commands');
const { status } = require('../core/status');
const { getModules } = require('../core/moduleRegistry');
const { DependencyCheckerService } = require('../services/dependencyCheckerService');
const { DoctorService } = require('../services/doctorService');
const { SelfTestService } = require('../services/selfTestService');
const { FlatFileMemoryService } = require('../services/flatFileMemoryService');
const { CodeCacheService } = require('../services/codeCacheService');
const { RollingSummaryService } = require('../services/rollingSummaryService');
const { VaultIndexService } = require('../services/vaultIndexService');
const { MarkdownVaultService } = require('../services/markdownVaultService');
const { AuditLogService } = require('../services/auditLogService');
const { ZeroLeakSanitizerService } = require('../services/zeroLeakSanitizerService');
const { IntentRouterService } = require('../services/intentRouterService');
const { NasIndexerService } = require('../services/nasIndexerService');
const { MqttService } = require('../services/mqttService');
const { WakeOnLanService } = require('../services/wakeOnLanService');
const { ClamAvService } = require('../services/clamAvService');
const { DockerPlannerService } = require('../services/dockerPlannerService');
const { NodeRedConnectorService } = require('../services/nodeRedConnectorService');
const { ServerlessWebhookService } = require('../services/serverlessWebhookService');
const { SystemDiagnosticsService } = require('../services/systemDiagnosticsService');
const { NaturalCommandService } = require('../services/naturalCommandService');
const { ConversationService } = require('../services/conversationService');
const { ResponseFormatterService } = require('../services/responseFormatterService');
const { EngineStateService } = require('../services/engineStateService');
const { PhaseStateService } = require('../services/phaseStateService');
const { MemoryService } = require('../services/memoryService');
const { LlmRouterService } = require('../services/llmRouterService');
const { SpotifyService } = require('../services/spotifyService');

class CommandRouter {
  constructor() {
    this.audit = new AuditLogService();
    this.natural = new NaturalCommandService();
    this.conversation = new ConversationService();
    this.formatter = new ResponseFormatterService();
  }

  async run(line, context = {}) { return this.handle(line, context); }

  async handle(line, context = {}) {
    const originalInput = String(line || '').trim();
    const jsonPrefix = originalInput.toLowerCase().startsWith('/json ');
    const input = jsonPrefix ? originalInput.slice(6).trim() : originalInput;
    const mode = jsonPrefix ? 'json' : (context.mode || 'human');
    const normalized = this.natural.normalize(input);
    const normalizedCommand = normalized.normalizedCommand || normalized.command || input;
    const execContext = { ...context, mode, originalInput, normalizedCommand };

    try {
      let payload;
      if (normalized.type === 'conversation') payload = await this.conversation.respond(normalized.action, normalized.text || input, execContext);
      else payload = await this.executeCommand(normalizedCommand, execContext);
      this.recordAudit(execContext, normalizedCommand, payload, 'ok');
      if (mode === 'json') return this.formatter.format({ command: normalizedCommand, data: payload }, { mode: 'json' });
      return this.formatter.format({ command: normalizedCommand, data: payload, human: payload && payload.human }, { ...execContext, mode: 'human' });
    } catch (error) {
      this.recordAudit(execContext, normalizedCommand, null, `error:${error.message}`);
      const safe = { status: 'error', message: 'internal_error' };
      return mode === 'json' ? this.formatter.format(safe, { mode: 'json' }) : this.formatter.format(safe, { mode: 'human', command: normalizedCommand });
    }
  }

  async executeCommand(input, context = {}) {
    const [cmd, sub, ...rest] = String(input || '').trim().split(' ');
    const text = rest.join(' ');
    if (cmd === '/help' || cmd === '/h' || cmd === '/ayuda') return { commands };
    if (cmd === '/status') return status();
    if (cmd === '/doctor' || cmd === '/diagnostico') return await new DoctorService().run();
    if (cmd === '/selftest') return await new SelfTestService().run();
    if (cmd === '/deps') return new DependencyCheckerService().checkAll();
    if (cmd === '/memoria' && sub === 'prueba') return this.conversation.saveMemory('memoria prueba', context.username || context.actor || 'local-root');
    if (cmd === '/memoria' && sub === 'guardar') return this.conversation.saveMemory(text, context.username || context.actor || 'local-root');
    if (cmd === '/memoria' && sub === 'buscar') return this.conversation.searchMemory(text);
    if (cmd === '/memoria' && (sub === 'ultimos' || sub === 'últimos')) return { type: 'memory_search', items: new FlatFileMemoryService().recent(10) };
    if (cmd === '/memoria' && sub === 'estado') return new MemoryService().status();
    if (cmd === '/codigo' && sub === 'prueba') return new CodeCacheService().prueba();
    if (cmd === '/codigo' && sub === 'buscar') return new CodeCacheService().buscar(text);
    if (cmd === '/codigo' && sub === 'listar') return new CodeCacheService().listar();
    if (cmd === '/codigo' && sub === 'estado') return new CodeCacheService().estado();
    if (cmd === '/summary' && sub === 'regenerar') return new RollingSummaryService().regenerate();
    if (cmd === '/summary') return new RollingSummaryService().read();
    if (cmd === '/indexar') return new VaultIndexService().index();
    if (cmd === '/indice' && sub === 'estado') return new VaultIndexService().status();
    if (cmd === '/vault' && sub === 'estado') return new MarkdownVaultService().status();
    if (cmd === '/vault' && sub === 'crear') return new MarkdownVaultService().createNote('bovedas', text || 'nueva-boveda', '');
    if (cmd === '/vault' && sub === 'listar') return new MarkdownVaultService().list();
    if (cmd === '/vault' && sub === 'importar') return { status: 'dry_run', dry_run: true, path: text };
    if (cmd === '/vault' && sub === 'verificar') return new MarkdownVaultService().status();
    if (cmd === '/auditoria' && sub === 'verificar') return new AuditLogService().verify();
    if (cmd === '/auditoria') return { file: require('../core/paths').paths.auditLog, verify: new AuditLogService().verify() };
    if (cmd === '/modulos') return getModules();
    if (cmd === '/zeroleak') return new ZeroLeakSanitizerService().sanitize([sub, ...rest].join(' '));
    if (cmd === '/intencion') return { intent: new IntentRouterService().classify([sub, ...rest].join(' ')) };
    if (cmd === '/nas' && sub === 'estado') return new NasIndexerService().status();
    if (cmd === '/nas' && sub === 'indexar') return new NasIndexerService().index();
    if (cmd === '/nas' && sub === 'buscar') return new NasIndexerService().search(text);
    if (cmd === '/mqtt' && sub === 'estado') return new MqttService().status();
    if (cmd === '/mqtt' && sub === 'publicar') return await new MqttService().publish(rest.shift(), rest.join(' '));
    if (cmd === '/spotify' && (sub === 'estado' || !sub)) return new SpotifyService().status();
    if (cmd === '/spotify' && sub === 'auth' && rest[0] === 'estado') return new SpotifyService().authStatus();
    if (cmd === '/spotify' && sub === 'buscar') return await new SpotifyService().search(text);
    if (cmd === '/spotify' && sub === 'reproducir') return await new SpotifyService().play(text);
    if (cmd === '/spotify' && sub === 'pausar') return await new SpotifyService().pause();
    if (cmd === '/spotify' && sub === 'siguiente') return await new SpotifyService().next();
    if (cmd === '/spotify' && sub === 'anterior') return await new SpotifyService().previous();
    if (cmd === '/wol' && sub === 'estado') return new WakeOnLanService().status();
    if (cmd === '/wol' && sub === 'probar') return { valid: new WakeOnLanService().isValidMac(text || rest[0] || '') };
    if (cmd === '/wol' && sub === 'enviar') return await new WakeOnLanService().send(text || rest[0] || '');
    if (cmd === '/clamav' && sub === 'estado') return new ClamAvService().status();
    if (cmd === '/clamav' && sub === 'escanear') return new ClamAvService().scan(text);
    if (cmd === '/docker' && sub === 'estado') return new DockerPlannerService().status();
    if (cmd === '/nodered' && sub === 'estado') return new NodeRedConnectorService().status();
    if (cmd === '/nodered' && sub === 'ping') return await new NodeRedConnectorService().ping();
    if (cmd === '/serverless' && sub === 'estado') return new ServerlessWebhookService().status();
    if (cmd === '/sistema' && sub === 'doctor') return new SystemDiagnosticsService().diagnose();
    if (cmd === '/ntp' && sub === 'estado') return { ntp: new SystemDiagnosticsService().diagnose().ntp };
    if (cmd === '/ip' && sub === 'estado') return { ip_local: new SystemDiagnosticsService().ipLocal() };
    if (cmd === '/motor' && (sub === 'actual' || sub === 'estado')) return new EngineStateService().current();
    if (cmd === '/motor' && sub === 'cambiar') return { status: 'dry_run', dry_run: true, requested: text, message: 'Cambio de motor no persistido en modo seguro.' };
    if (cmd === '/fase' && (sub === 'actual' || sub === 'estado')) return new PhaseStateService().current();
    if (cmd === '/fase' && sub === 'cambiar') return { status: 'dry_run', dry_run: true, requested: text, message: 'Cambio de fase no persistido en modo seguro.' };
    if (cmd === '/salir') return { status: 'bye' };
    return { human: await new LlmRouterService().respondLocal(input), data: { status: 'local_fallback', input } };
  }

  recordAudit(context, normalizedCommand, payload, result) {
    const module = String(normalizedCommand || '').split(' ')[0].replace('/', '') || 'conversation';
    this.audit.record({
      actor: context.username || context.userId || context.actor || 'local-root',
      command: context.originalInput || normalizedCommand,
      normalized_command: normalizedCommand,
      normalizedCommand,
      originalInput: context.originalInput || normalizedCommand,
      source: context.source || 'cli',
      userId: context.userId,
      username: context.username,
      chatId: context.chatId,
      module,
      result,
      dry_run: true,
      error: result.startsWith && result.startsWith('error:') ? result : undefined
    });
  }
}
module.exports = { CommandRouter };
