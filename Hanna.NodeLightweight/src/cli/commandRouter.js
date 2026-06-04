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
const { ObsidianVaultService } = require('../services/obsidianVaultService');
const { KnowledgeIndexService } = require('../services/knowledgeIndexService');
const { EmotionStateService } = require('../services/emotionStateService');
const { ReactionService } = require('../services/reactionService');
const { SafeLogService } = require('../services/safeLogService');
const { VoiceService } = require('../services/voiceService');
const { VisionService } = require('../services/visionService');

class CommandRouter {
  constructor() {
    this.audit = new AuditLogService();
    this.natural = new NaturalCommandService();
    this.conversation = new ConversationService();
    this.formatter = new ResponseFormatterService();
    this.log = new SafeLogService();
    this.services = {
      doctor: new DoctorService(),
      selfTest: new SelfTestService(),
      deps: new DependencyCheckerService(),
      flatMemory: new FlatFileMemoryService(),
      memory: new MemoryService(),
      codeCache: new CodeCacheService(),
      summary: new RollingSummaryService(),
      vaultIndex: new VaultIndexService(),
      vault: new MarkdownVaultService(),
      audit: this.audit,
      zeroLeak: new ZeroLeakSanitizerService(),
      intent: new IntentRouterService(),
      nas: new NasIndexerService(),
      mqtt: new MqttService(),
      spotify: new SpotifyService(),
      wol: new WakeOnLanService(),
      clamav: new ClamAvService(),
      docker: new DockerPlannerService(),
      nodeRed: new NodeRedConnectorService(),
      serverless: new ServerlessWebhookService(),
      diagnostics: new SystemDiagnosticsService(),
      engine: new EngineStateService(),
      phase: new PhaseStateService(),
      llm: new LlmRouterService(),
      obsidian: new ObsidianVaultService(),
      knowledgeIndex: new KnowledgeIndexService(),
      emotions: new EmotionStateService(),
      reactions: new ReactionService(),
      voice: new VoiceService(),
      vision: new VisionService()
    };
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
    if (cmd === '/doctor' || cmd === '/diagnostico') return await this.services.doctor.run();
    if (cmd === '/selftest') return await this.services.selfTest.run();
    if (cmd === '/deps') return this.services.deps.checkAll();
    if (cmd === '/memoria' && sub === 'prueba') return this.conversation.saveMemory('memoria prueba', context.username || context.actor || 'local-root');
    if (cmd === '/memoria' && sub === 'guardar') return this.conversation.saveMemory(text, context.username || context.actor || 'local-root');
    if (cmd === '/memoria' && sub === 'buscar') return this.conversation.searchMemory(text);
    if (cmd === '/memoria' && (sub === 'ultimos' || sub === 'últimos')) return { type: 'memory_search', items: this.services.flatMemory.recent(10) };
    if (cmd === '/memoria' && sub === 'estado') return this.services.memory.status();
    if (cmd === '/codigo' && sub === 'prueba') return this.services.codeCache.prueba();
    if (cmd === '/codigo' && sub === 'buscar') return this.services.codeCache.buscar(text);
    if (cmd === '/codigo' && sub === 'listar') return this.services.codeCache.listar();
    if (cmd === '/codigo' && sub === 'estado') return this.services.codeCache.estado();
    if (cmd === '/summary' && sub === 'regenerar') return this.services.summary.regenerate();
    if (cmd === '/summary') return this.services.summary.read();
    if (cmd === '/indexar') return this.services.vaultIndex.index();
    if (cmd === '/indice' && sub === 'estado') return this.services.vaultIndex.status();
    if (cmd === '/obsidian' && sub === 'estado') return this.services.obsidian.status();
    if (cmd === '/obsidian' && sub === 'indexar') return this.services.knowledgeIndex.index();
    if (cmd === '/obsidian' && sub === 'buscar') return { type: 'obsidian_search', query: text, items: this.services.obsidian.search(text, 10) };
    if (cmd === '/obsidian' && sub === 'guardar') { const parts = text.split('::'); return this.services.obsidian.createNote((parts[0] || 'nota').trim(), (parts.slice(1).join('::') || parts[0] || '').trim(), { area: 'conocimiento', tags: ['hanna', 'obsidian'] }); }
    if (cmd === '/graphifyy' && sub === 'guardar') return this.services.obsidian.createNote('graphifyy', text, { area: 'graphifyy', tags: ['graphifyy'] });
    if (cmd === '/graphifyy' && sub === 'buscar') return { type: 'obsidian_search', query: text, items: this.services.obsidian.search(text, 10).filter(x => String(x.relative || '').includes('graphifyy')) };
    if (cmd === '/vault' && sub === 'estado') return this.services.vault.status();
    if (cmd === '/vault' && sub === 'crear') return this.services.vault.createNote('bovedas', text || 'nueva-boveda', '');
    if (cmd === '/vault' && sub === 'listar') return this.services.vault.list();
    if (cmd === '/vault' && sub === 'importar') return { status: 'dry_run', dry_run: true, path: text };
    if (cmd === '/vault' && sub === 'verificar') return this.services.vault.status();
    if (cmd === '/auditoria' && sub === 'verificar') return this.services.audit.verify();
    if (cmd === '/auditoria') return { file: require('../core/paths').paths.auditLog, verify: this.services.audit.verify() };
    if (cmd === '/modulos') return getModules();
    if (cmd === '/zeroleak') return this.services.zeroLeak.sanitize([sub, ...rest].join(' '));
    if (cmd === '/intencion') return { intent: this.services.intent.classify([sub, ...rest].join(' ')) };
    if (cmd === '/nas' && sub === 'estado') return this.services.nas.status();
    if (cmd === '/nas' && sub === 'indexar') return this.services.nas.index();
    if (cmd === '/nas' && sub === 'buscar') return this.services.nas.search(text);
    if (cmd === '/mqtt' && sub === 'estado') return this.services.mqtt.status();
    if (cmd === '/mqtt' && sub === 'publicar') return await this.services.mqtt.publish(rest.shift(), rest.join(' '));
    if (cmd === '/spotify' && (sub === 'estado' || !sub)) return this.services.spotify.status();
    if (cmd === '/spotify' && sub === 'auth' && rest[0] === 'estado') return this.services.spotify.authStatus();
    if (cmd === '/spotify' && sub === 'buscar') return await this.services.spotify.search(text);
    if (cmd === '/spotify' && sub === 'reproducir') return await this.services.spotify.play(text);
    if (cmd === '/spotify' && sub === 'pausar') return await this.services.spotify.pause();
    if (cmd === '/spotify' && sub === 'siguiente') return await this.services.spotify.next();
    if (cmd === '/spotify' && sub === 'anterior') return await this.services.spotify.previous();
    if (cmd === '/voz' && (sub === 'estado' || !sub)) return this.services.voice.status();
    if (cmd === '/voz' && (sub === 'decir' || sub === 'hablar')) return await this.services.voice.speak(text);
    if (cmd === '/escuchar') { const duration = Number(sub) || 5; const outputPath = Number(sub) ? text : [sub, ...rest].filter(Boolean).join(' '); return await this.services.voice.record(duration, outputPath || undefined); }
    if (cmd === '/pantalla' && sub === 'estado') return this.services.vision.status();
    if (cmd === '/pantalla' && sub === 'capturar') return await this.services.vision.captureScreen(text || undefined);
    if (cmd === '/analizar_pantalla') return await this.services.vision.captureScreen([sub, ...rest].filter(Boolean).join(' ') || undefined);
    if (cmd === '/wol' && sub === 'estado') return this.services.wol.status();
    if (cmd === '/wol' && sub === 'probar') return { valid: this.services.wol.isValidMac(text || rest[0] || '') };
    if (cmd === '/wol' && sub === 'enviar') return await this.services.wol.send(text || rest[0] || '');
    if (cmd === '/clamav' && sub === 'estado') return this.services.clamav.status();
    if (cmd === '/clamav' && sub === 'escanear') return await this.services.clamav.scan(text);
    if (cmd === '/docker' && sub === 'estado') return this.services.docker.status();
    if (cmd === '/nodered' && sub === 'estado') return this.services.nodeRed.status();
    if (cmd === '/nodered' && sub === 'ping') return await this.services.nodeRed.ping();
    if (cmd === '/serverless' && sub === 'estado') return this.services.serverless.status();
    if (cmd === '/sistema' && sub === 'doctor') return this.services.diagnostics.diagnose();
    if (cmd === '/ntp' && sub === 'estado') return { ntp: this.services.diagnostics.diagnose().ntp };
    if (cmd === '/ip' && sub === 'estado') return { ip_local: this.services.diagnostics.ipLocal() };
    if (cmd === '/motor' && (sub === 'actual' || sub === 'estado')) return this.services.engine.current();
    if (cmd === '/motor' && sub === 'cambiar') return { status: 'dry_run', dry_run: true, requested: text, message: 'Cambio de motor no persistido en modo seguro.' };
    if (cmd === '/fase' && (sub === 'actual' || sub === 'estado')) return this.services.phase.current();
    if (cmd === '/fase' && sub === 'cambiar') return { status: 'dry_run', dry_run: true, requested: text, message: 'Cambio de fase no persistido en modo seguro.' };
    if (cmd === '/emocion' && sub === 'estado') return this.services.emotions.getState();
    if (cmd === '/reaccion' && sub === 'estado') return this.services.reactions.status();
    if (cmd === '/ia' && sub === 'estado') return this.services.llm.status();
    if (cmd === '/telegram' && sub === 'estado') return { status: process.env.TELEGRAM_BOT_TOKEN ? 'configured' : 'missing_configuration', service: 'telegram', always_on: 'systemd:hanna-telegram.service' };
    if (cmd === '/web' && sub === 'estado') return { status: 'configured', service: 'web', port: Number(process.env.HANNA_WEB_PORT || 8787), always_on: 'systemd:hanna-web.service' };
    if (cmd === '/salir') return { status: 'bye' };

    try {
      const llmResponse = await this.services.llm.generate(input, { ...context, timeout: context.timeout || 15000 });
      if (llmResponse && llmResponse.status === 'ok') {
        return { human: llmResponse.text, data: { status: 'llm_answered', provider: llmResponse.provider || 'unknown' } };
      }
      if (llmResponse && llmResponse.status && llmResponse.status !== 'missing_configuration') {
        this.log.write('LLM_ROUTING_STATUS', { status: llmResponse.status, provider: llmResponse.provider });
      }
    } catch (error) {
      this.log.write('LLM_ROUTING_ERROR', { error: error.message });
    }

    const localFallback = this.services.llm && typeof this.services.llm.respondLocal === 'function'
      ? await this.services.llm.respondLocal(input)
      : `No tengo un motor IA disponible ahora mismo, pero puedo ayudarte con /help, /status, /doctor, memoria y diagnóstico local. Texto recibido: “${String(input || '').slice(0, 160)}”.`;
    return { human: localFallback, data: { status: 'local_fallback', input } };
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
