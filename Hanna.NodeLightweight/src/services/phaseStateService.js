class PhaseStateService {
  current() { return { current: process.env.HANNA_PHASE || 'node-lightweight', status: 'ok', profile: 'HP Mini i386 / bajo consumo', compatible_with: 'PhaseService C# mínimo' }; }
}
module.exports = { PhaseStateService };
