const { commandExists, run } = require('../utils/processRunner'); const { config } = require('../core/config');
class DockerPlannerService { status() { if (!commandExists('docker')) return { status: 'missing_dependency', dependency: 'docker', dry_run: true }; const r = run('docker', ['--version']); return { status: 'found', dry_run: true, deploy_allowed: config.allowDeploy, version: r.stdout.trim() }; } }
module.exports = { DockerPlannerService };
