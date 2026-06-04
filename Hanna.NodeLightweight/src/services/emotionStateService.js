'use strict';

const fs = require('fs');
const path = require('path');
const { paths } = require('../core/paths');
const { ensureDir } = require('../utils/fsSafe');

const NEUTRAL_STATE = {
  status: 'ok',
  mood: 'enfocada',
  energy: 0.74,
  patience: 0.72,
  curiosity: 0.70,
  confidence: 0.82,
  mode: 'asistente_edge',
  tone: 'cálido, claro y directo',
  last_reaction: 'lista para ayudar'
};

const EVENT_DELTAS = {
  task_success: { mood: 'satisfecha', energy: 0.03, confidence: 0.06, patience: 0.02 },
  task_failed: { mood: 'alerta', energy: -0.02, confidence: -0.05, patience: -0.03 },
  user_thanks: { mood: 'de buen ánimo', patience: 0.03, confidence: 0.02 },
  unknown_command: { patience: -0.02, curiosity: 0.02 },
  repeated_error: { mood: 'frustrada pero controlada', patience: -0.07, confidence: -0.04 },
  wakeup: { mood: 'activa', energy: 0.05 },
  user_frustrated: { mood: 'cuidadosa', patience: -0.03 }
};

class EmotionStateService {
  constructor(file = path.join(paths.runtime, 'emotion_state.json')) {
    this.file = file;
  }

  defaultState() {
    return {
      ...NEUTRAL_STATE,
      updated_at: new Date().toISOString()
    };
  }

  clamp(value) {
    return Math.max(0, Math.min(1, Number(value)));
  }

  getState() {
    try {
      if (!fs.existsSync(this.file)) return this.defaultState();
      const parsed = JSON.parse(fs.readFileSync(this.file, 'utf8'));
      return { ...this.defaultState(), ...parsed };
    } catch {
      return this.defaultState();
    }
  }

  update(patch = {}) {
    const current = this.getState();
    const state = { ...current, ...patch, updated_at: new Date().toISOString() };

    for (const key of ['energy', 'patience', 'curiosity', 'confidence']) {
      state[key] = this.clamp(state[key]);
    }

    ensureDir(path.dirname(this.file));
    fs.writeFileSync(this.file, JSON.stringify(state, null, 2), 'utf8');
    return state;
  }

  applyEvent(type) {
    const delta = EVENT_DELTAS[type];
    if (!delta) return this.getState();

    const current = this.getState();
    const patch = {};

    for (const [key, value] of Object.entries(delta)) {
      if (typeof value === 'number') patch[key] = this.clamp((current[key] ?? NEUTRAL_STATE[key] ?? 0.5) + value);
      else patch[key] = value;
    }

    return this.update(patch);
  }

  applyCustom(deltas = {}) {
    const current = this.getState();
    const patch = {};

    for (const [key, value] of Object.entries(deltas)) {
      if (typeof value === 'number' && key in NEUTRAL_STATE) patch[key] = this.clamp((current[key] ?? NEUTRAL_STATE[key]) + value);
      else patch[key] = value;
    }

    return this.update(patch);
  }

  recordReaction(type, message) {
    const state = this.applyEvent(type);
    return this.update({
      ...state,
      last_reaction: message,
      last_reaction_type: type
    });
  }

  buildEmotionContext() {
    const state = this.getState();
    return `Estado emocional interno: ánimo ${state.mood}, energía ${Math.round(state.energy * 100)}%, paciencia ${Math.round(state.patience * 100)}%, curiosidad ${Math.round(state.curiosity * 100)}% y confianza ${Math.round(state.confidence * 100)}%. Debe reflejarse sutilmente en el tono sin mencionarlo directamente.`;
  }

  summary() {
    const state = this.getState();
    return {
      mood: state.mood,
      energy: `${Math.round(state.energy * 100)}%`,
      patience: `${Math.round(state.patience * 100)}%`,
      curiosity: `${Math.round(state.curiosity * 100)}%`,
      confidence: `${Math.round(state.confidence * 100)}%`,
      tone: state.tone,
      last_reaction: state.last_reaction
    };
  }
}

module.exports = { EmotionStateService };
