const { spawnSync } = require('child_process');

const DEPENDENCIES = {
  node: {
    debian: 'sudo apt install nodejs',
    windows: 'Instalar Node.js LTS desde https://nodejs.org/ o usar winget install OpenJS.NodeJS.LTS',
    candidates: ['node', 'node.exe'],
    version: [['node', ['--version']]]
  },
  npm: {
    debian: 'sudo apt install npm',
    windows: 'npm se instala junto con Node.js; verifica que npm.cmd esté en PATH',
    candidates: ['npm', 'npm.cmd', 'npm.exe', 'npm.bat'],
    version: [['npm', ['--version']], ['npm.cmd', ['--version']]]
  },
  git: {
    debian: 'sudo apt install git',
    windows: 'Instalar Git for Windows o usar winget install Git.Git',
    candidates: ['git', 'git.exe', 'git.cmd'],
    version: [['git', ['--version']]]
  },
  rg: {
    debian: 'sudo apt install ripgrep',
    windows: 'Instalar ripgrep o usar winget install BurntSushi.ripgrep.MSVC',
    candidates: ['rg', 'rg.exe'],
    version: [['rg', ['--version']]]
  },
  mosquitto: {
    debian: 'sudo apt install mosquitto-clients mosquitto',
    windows: 'Instalar Mosquitto para Windows o usar un broker MQTT remoto configurado',
    candidates: ['mosquitto', 'mosquitto.exe'],
    version: [['mosquitto', ['--help']]]
  },
  docker: {
    debian: 'sudo apt install docker.io',
    windows: 'Instalar Docker Desktop; en HP Mini i386 normalmente dejar este módulo en dry-run',
    candidates: ['docker', 'docker.exe'],
    version: [['docker', ['--version']]]
  },
  clamscan: {
    debian: 'sudo apt install clamav',
    windows: 'Instalar ClamAV for Windows o dejar el módulo como missing_dependency',
    candidates: ['clamscan', 'clamscan.exe'],
    version: [['clamscan', ['--version']]]
  },
  'node-red': {
    debian: 'sudo npm install -g --unsafe-perm node-red',
    windows: 'npm install -g node-red',
    candidates: ['node-red', 'node-red.cmd', 'node-red.exe', 'node-red.bat'],
    version: [['node-red', ['--version']], ['node-red.cmd', ['--version']]]
  },
  curl: {
    debian: 'sudo apt install curl',
    windows: 'curl suele venir con Windows 10/11; si falta, instala curl o Git for Windows',
    candidates: ['curl', 'curl.exe'],
    version: [['curl', ['--version']]]
  },
  ping: {
    debian: 'sudo apt install iputils-ping',
    windows: 'ping viene incluido con Windows',
    candidates: ['ping', 'ping.exe'],
    version: [['ping', process.platform === 'win32' ? ['/?'] : ['-V']]]
  },
  systemctl: {
    debian: 'sudo apt install systemd',
    windows: 'No aplica en Windows; systemd solo se usa en Linux',
    windowsNotApplicable: true,
    candidates: ['systemctl'],
    version: [['systemctl', ['--version']]]
  },
  timedatectl: {
    debian: 'sudo apt install systemd',
    windows: 'No aplica en Windows; timedatectl solo se usa en Linux/systemd',
    windowsNotApplicable: true,
    candidates: ['timedatectl'],
    version: [['timedatectl', ['--version']]]
  },
  ip: {
    debian: 'sudo apt install iproute2',
    windows: 'No aplica igual en Windows; usar ipconfig para diagnóstico local',
    candidates: ['ip', 'ip.exe'],
    version: [['ip', ['-V']]]
  },
  hostname: {
    debian: 'sudo apt install hostname',
    windows: 'hostname viene incluido con Windows',
    candidates: ['hostname', 'hostname.exe'],
    version: [['hostname', []]]
  }
};

class DependencyCheckerService {
  constructor(options = {}) {
    this.platform = options.platform || process.platform;
    this.execPath = options.execPath || process.execPath;
    this.spawn = options.spawn || spawnSync;
  }

  checkOne(name) {
    const dependency = DEPENDENCIES[name] || {
      debian: 'Instalar paquete Debian 12 i386 equivalente',
      windows: 'Instalar herramienta equivalente para Windows',
      candidates: [name],
      version: [[name, ['--version']]]
    };

    if (this.platform === 'win32' && dependency.windowsNotApplicable) {
      return {
        name,
        status: 'not_applicable',
        found: false,
        message: `${name} no aplica en Windows; se usa solo en Linux/systemd.`,
        suggestion: dependency.debian,
        suggestion_debian12_i386: dependency.debian,
        suggestion_windows: dependency.windows
      };
    }

    const path = this.findExecutable(name, dependency);
    const version = this.getVersion(name, dependency, path);
    const found = Boolean(path || version);

    if (!found) {
      return {
        name,
        status: 'missing_dependency',
        found: false,
        suggestion: dependency.debian,
        suggestion_debian12_i386: dependency.debian,
        suggestion_windows: dependency.windows
      };
    }

    return {
      name,
      status: 'found',
      found: true,
      path: path || '',
      version: version || '',
      suggestion: dependency.debian,
      suggestion_debian12_i386: dependency.debian,
      suggestion_windows: dependency.windows
    };
  }

  checkAll() {
    return Object.keys(DEPENDENCIES).map(name => this.checkOne(name));
  }

  findExecutable(name, dependency) {
    if (name === 'node' && this.execPath) return this.execPath;
    const candidates = dependency.candidates || [name];
    if (this.platform === 'win32') return this.findWindows(candidates);
    return this.findUnix(candidates[0] || name);
  }

  findWindows(candidates) {
    for (const candidate of candidates) {
      const result = this.spawn('where.exe', [candidate], { encoding: 'utf8', timeout: 2500, windowsHide: true });
      if (result.status === 0 && result.stdout) return result.stdout.split(/\r?\n/).map(x => x.trim()).find(Boolean) || '';
    }
    return '';
  }

  findUnix(command) {
    const commandV = this.spawn('sh', ['-c', `command -v ${shellQuote(command)}`], { encoding: 'utf8', timeout: 2500 });
    if (commandV.status === 0 && commandV.stdout) return firstLine(commandV.stdout);

    const which = this.spawn('sh', ['-c', `if command -v which >/dev/null 2>&1; then which ${shellQuote(command)}; fi`], { encoding: 'utf8', timeout: 2500 });
    if (which.status === 0 && which.stdout) return firstLine(which.stdout);
    return '';
  }

  getVersion(name, dependency, foundPath) {
    const versionCommands = dependency.version || [[name, ['--version']]];
    const attempts = [];

    if (name === 'node' && this.execPath) attempts.push([this.execPath, ['--version']]);
    if (foundPath) attempts.push([foundPath, versionCommands[0][1] || ['--version']]);
    attempts.push(...versionCommands);

    for (const [command, args] of attempts) {
      const result = this.spawn(command, args, {
        encoding: 'utf8',
        timeout: 3000,
        windowsHide: true,
        shell: this.platform === 'win32' && /\.(cmd|bat)$/i.test(command)
      });
      const output = `${result.stdout || ''}${result.stderr || ''}`.trim();
      if (result.status === 0 || output) return firstLine(output);
    }
    return '';
  }
}

function firstLine(text) {
  return String(text || '').split(/\r?\n/).map(x => x.trim()).find(Boolean) || '';
}

function shellQuote(value) {
  return `'${String(value).replace(/'/g, `'\\''`)}'`;
}

module.exports = { DependencyCheckerService, DEPENDENCIES };
