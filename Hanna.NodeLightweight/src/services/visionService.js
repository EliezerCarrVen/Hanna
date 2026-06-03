const fs = require('fs');
const { runAsync, commandExists } = require('../utils/processRunner');

class VisionService {
  constructor(options = {}) {
    this.commandExists = options.commandExists || commandExists;
    this.runAsync = options.runAsync || runAsync;
    this.readFileSync = options.readFileSync || fs.readFileSync;
  }

  status() {
    return this.commandExists('scrot')
      ? { status: 'available', dependency: 'scrot' }
      : { status: 'missing_dependency', dependency: 'scrot', detail: 'missing_scrot' };
  }

  async captureScreen(outputPath = '/tmp/hanna_screen.jpg') {
    if (!this.commandExists('scrot')) {
      return { ok: false, status: 'missing_dependency', error: 'missing_dependency', dependency: 'scrot' };
    }
    const result = await this.runAsync('scrot', ['-q', '70', '-o', outputPath], { timeout: 30000 });
    if (result.status !== 0) {
      return { ok: false, status: 'failed', error: result.error || result.stderr || 'scrot_failed', code: result.status };
    }
    try {
      const base64Image = this.readFileSync(outputPath, { encoding: 'base64' });
      return { ok: true, status: 'ok', path: outputPath, base64: base64Image };
    } catch (error) {
      return { ok: false, status: 'failed', error: error.message };
    }
  }
}
module.exports = { VisionService };
