const fs = require('fs');
const os = require('os');
const path = require('path');
const https = require('https');
const { runAsync, commandExists } = require('../utils/processRunner');
const { paths } = require('../core/paths');

class VoiceService {
  constructor(options = {}) {
    this.commandExists = options.commandExists || commandExists;
    this.runAsync = options.runAsync || runAsync;
    this.voiceStatePath = path.join(paths.dataRoot, 'config', 'voice.json');
    this.azureVoicesPath = path.join(paths.dataRoot, 'config', 'azure-voices.json');
  }

  normalizeAlias(value) {
    return String(value || '')
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase()
      .trim();
  }

  readJson(file, fallback) {
    try {
      if (!fs.existsSync(file)) return fallback;
      return JSON.parse(fs.readFileSync(file, 'utf8'));
    } catch {
      return fallback;
    }
  }

  writeJson(file, data) {
    fs.mkdirSync(path.dirname(file), { recursive: true });
    fs.writeFileSync(file, JSON.stringify(data, null, 2));
  }

  loadAzureVoices() {
    const config = this.readJson(this.azureVoicesPath, { voices: {} });
    return config.voices || {};
  }

  getVoiceState() {
    return this.readJson(this.voiceStatePath, {
      provider: process.env.HANNA_TTS_PROVIDER || 'azure',
      alias: process.env.HANNA_TTS_VOICE_ALIAS || 'dalia'
    });
  }

  getCurrentVoice() {
    const state = this.getVoiceState();
    const alias = this.normalizeAlias(state.alias || process.env.HANNA_TTS_VOICE_ALIAS || 'dalia');
    const voices = this.loadAzureVoices();

    if (voices[alias]) {
      return voices[alias];
    }

    if (process.env.AZURE_SPEECH_VOICE) {
      return {
        alias: 'env',
        shortName: process.env.AZURE_SPEECH_VOICE,
        locale: process.env.AZURE_SPEECH_LOCALE || 'es-MX',
        gender: 'Unknown',
        displayName: process.env.AZURE_SPEECH_VOICE
      };
    }

    return null;
  }

  listVoices() {
    const voices = this.loadAzureVoices();
    const current = this.getCurrentVoice();

    return {
      type: 'voice_list',
      provider: 'azure',
      current,
      voices
    };
  }

  setVoice(alias) {
    const requested = this.normalizeAlias(alias);
    const voices = this.loadAzureVoices();

    if (!requested) {
      return {
        ok: false,
        status: 'missing_voice',
        message: 'Indica una voz: estrella, karla, dalia, camila o tania.',
        available: Object.keys(voices)
      };
    }

    if (!voices[requested]) {
      return {
        ok: false,
        status: 'voice_not_available',
        requested,
        message: `No encontré la voz "${requested}" en las voces seleccionadas.`,
        available: Object.keys(voices).filter(key => voices[key])
      };
    }

    const state = {
      provider: 'azure',
      alias: requested,
      voice: voices[requested],
      updated_at: new Date().toISOString()
    };

    this.writeJson(this.voiceStatePath, state);

    return {
      ok: true,
      status: 'voice_changed',
      alias: requested,
      voice: voices[requested],
      human: `Listo. Cambié la voz de Hanna a ${requested}.`
    };
  }

  status() {
    const hasAzure = Boolean(process.env.AZURE_SPEECH_KEY && process.env.AZURE_SPEECH_REGION);
    const hasFestival = Boolean(this.commandExists('text2wave') && this.commandExists('aplay'));
    const hasFlite = Boolean(this.commandExists('flite'));
    const hasEspeak = Boolean(this.commandExists('espeak-ng'));
    const hasArecord = Boolean(this.commandExists('arecord'));

    return {
      status: hasAzure || hasFestival || hasFlite || hasEspeak || hasArecord ? 'available' : 'missing_dependency',
      tts: hasAzure ? 'azure' : hasFestival ? 'festival' : hasFlite ? 'flite' : hasEspeak ? 'espeak-ng' : 'missing_dependency',
      current_voice: this.getCurrentVoice(),
      azure: hasAzure ? 'available' : 'missing_configuration',
      festival: hasFestival ? 'available' : 'missing_dependency',
      flite: hasFlite ? 'available' : 'missing_dependency',
      espeak: hasEspeak ? 'available' : 'missing_dependency',
      stt: hasArecord ? 'available' : 'missing_dependency'
    };
  }

