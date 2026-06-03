const { run, commandExists } = require('../utils/processRunner');
const deps = {
  node: 'apt install nodejs', npm: 'apt install npm', git: 'apt install git', rg: 'apt install ripgrep', mosquitto: 'apt install mosquitto-clients', docker: 'apt install docker.io', clamscan: 'apt install clamav', 'node-red': 'npm install -g --unsafe-perm node-red', curl: 'apt install curl', ping: 'apt install iputils-ping', systemctl: 'apt install systemd', timedatectl: 'apt install systemd', ip: 'apt install iproute2', hostname: 'apt install hostname'
};
class DependencyCheckerService {
  checkOne(command) {
    const bin = commandExists(command);
    if (!bin) return { name: command, status: 'missing_dependency', found: false, suggestion: deps[command] || 'instalar paquete Debian equivalente' };
    const versionArgs = command === 'ping' ? ['-V'] : ['--version'];
    const v = run(command, versionArgs, { timeout: 2500 });
    return { name: command, status: 'found', found: true, path: bin, version: (v.stdout || v.stderr).split('\n')[0].trim() };
  }
  checkAll() { return Object.keys(deps).map(d => this.checkOne(d)); }
}
module.exports = { DependencyCheckerService };
