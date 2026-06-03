const { PersonaService } = require('./personaService');
const { EmotionStateService } = require('./emotionStateService');
class PersonalityRuntimeService {
  constructor() { this.persona = new PersonaService(); this.emotions = new EmotionStateService(); }
  status() { return { status: 'ok', identity: 'Hanna.NodeLightweight', style: 'cálida, directa, honesta y útil', emotions: this.emotions.getState() }; }
  capabilities() { return this.persona.capabilities(); }
}
module.exports = { PersonalityRuntimeService };
