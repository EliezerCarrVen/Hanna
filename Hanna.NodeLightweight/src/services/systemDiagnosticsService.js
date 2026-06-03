const os = require('os'); const { networkInterfaces } = require('os'); const { commandExists, run } = require('../utils/processRunner');
class SystemDiagnosticsService {
  ipLocal() { const nets = networkInterfaces(); const out = []; for (const addrs of Object.values(nets)) for (const a of addrs || []) if (a.family === 'IPv4' && !a.internal) out.push(a.address); return out; }
  diagnose() { return { os: `${os.type()} ${os.release()}`, platform: os.platform(), arch: os.arch(), ram_mb: Math.round(os.totalmem() / 1024 / 1024), ip_local: this.ipLocal(), systemd: !!commandExists('systemctl'), ntp: commandExists('timedatectl') ? run('timedatectl', ['show', '-p', 'NTPSynchronized', '--value']).stdout.trim() : 'missing_dependency', hostname: os.hostname() }; }
}
module.exports = { SystemDiagnosticsService };
