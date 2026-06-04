const { ReactionService } = require('./reactionService');
class ResponseStyleService {
  constructor() { this.reactions = new ReactionService(); }
  apply(text, kind = 'neutral') { if (kind === 'error') return this.reactions.error(); if (kind === 'success') return this.reactions.success(text); return String(text || ''); }
}
module.exports = { ResponseStyleService };