  cleanText(text) {
    return String(text || '')
      .replace(/["`$\\]/g, '')
      .replace(/\s+/g, ' ')
      .trim()
      .slice(0, 500);
  }

  escapeXml(text) {
    return String(text || '')
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&apos;');
  }

  async speak(text) {
    const cleanText = this.cleanText(text);

    if (!cleanText) {
      return { ok: false, status: 'empty_text', error: 'empty_text' };
    }

    if (
      process.env.HANNA_TTS_PROVIDER === 'azure' &&
      process.env.AZURE_SPEECH_KEY &&
      process.env.AZURE_SPEECH_REGION
    ) {
      const azureResult = await this.speakAzure(cleanText);
      if (azureResult.ok) return azureResult;
    }

    if (this.commandExists('text2wave') && this.commandExists('aplay')) {
      const festivalResult = await this.speakFestival(cleanText);
      if (festivalResult.ok) return festivalResult;
    }

    if (this.commandExists('flite')) {
      const result = await this.runAsync('flite', ['-t', cleanText], { timeout: 30000 });
      return {
        ok: result.status === 0,
        status: result.status === 0 ? 'spoken' : 'tts_error',
        engine: 'flite',
        error: result.error || result.stderr || ''
      };
    }

    if (this.commandExists('espeak-ng')) {
      const result = await this.runAsync(
        'espeak-ng',
        ['-v', 'es+f3', '-s', '135', '-a', '160', cleanText],
        { timeout: 30000 }
      );

      return {
        ok: result.status === 0,
        status: result.status === 0 ? 'spoken' : 'tts_error',
        engine: 'espeak-ng',
        error: result.error || result.stderr || ''
      };
    }

    return {
      ok: false,
      status: 'missing_dependency',
      dependency: 'azure, festival, flite or espeak-ng'
    };
  }

  async speakAzure(text) {
    const voice = this.getCurrentVoice();

    if (!voice || !voice.shortName) {
      return {
        ok: false,
        status: 'missing_voice',
        engine: 'azure',
        error: 'No hay voz Azure seleccionada.'
      };
    }

    const region = process.env.AZURE_SPEECH_REGION;
    const key = process.env.AZURE_SPEECH_KEY;
    const output = process.env.AZURE_SPEECH_OUTPUT || 'riff-24khz-16bit-mono-pcm';
    const locale = voice.locale || 'es-MX';

    const ssml = `<speak version="1.0" xml:lang="${locale}"><voice name="${voice.shortName}">${this.escapeXml(text)}</voice></speak>`;
    const wavPath = path.join(os.tmpdir(), `hanna_azure_${Date.now()}.wav`);

    const options = {
      hostname: `${region}.tts.speech.microsoft.com`,
      path: '/cognitiveservices/v1',
      method: 'POST',
      headers: {
        'Ocp-Apim-Subscription-Key': key,
        'Content-Type': 'application/ssml+xml',
        'X-Microsoft-OutputFormat': output,
        'User-Agent': 'HannaNodeLightweight',
        'Content-Length': Buffer.byteLength(ssml)
      },
      timeout: 30000
    };

    const audio = await new Promise((resolve) => {
      const req = https.request(options, (res) => {
        const chunks = [];

        res.on('data', (chunk) => chunks.push(chunk));
        res.on('end', () => {
          const buffer = Buffer.concat(chunks);

          if (res.statusCode >= 200 && res.statusCode < 300) {
            resolve({ ok: true, buffer });
          } else {
            resolve({ ok: false, error: buffer.toString('utf8'), statusCode: res.statusCode });
          }
        });
      });

      req.on('error', (error) => resolve({ ok: false, error: error.message }));
      req.write(ssml);
      req.end();
    });

    if (!audio.ok) {
      return {
        ok: false,
        status: 'tts_error',
        engine: 'azure',
        voice,
        error: audio.error || 'azure_tts_failed'
      };
    }

    fs.writeFileSync(wavPath, audio.buffer);

    const alsaDevice = process.env.HANNA_ALSA_DEVICE;
    const aplayArgs = alsaDevice ? ['-D', alsaDevice, wavPath] : [wavPath];
    const play = await this.runAsync('aplay', aplayArgs, { timeout: 30000 });

    try {
      fs.unlinkSync(wavPath);
    } catch {}

    return {
      ok: play.status === 0,
      status: play.status === 0 ? 'spoken' : 'playback_error',
      engine: 'azure',
      voice,
      error: play.error || play.stderr || ''
    };
  }

  async speakFestival(text) {
    const txtPath = path.join(os.tmpdir(), `hanna_festival_${Date.now()}.txt`);
    const wavPath = `${txtPath}.wav`;

    fs.writeFileSync(txtPath, text, 'utf8');

    const makeWav = await this.runAsync('text2wave', [txtPath, '-o', wavPath], { timeout: 30000 });

    if (makeWav.status !== 0) {
      try { fs.unlinkSync(txtPath); } catch {}
      return {
        ok: false,
        status: 'tts_error',
        engine: 'festival',
        error: makeWav.error || makeWav.stderr || ''
      };
    }

    const alsaDevice = process.env.HANNA_ALSA_DEVICE;
    const aplayArgs = alsaDevice ? ['-D', alsaDevice, wavPath] : [wavPath];
    const play = await this.runAsync('aplay', aplayArgs, { timeout: 30000 });

    try { fs.unlinkSync(txtPath); } catch {}
    try { fs.unlinkSync(wavPath); } catch {}

    return {
      ok: play.status === 0,
      status: play.status === 0 ? 'spoken' : 'playback_error',
      engine: 'festival',
      error: play.error || play.stderr || ''
    };
  }

  async record(durationSeconds = 5, outputPath = '/tmp/hanna_record.wav') {
    if (!this.commandExists('arecord')) {
      return {
        ok: false,
        status: 'missing_dependency',
        dependency: 'arecord'
      };
    }

    const seconds = Math.max(1, Math.min(Number(durationSeconds) || 5, 30));

    const result = await this.runAsync(
      'arecord',
      ['-d', String(seconds), '-f', 'cd', outputPath],
      { timeout: (seconds + 5) * 1000 }
    );

    return {
      ok: result.status === 0,
      status: result.status === 0 ? 'recorded' : 'record_error',
      path: outputPath,
      error: result.error || result.stderr || ''
    };
  }
}

module.exports = { VoiceService };
