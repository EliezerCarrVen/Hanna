const path = require('path');
const { paths } = require('../core/paths');

class ResponseFormatterService {
  format(payload, context = {}) {
    const mode = context.mode || 'human';
    if (mode === 'json') return JSON.stringify(payload, null, 2);
    if (typeof payload === 'string') return payload;
    const command = payload && (payload.normalizedCommand || payload.command || context.normalizedCommand || context.command || '');
    const data = payload && Object.prototype.hasOwnProperty.call(payload, 'data') ? payload.data : payload;

    if (payload && payload.human) return payload.human;
    if (command.startsWith('/status')) return this.status(data);
    if (command.startsWith('/doctor') || command.startsWith('/diagnostico')) return this.doctor(data);
    if (command.startsWith('/deps')) return this.dependencies(data);
    if (command.startsWith('/spotify')) return this.spotify(data);
    if (command.startsWith('/voz') || command.startsWith('/escuchar')) return this.voice(data);
    if (command.startsWith('/pantalla') || command.startsWith('/analizar_pantalla')) return this.vision(data);
    if (command.startsWith('/obsidian') || command.startsWith('/graphifyy')) return this.obsidian(data);
    if (command.startsWith('/emocion') || command.startsWith('/reaccion')) return this.emotion(data);
    if (command.startsWith('/ia')) return this.ai(data);
    if (command.startsWith('/telegram')) return this.serviceStatus('Telegram', data);
    if (command.startsWith('/web')) return this.serviceStatus('Web compacta', data);
    if (command.startsWith('/auditoria verificar')) return this.auditVerify(data);
    if (command.startsWith('/auditoria')) return this.audit(data);
    if (command.startsWith('/modulos')) return this.modules(data);
    if (command.startsWith('/memoria guardar') || (data && data.type === 'memory_saved')) return this.memorySaved(data);
    if (command.startsWith('/memoria buscar') || (data && data.type === 'memory_search')) return this.memorySearch(data);
    if (command.startsWith('/memoria ultimos')) return this.memoryRecent(data);
    if (command.startsWith('/summary')) return this.summary(data);
    if (command.startsWith('/motor')) return this.engine(data);
    if (command.startsWith('/fase')) return this.phase(data);
    if (command.startsWith('/help')) return this.help(data);
    if (data && data.type === 'general_qa') return this.generalQa(data);
    if (data && data.status === 'unknown_command') return 'Te leí, pero no pude convertir eso en una acción segura. Puedes decir: estado, diagnóstico, qué puedes hacer, guarda esto en memoria, o busca en memoria.';
    if (data && data.status === 'error') return 'Tuve un problema interno procesando eso, pero ya lo registré sin exponer detalles técnicos. Prueba de nuevo o ejecuta diagnóstico.';
    return this.generic(data);
  }

  status(data = {}) {
    return [
      'Hanna está activa.',
      `Runtime: ${data.runtime === 'node' ? 'Node.js' : (data.runtime || 'Node.js')}`,
      `Modo: ${data.mode || 'lightweight/i386'}`,
      `Versión Node: ${data.node || process.version}`,
      `Arquitectura: ${data.arch || process.arch}`,
      `Modo seguro: ${data.dry_run ? 'dry-run' : 'operativo'}`,
      `Datos: ${this.shortPath(data.dataRoot || paths.dataRoot)}`,
      `Telegram: ${data.telegram || 'missing_configuration'}`,
      `Motor LLM: ${data.llm || 'missing_configuration'}`,
      `Módulos cargados: ${data.modules ?? 'desconocido'}`
    ].join('\n');
  }

  doctor(data = {}) {
    const deps = data.dependencies || [];
    const critical = ['node', 'npm', 'git'];
    const foundCritical = deps.filter(d => critical.includes(d.name) && d.found).map(d => d.name);
    const optionalMissing = deps.filter(d => !critical.includes(d.name) && d.status === 'missing_dependency').map(d => d.name);
    const blocked = data.blocked || {};
    const audit = data.audit && data.audit.ok ? 'auditoría íntegra' : 'auditoría por revisar';
    const blockedText = Object.entries(blocked).filter(([, v]) => v && v.status && v.status !== 'configured').map(([k, v]) => `${k}: ${v.status}`);
    return [
      `Diagnóstico general: ${data.status === 'ok' ? 'correcto' : (data.status || 'revisar')}.`,
      `Sistema: ${data.diagnostics ? `${data.diagnostics.platform}/${data.diagnostics.arch}` : process.platform + '/' + process.arch}.`,
      `Dependencias críticas: ${foundCritical.length ? foundCritical.join(', ') + ' encontradas' : 'revisar node/npm/git'}.`,
      `Pendientes opcionales: ${optionalMissing.length ? optionalMissing.join(', ') : 'ninguno crítico detectado'}.`,
      blockedText.length ? `Configuraciones pendientes: ${blockedText.join(', ')}.` : 'Configuraciones base: sin bloqueos críticos.',
      `Auditoría: ${audit}.`,
      'Siguiente acción recomendada: en la HP Mini instala solo las dependencias opcionales que realmente necesites.'
    ].join('\n');
  }

