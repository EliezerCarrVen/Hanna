const fs = require('fs');
const path = require('path');
const { paths } = require('../core/paths');
const { ensureDir } = require('../utils/fsSafe');

class EmotionStateService {
  constructor(file = path.join(paths.runtime, 'emotion_state.json')) { this.file = file; }
  defaultState() { return { status: 'ok', mood: 'enfocada', energy: 0.74, confidence: 0.82, mode: 'asistente_edge', tone: 'cálido, claro y directo', last_reaction: 'lista para ayudar', updated_at: new Date().toISOString() }; }
  getState() { try { return fs.existsSync(this.file) ? { ...this.defaultState(), ...JSON.parse(fs.readFileSync(this.file, 'utf8')) } : this.defaultState(); } catch { return this.defaultState(); } }
  update(patch = {}) { const state = { ...this.getState(), ...patch, updated_at: new Date().toISOString() }; ensureDir(path.dirname(this.file)); fs.writeFileSync(this.file, JSON.stringify(state, null, 2)); return state; }
  recordReaction(type, message) { return this.update({ last_reaction: message, last_reaction_type: type }); }
}
module.exports = { EmotionStateService };
