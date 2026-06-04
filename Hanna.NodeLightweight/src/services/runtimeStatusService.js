const { status } = require('../core/status');
const { AppConfigService } = require('./appConfigService');
const { EngineStateService } = require('./engineStateService');
const { PhaseStateService } = require('./phaseStateService');
const { MemoryService } = require('./memoryService');
const { AuditLogService } = require('./auditLogService');
class RuntimeStatusService {
  getStatus() {
    return { ...status(), config: new AppConfigService().getSnapshot(), engine: new EngineStateService().current(), phase: new PhaseStateService().current(), memory: new MemoryService().status(), audit: new AuditLogService().verify() };
  }
}
module.exports = { RuntimeStatusService };