  dependencies(deps = []) {
    const found = deps.filter(d => d.found).map(d => d.name);
    const missing = deps.filter(d => d.status === 'missing_dependency').map(d => d.name);
    const na = deps.filter(d => d.status === 'not_applicable').map(d => d.name);
    return [
      'Revisión de dependencias:',
      `Encontradas: ${found.length ? found.join(', ') : 'ninguna'}.`,
      `Faltantes opcionales o pendientes: ${missing.length ? missing.join(', ') : 'ninguna'}.`,
      na.length ? `No aplican en esta plataforma: ${na.join(', ')}.` : '',
      'Críticas esperadas para Hanna.NodeLightweight: node, npm y git.'
    ].filter(Boolean).join('\n');
  }

  auditVerify(data = {}) {
    if (data.ok) return `Auditoría válida.\nEventos revisados: ${data.events}.\nCadena hash íntegra.`;
    return `Auditoría con alerta: ${data.status || 'revisar'}.\nEvento/índice: ${data.event_id || data.index || 'desconocido'}.`;
  }

  audit(data = {}) { return data.verify ? this.auditVerify(data.verify) : this.generic(data); }

  modules(modules = []) {
    return 'Módulos disponibles:\n' + modules.map(m => `- ${m.name}${m.dry_run ? ' (dry-run)' : ''}${m.optional ? ' (opcional)' : ''}`).join('\n');
  }

  spotify(data = {}) {
    const action = data.action || 'estado';
    const missing = Array.isArray(data.missing) && data.missing.length ? `\nConfiguración pendiente: ${data.missing.join(', ')}.` : '';
    if (data.status === 'blocked_by_configuration' || data.status === 'missing_configuration') {
      return `Spotify está bloqueado por configuración.\nEstado: ${data.status}.\nModo seguro: dry-run.${missing}\nConfigura SPOTIFY_CLIENT_ID, SPOTIFY_CLIENT_SECRET, SPOTIFY_REDIRECT_URI y SPOTIFY_REFRESH_TOKEN para habilitar el adapter sin guardar secretos.`;
    }
    if (data.status === 'dry_run') return `Spotify recibió la acción "${action}" en modo dry-run.\nNo se enviaron cambios al reproductor.\n${data.message || 'Desactiva HANNA_SPOTIFY_DRY_RUN solo en una máquina configurada.'}`;
    if (action === 'buscar' && data.status === 'ok') {
      const tracks = (data.tracks || []).map((track, i) => `${i + 1}. ${track.name} — ${(track.artists || []).join(', ')}`).join('\n');
      return tracks ? `Resultados de Spotify para "${data.query}":\n${tracks}` : `No encontré canciones en Spotify para "${data.query}".`;
    }
    if (action === 'reproducir' && data.status === 'ok') return `Spotify reproduciendo: ${data.track?.name || 'pista seleccionada'}.`;
    if (['pausar', 'siguiente', 'anterior'].includes(action) && data.status === 'ok') return `Spotify: acción "${action}" ejecutada correctamente.`;
    if (data.status === 'service_unavailable') return `Spotify no respondió correctamente (${data.error || 'service_unavailable'}). Revisa red, sesión Premium/dispositivo activo y credenciales.`;
    return `Spotify estado: ${data.status || 'desconocido'}.\nModo seguro: ${data.dry_run ? 'dry-run' : 'operativo'}.`;
  }

  generalQa(data = {}) {
    if (data.status === 'ok') return data.answer || 'Encontré una respuesta con el contexto local de Hanna.';
    return data.message || 'Puedo responder eso cuando configures un motor IA. Falta configurar GROQ_API_KEY, GEMINI_API_KEY, OPENROUTER_API_KEY u OLLAMA_BASE_URL.';
  }

  voice(data = {}) {
    if (data.tts || data.stt) {
      return `Voz local: ${data.status || 'desconocido'}.\nTTS/espeak-ng: ${data.tts || 'n/d'}.\nGrabación/arecord: ${data.stt || 'n/d'}.`;
    }
    if (data.status === 'missing_dependency' || data.error === 'missing_dependency') {
      return `Voz no disponible: falta ${data.dependency || 'una herramienta de audio'}.\nEn Debian 12 i386 instala manualmente espeak-ng y alsa-utils si quieres activar voz.`;
    }
    if (data.ok && data.path) return `Grabación completada. Archivo: ${this.shortPath(data.path)}.`;
    if (data.ok) return 'Voz enviada con espeak-ng.';
    return `Voz: ${data.status || 'revisar'}. ${data.error || ''}`.trim();
  }

