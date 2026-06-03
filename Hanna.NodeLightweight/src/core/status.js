const { paths } = require('./paths'); const { config } = require('./config'); const { getModules } = require('./moduleRegistry');
function status() { return { name: 'Hanna.NodeLightweight', runtime: 'node', node: process.version, arch: process.arch, dataRoot: paths.dataRoot, dry_run: config.dryRun, modules: getModules().length }; }
module.exports = { status };
