const { paths } = require('./paths');

function boolEnv(name, defaultValue) {
  const value = process.env[name];
  if (value === undefined || value === '') return defaultValue;
  return ['1', 'true', 'yes', 'on'].includes(String(value).toLowerCase());
}

function csvEnv(name) {
  return (process.env[name] || '').split(',').map(x => x.trim()).filter(Boolean);
}

const config = {
  dataRoot: paths.dataRoot,
  dryRun: boolEnv('HANNA_DRY_RUN', true),
  activeUser: process.env.HANNA_ACTIVE_USER || 'local-root',
  vaultPassword: process.env.HANNA_VAULT_PASSWORD || '',
  totpSecret: process.env.HANNA_TOTP_SECRET || '',
  mqttBrokerUrl: process.env.HANNA_MQTT_BROKER_URL || '',
  mqttClientId: process.env.HANNA_MQTT_CLIENT_ID || 'hanna-node-lightweight',
  nodeRedUrl: process.env.HANNA_NODE_RED_URL || 'http://127.0.0.1:1880',
  allowDeploy: boolEnv('HANNA_ALLOW_DEPLOY', false),
  nasAllowlist: csvEnv('HANNA_NAS_ALLOWLIST'),
  maxTextBytes: Number(process.env.HANNA_MAX_TEXT_BYTES || 4096),
  maxReadEntries: Number(process.env.HANNA_MAX_READ_ENTRIES || 50),
  maxFileBytes: Number(process.env.HANNA_MAX_FILE_BYTES || 1024 * 1024)
};

module.exports = { config, boolEnv };
