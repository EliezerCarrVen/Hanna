const { EmotionStateService } = require('./emotionStateService');
class ReactionService {
  constructor() { this.emotions = new EmotionStateService(); }
  status() { return { status: 'ok', ...this.emotions.getState() }; }
  greeting() { this.emotions.recordReaction('greeting', 'saludo cálido'); return 'Hola, soy Hanna. Estoy contigo y en línea. Puedo ayudarte con diagnóstico, memoria, Obsidian, IA, Spotify, Telegram y automatización segura. ¿Qué necesitas hacer?'; }
  success(text = 'Listo, salió bien.') { this.emotions.recordReaction('success', text); return `✨ ${text}`; }
  warning(text) { this.emotions.recordReaction('warning', text); return `Atención: ${text}`; }
  error() { this.emotions.recordReaction('error', 'error empático registrado'); return 'Tuve un problema procesando eso, pero lo registré sin exponer detalles técnicos. Prueba con diagnóstico o ayuda.'; }
}
module.exports = { ReactionService };
