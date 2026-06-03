const { runAsync, commandExists } = require('../utils/processRunner');

class VoiceService {
  constructor(options = {}) {
    this.commandExists = options.commandExists || commandExists;
    this.runAsync = options.runAsync || runAsync;
  }

  status() {
    const ttsAvailable = Boolean(this.commandExists('espeak-ng'));
    const sttAvailable = Boolean(this.commandExists('arecord'));
    return {
      status: ttsAvailable && sttAvailable ? 'available' : 'missing_dependency',
      tts: ttsAvailable ? 'available' : 'missing_espeak-ng',
      stt: sttAvailable ? 'available' : 'missing_arecord',
      dependencies: {
        'espeak-ng': ttsAvailable ? 'available' : 'missing_dependency',
        arecord: sttAvailable ? 'available' : 'missing_dependency'
      }
    };
  }

  sanitizeText(text) {
    return String(text || '')
      .replace(/["'$`\\]/g, '')
      .replace(/\s+/g, ' ')
      .trim()
      .slice(0, 500);
  }

  async speak(text) {
    if (!this.commandExists('espeak-ng')) {
      return { ok: false, status: 'missing_dependency', error: 'missing_dependency', dependency: 'espeak-ng' };
    }
    const cleanText = this.sanitizeText(text);
    if (!cleanText) return { ok: false, status: 'invalid_input', error: 'empty_text' };
    const result = await this.runAsync('espeak-ng', ['-v', 'es', '-s', '150', cleanText], { timeout: 30000 });
    return {
      ok: result.status === 0,
      status: result.status === 0 ? 'ok' : 'failed',
      code: result.status,
      error: result.error || (result.stderr || '').slice(0, 240)
    };
  }

  async record(durationSeconds = 5, outputPath = '/tmp/hanna_record.wav') {
    if (!this.commandExists('arecord')) {
      return { ok: false, status: 'missing_dependency', error: 'missing_dependency', dependency: 'arecord' };
    }
    const duration = Math.max(1, Math.min(60, Number(durationSeconds) || 5));
    const result = await this.runAsync('arecord', ['-d', String(duration), '-f', 'cd', outputPath], { timeout: (duration + 5) * 1000 });
    return {
      ok: result.status === 0,
      status: result.status === 0 ? 'ok' : 'failed',
      path: outputPath,
      code: result.status,
      error: result.error || (result.stderr || '').slice(0, 240)
    };
  }
}
module.exports = { VoiceService };