  vision(data = {}) {
    if (data.status === 'available') return 'Captura de pantalla disponible con scrot.';
    if (data.status === 'missing_dependency' || data.error === 'missing_dependency') {
      return `Captura de pantalla no disponible: falta ${data.dependency || 'scrot'}.\nInstala scrot manualmente en Debian si quieres usar /analizar_pantalla.`;
    }
    if (data.ok) return `Captura de pantalla lista: ${this.shortPath(data.path)}. Base64 generado (${String(data.base64 || '').length} caracteres).`;
    return `Captura de pantalla: ${data.status || 'revisar'}. ${data.error || ''}`.trim();
  }

  obsidian(data = {}) {
    if (data.type === 'obsidian_search') {
      const items = data.items || [];
      if (!items.length) return `No encontré coincidencias en Obsidian para “${data.query || ''}”.`;
      return 'Encontré esto en Obsidian:\n' + items.slice(0, 6).map((x, i) => `${i + 1}. ${x.relative || x.path}: ${x.preview || ''}`.slice(0, 220)).join('\n');
    }
    if (data.status === 'ok' && data.file) return `Guardé la nota en Obsidian.\nÁrea: ${data.area || 'conocimiento'}\nResumen: ${data.summary || data.title || ''}`;
    if (data.root) return `Obsidian/RAG activo.\nBóveda: ${this.shortPath(data.root)}\nNotas: ${data.notes || 0}\nÁreas: ${(data.areas || []).join(', ')}`;
    return this.generic(data);
  }

  emotion(data = {}) { return `Estado emocional de Hanna: ${data.mood || 'enfocada'}.\nEnergía: ${data.energy ?? 'n/d'}.\nConfianza: ${data.confidence ?? 'n/d'}.\nTono: ${data.tone || 'cálido y directo'}.\nÚltima reacción: ${data.last_reaction || 'lista para ayudar'}.`; }
  ai(data = {}) { return `IA de Hanna: ${data.status}.\nMotor activo: ${data.active || 'local_fallback'}.\nProveedores: ${(data.providers || []).map(p => `${p.provider}:${p.status}`).join(', ') || 'sin proveedores configurados'}.`; }
  serviceStatus(name, data = {}) { return `${name}: ${data.status || 'desconocido'}.\nServicio permanente: ${data.always_on || 'systemd opcional'}.`; }

  memorySaved(data = {}) { return `Listo. Guardé esa memoria real de forma local y sanitizada.\nArchivo/registro: ${this.shortPath(data.file || data.path || 'short_memory.jsonl')}`; }
  memorySearch(data = {}) {
    const items = Array.isArray(data.items) ? data.items : Array.isArray(data) ? data : [];
    if (!items.length) return 'No encontré coincidencias en la memoria local de Hanna.';
    return 'Encontré esto en la memoria local:\n' + items.slice(0, 5).map((x, i) => `${i + 1}. ${x.text || x.preview || x.file || JSON.stringify(x).slice(0, 160)}`).join('\n');
  }
  memoryRecent(data = {}) { return this.memorySearch({ items: Array.isArray(data) ? data : data.items || [] }); }
  summary(data = '') { return typeof data === 'string' ? (data.trim() || 'Aún no hay resumen rolling.') : `Resumen actualizado: ${this.shortPath(data.file || '')}`; }
  engine(data = {}) { return `Motor actual: ${data.current || 'local-node'}.\nEstado: ${data.status || 'ok'}.\nLLM externo: ${data.external_llm || 'missing_configuration'}.\nModo seguro: ${data.dry_run ? 'dry-run' : 'operativo'}.`; }
  phase(data = {}) { return `Fase actual: ${data.current || 'node-lightweight'}.\nEstado: ${data.status || 'ok'}.\nPerfil: ${data.profile || 'HP Mini i386 / bajo consumo'}.`; }
  help(data = {}) {
    const commands = data.commands || [];
    return [
      'Soy Hanna.NodeLightweight, la conversión Node.js de Hanna para HP Mini i386.',
      'Puedo ayudarte con diagnóstico real, memoria local, auditoría, dependencias, motor, fase, vault, Spotify, NAS, MQTT y Wake-on-LAN en modo seguro.',
      'Puedes hablarme normal: hola, estado, diagnóstico, verifica auditoría, qué falta instalar, guarda esto en memoria: ..., busca en memoria ...',
      commands.length ? `Comandos slash principales: ${commands.slice(0, 18).join(', ')}...` : 'Comandos slash: /status, /doctor, /deps, /auditoria verificar, /memoria guardar, /memoria buscar, /motor actual, /fase actual.',
      'Si necesitas JSON crudo usa: /json /doctor'
    ].join('\n');
  }
  generic(data) { return typeof data === 'object' ? 'Listo. Procesé la solicitud correctamente.' : String(data ?? 'Listo.'); }
  shortPath(value) { if (!value) return ''; const rel = path.relative(paths.repoRoot, value); return rel && !rel.startsWith('..') ? rel : value; }
}
module.exports = { ResponseFormatterService };
