const fs = require('fs');
const path = require('path');
const { paths } = require('../core/paths');
class PersonaService {
  constructor() { this.personaPath = path.join(paths.repoRoot, 'Hanna', 'Personalidad.txt'); }
  greeting() { return 'Hola, soy Hanna. Estoy en línea. Puedo ayudarte con diagnóstico, memoria, auditoría, dependencias, motor, fase y automatización local. ¿Qué necesitas hacer?'; }
  capabilities() {
    return [
      'Puedo ayudarte con capacidades reales de esta versión NodeLightweight:',
      '- Diagnóstico real del sistema, runtime y dependencias para Debian 12 i386/Windows.',
      '- Memoria local real: guardar en JSONL/Markdown, buscar y generar resumen.',
      '- Auditoría real con hash-chain y sanitización de secretos.',
      '- Estado de motor, fase, Telegram, vault, NAS, MQTT, WOL, ClamAV, Docker y Node-RED con dry-run seguro.',
      '- Comandos naturales en español y comandos slash compatibles.',
      'Ejemplos: estado, diagnóstico, verifica auditoría, qué falta instalar, guarda esto en memoria: prueba.'
    ].join('\n');
  }
  identitySummary() { try { return fs.existsSync(this.personaPath) ? fs.readFileSync(this.personaPath, 'utf8').slice(0, 300) : ''; } catch { return ''; } }
  fallback(text) { return `No estoy segura de lo que necesitas. Puedo ayudarte con diagnóstico, memoria, auditoría, dependencias, motor, fase, vault, NAS, MQTT o Wake-on-LAN. Texto recibido: “${String(text || '').slice(0, 160)}”.`; }
}
module.exports = { PersonaService };
