const { CommandRouter } = require('../cli/commandRouter');
class NodeRedBridge {
  constructor(router = new CommandRouter()) { this.router = router; }
  status() { return { status: 'optional', integration: 'nodered', message: 'Node-RED puede invocar CommandRouter.run desde flujos externos.' }; }
  async handle(payload) { return this.router.run(String(payload || ''), { source: 'nodered', mode: 'human' }); }
}
module.exports = { NodeRedBridge };
