const { spawnSync } = require('child_process');
const os = require('os');

const DEPENDENCIES = {
  node: {
    windows: ['node.exe', 'node'],
    linux: ['node'],
    versionArgs: ['--version'],
    suggestion_debian12_i386: 'apt install nodejs',
    suggestion_windows: 'Instalar Node.js o agregar node.exe al PATH'
  },
  npm: {
    windows: ['npm.cmd', 'npm.exe', 'npm'],
    linux: ['npm'],
    versionArgs: ['--version'],
    suggestion_debian12_i386: 'apt install npm',
    suggestion_windows: 'Instalar npm con Node.js o agregar npm.cmd al PATH'
  },
  git: {
    windows: ['git.exe', 'git.cmd', 'git'],
    linux: ['git'],
    versionArgs: ['--version'],
    suggestion_debian12_i386: 'apt install git',
    suggestion_windows: 'Instalar Git for Windows o agregar git.exe al PATH'
  },
  rg: {
    windows: ['rg.exe', 'rg'],
    linux: ['rg'],
    versionArgs: ['--version'],
    suggestion_debian12_i386: 'apt install ripgrep',
    suggestion_windows: 'Instalar ripgrep o agregar rg.exe al PATH'
  },
  mosquitto: {
    windows: ['mosquitto.exe', 'mosquitto'],
    linux: ['mosquitto'],
    versionArgs: ['-h'],
    suggestion_debian12_i386: 'apt install mosquitto mosquitto-clients',
    suggestion_windows: 'Instalar Mosquitto para Windows o usar un broker MQTT remoto'
  },
  docker: {
    windows: ['docker.exe', 'docker'],
    linux: ['docker'],
    versionArgs: ['--version'],
    suggestion_debian12_i386: 'apt install docker.io',
    suggestion_windows: 'Instalar Docker Desktop si el equipo lo soporta'
  },
  clamscan: {
    windows: ['clamscan.exe', 'clamscan'],
    linux: ['clamscan'],
    versionArgs: ['--version'],
    suggestion_debian12_i386: 'apt install clamav',
    suggestion_windows: 'Instalar ClamAV para Windows o usar ClamAV en Debian'
  },
  'node-red': {
    windows: ['node-red.cmd', 'node-red.exe', 'node-red'],
    linux: ['node-red'],
    versionArgs: ['--version'],
    suggestion_debian12_i386: 'npm install -g --unsafe-perm node-red',
    suggestion_windows: 'npm install -g node-red'
  },
  curl: {
    windows: ['curl.exe', 'curl'],
    linux: ['curl'],
    versionArgs: ['--version'],
    suggestion_debian12_i386: 'apt install curl',
    suggestion_windows: 'Usar curl incluido en Windows o instalarlo'
  },
  ping: {
    windows: ['ping.exe', 'ping'],
    linux: ['ping'],
    versionArgs: ['-V'],
    suggestion_debian12_i386: 'apt install iputils-ping',
    suggestion_windows: 'ping viene incluido con Windows'
  },
  systemctl: {
    windows: ['systemctl'],
    linux: ['systemctl'],
    systemdOnly: true,
    versionArgs: ['--version'],
    suggestion_debian12_i386: 'apt install systemd',
    suggestion_windows: 'No aplica en Windows; se usa Service Control Manager'
  },
  timedatectl: {
    windows: ['timedatectl'],
    linux: ['timedatectl'],
    systemdOnly: true,
    versionArgs: ['--version'],
    suggestion_debian12_i386: 'apt install systemd',
    suggestion_windows: 'No aplica en Windows; revisar hora desde Configuración o PowerShell'
  },
  ip: {
    windows: ['ip.exe', 'ip'],
    linux: ['ip'],
    versionArgs: ['-V'],
    suggestion_debian12_i386: 'apt install iproute2',
    suggestion_windows: 'No aplica normalmente en Windows; usar ipconfig'
  },
  hostname: {
    windows: ['hostname.exe', 'hostname'],
    linux: ['hostname'],
    versionArgs: [],
    suggestion_debian12_i386: 'apt install hostname',
    suggestion_windows: 'hostname viene incluido con Windows'
  }
};

function isWindows() {
  return process.platform === 'win32';
}

function run(command, args = [], options = {}) {
  const useShell = options.shell ?? (isWindows() && /\.(cmd|bat)$/i.test(command));
  const result = spawnSync(command, args, {
    encoding: 'utf8',
    timeout: options.timeout || 2500,
    maxBuffer: 1024 * 1024,
    shell: useShell,
    windowsHide: true
  });

  return {
    status: result.status,
    stdout: result.stdout || '',
    stderr: result.stderr || '',
    error: result.error ? result.error.message : ''
  };
}

function firstLine(text) {
  return String(text || '').split(/\r?\n/).map(line => line.trim()).find(Boolean) || '';
}

function findWindowsExecutable(candidates) {
  for (const candidate of candidates) {
    const found = run('where.exe', [candidate], { shell: false });
    if (found.status === 0) {
      const line = firstLine(found.stdout);
      if (line) return line;
    }
  }

  return '';
}

function findLinuxExecutable(candidates) {
  for (const candidate of candidates) {
    const escaped = candidate.replace(/'/g, "'\\''");
    const found = run('/bin/sh', ['-lc', `command -v '${escaped}' || which '${escaped}'`], { shell: false });
    if (found.status === 0) {
      const line = firstLine(found.stdout);
      if (line) return line;
    }
  }

  return '';
}

function detectExecutable(name, metadata) {
  if (name === 'node' && process.execPath) {
    return process.execPath;
  }

  return isWindows()
    ? findWindowsExecutable(metadata.windows || [name])
    : findLinuxExecutable(metadata.linux || [name]);
}

function detectVersion(name, executable, metadata) {
  if (name === 'node') {
    return process.version;
  }

  const args = metadata.versionArgs || ['--version'];
  const result = run(executable || name, args, { timeout: 3000 });
  if (result.status === 0 || result.stdout || result.stderr) {
    return firstLine(result.stdout || result.stderr);
  }

  return '';
}

class DependencyCheckerService {
  checkOne(command) {
    const metadata = DEPENDENCIES[command] || {
      windows: [command],
      linux: [command],
      versionArgs: ['--version'],
      suggestion_debian12_i386: 'instalar paquete Debian equivalente',
      suggestion_windows: 'instalar herramienta equivalente para Windows'
    };

    if (isWindows() && metadata.systemdOnly) {
      return {
        name: command,
        status: 'not_applicable',
        found: false,
        message: `${command} es una herramienta de systemd/Linux y no aplica en Windows`,
        suggestion_debian12_i386: metadata.suggestion_debian12_i386,
        suggestion_windows: metadata.suggestion_windows,
        suggestion: metadata.suggestion_debian12_i386
      };
    }

    const executable = detectExecutable(command, metadata);
    if (!executable) {
      return {
        name: command,
        status: 'missing_dependency',
        found: false,
        suggestion_debian12_i386: metadata.suggestion_debian12_i386,
        suggestion_windows: metadata.suggestion_windows,
        suggestion: metadata.suggestion_debian12_i386
      };
    }

    return {
      name: command,
      status: 'found',
      found: true,
      path: executable,
      version: detectVersion(command, executable, metadata),
      suggestion_debian12_i386: metadata.suggestion_debian12_i386,
      suggestion_windows: metadata.suggestion_windows,
      suggestion: metadata.suggestion_debian12_i386
    };
  }

  checkAll() {
    return Object.keys(DEPENDENCIES).map(dep => this.checkOne(dep));
  }
}

module.exports = { DependencyCheckerService };