const { IntentRouterService } = require('./intentRouterService');
class NaturalCommandService {
  constructor() { this.intentRouter = new IntentRouterService(); }
  normalize(input) {
    const originalText = String(input || '').trim();
    const simplified = this.simplify(originalText);
    const hit = (normalizedCommand, intent, confidence = 0.95, extra = {}) => ({ matched: true, normalizedCommand, command: normalizedCommand, intent, confidence, originalText, ...extra });
    if (!originalText) return hit('/help', 'help', 0.6, { type: 'empty' });
    if (originalText.startsWith('/')) return hit(originalText, 'slash', 1, { type: 'slash' });
    if (/^(hola|buenas|hey|que tal)\b/.test(simplified)) return { matched: true, normalizedCommand: '', intent: 'greeting', confidence: 0.95, originalText, type: 'conversation', action: 'greeting' };
    if (/^(que puedes hacer|ayuda|help|capacidades|que sabes hacer)/.test(simplified)) return hit('/help', 'help');
    if (/^(estado|status)$/.test(simplified)) return hit('/status', 'status');
    if (/^(como estas|estas bien)$/.test(simplified)) return hit('/doctor', 'diagnostico', 0.9, { summarizeWithStatus: true });
    if (/diagnostico|doctor|revisa el sistema|revisa si todo esta bien|todo esta bien/.test(simplified)) return hit('/doctor', 'diagnostico');
    if (/estado de spotify|spotify estado|spotify auth estado/.test(simplified)) return hit(simplified.includes('auth') ? '/spotify auth estado' : '/spotify estado', 'spotify');
    if (/^(pausa spotify|pausar spotify|deten spotify|detener spotify)$/.test(simplified)) return hit('/spotify pausar', 'spotify');
    if (/^(siguiente cancion|siguiente canción|spotify siguiente|siguiente spotify)$/.test(simplified)) return hit('/spotify siguiente', 'spotify');
    if (/^(anterior cancion|anterior canción|spotify anterior|anterior spotify)$/.test(simplified)) return hit('/spotify anterior', 'spotify');
    const spotifyPlay = originalText.match(/^(reproduce en spotify|reproducir en spotify|spotify reproducir|pon en spotify)\s+(.+)$/i);
    if (spotifyPlay) return hit(`/spotify reproducir ${spotifyPlay[2].trim()}`, 'spotify');
    const spotifySearch = originalText.match(/^(busca en spotify|spotify buscar)\s+(.+)$/i);
    if (spotifySearch) return hit(`/spotify buscar ${spotifySearch[2].trim()}`, 'spotify');
    if (/que falta instalar|dependencias|deps|faltan dependencias/.test(simplified)) return hit('/deps', 'dependencia');
    if (/^(modulos|muestrame modulos|lista modulos)$/.test(simplified)) return hit('/modulos', 'sistema');
    if (/(verifica|verificar|revisa|revisar).*auditoria|auditoria.*(verifica|revisa)/.test(simplified)) return hit('/auditoria verificar', 'auditoria');
    if (/motor actual/.test(simplified)) return hit('/motor actual', 'motor');
    if (/estado del motor|motor estado/.test(simplified)) return hit('/motor estado', 'motor');
    if (/fase actual/.test(simplified)) return hit('/fase actual', 'fase');
    if (/estado de fase|fase estado/.test(simplified)) return hit('/fase estado', 'fase');
    const save = originalText.match(/^(recuerda que|guarda esto en memoria:|guarda esto:|guarda en memoria|memoriza)\s*(.+)$/i);
    if (save) return hit(`/memoria guardar ${save[2].trim()}`, 'memoria');
    const search = originalText.match(/^(busca en memoria|busca memoria|qué recuerdas de|que recuerdas de)\s*:??\s*(.+)$/i);
    if (search) return hit(`/memoria buscar ${search[2].trim()}`, 'memoria');
    return { matched: false, normalizedCommand: '', intent: this.intentRouter.classify(originalText), confidence: 0.25, originalText, type: 'conversation', action: 'fallback', text: originalText };
  }
  simplify(text) { return String(text || '').toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[¿?¡!.,;]+/g, ' ').replace(/\s+/g, ' ').trim(); }
}
module.exports = { NaturalCommandService };
