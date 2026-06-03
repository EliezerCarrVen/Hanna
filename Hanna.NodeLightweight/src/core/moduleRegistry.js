function getModules() { return [
  { name: 'memoria', dangerous: false }, { name: 'codigo', dangerous: false }, { name: 'vault', dangerous: false }, { name: 'auditoria', dangerous: false }, { name: 'zeroleak', dangerous: false }, { name: 'intencion', dangerous: false },
  { name: 'nas', dangerous: false }, { name: 'mqtt', dangerous: true, dry_run: true }, { name: 'wol', dangerous: true, dry_run: true }, { name: 'clamav', dangerous: false }, { name: 'docker', dangerous: true, dry_run: true }, { name: 'nodered', dangerous: false, optional: true }, { name: 'serverless', dangerous: true, dry_run: true }, { name: 'sistema', dangerous: false }, { name: 'rbac', dangerous: false }, { name: 'totp', dangerous: false }
]; }
module.exports = { getModules };
