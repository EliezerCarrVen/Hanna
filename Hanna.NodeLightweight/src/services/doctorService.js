const { DependencyCheckerService } = require('./dependencyCheckerService');
const { SystemDiagnosticsService } = require('./systemDiagnosticsService');
const { VaultEncryptionService } = require('./vaultEncryptionService');
const { MqttService } = require('./mqttService');
const { NasIndexerService } = require('./nasIndexerService');
const { SpotifyService } = require('./spotifyService');
const { ObsidianVaultService } = require('./obsidianVaultService');
const { EmotionStateService } = require('./emotionStateService');
const { LlmRouterService } = require('./llmRouterService');
const { RuntimeStatusService } = require('./runtimeStatusService');
const { EngineStateService } = require('./engineStateService');
const { PhaseStateService } = require('./phaseStateService');
class DoctorService {
  async run() {
    const deps = new DependencyCheckerService().checkAll();
    const runtime = new RuntimeStatusService().getStatus();
    return {
      status: 'ok',
      diagnostics: new SystemDiagnosticsService().diagnose(),
      runtime,
      dependencies: deps,
      telegram: runtime.telegram || runtime.config.telegram,
      memory: runtime.memory,
      audit: runtime.audit,
      engine: new EngineStateService().current(),
      phase: new PhaseStateService().current(),
      obsidian: new ObsidianVaultService().status(),
      emotions: new EmotionStateService().getState(),
      ai: new LlmRouterService().status(),
      blocked: { vault: new VaultEncryptionService().status(), mqtt: new MqttService().status(), nas: new NasIndexerService().status(), spotify: new SpotifyService().status() }
    };
  }
}
module.exports = { DoctorService };
