const net = require('net'); const { URL } = require('url'); const { config } = require('../core/config');
class MqttService {
  status() { if (!config.mqttBrokerUrl) return { status: 'missing_configuration', dry_run: true, message: 'HANNA_MQTT_BROKER_URL no está configurado' }; return { status: 'configured', broker: config.mqttBrokerUrl, dry_run: true }; }
  async publish(topic, message, options = {}) {
    const dry = options.dry_run !== undefined ? options.dry_run : true;
    if (!config.mqttBrokerUrl) return { ok: false, status: 'missing_configuration', dry_run: dry };
    if (dry) return { ok: true, status: 'dry_run', dry_run: true, topic, bytes: Buffer.byteLength(String(message || '')) };
    return { ok: false, status: 'service_unavailable', message: 'Publicación MQTT real requiere cliente MQTT externo; use mosquitto_pub.' };
  }
  pingBroker(timeout = 1500) { if (!config.mqttBrokerUrl) return Promise.resolve({ status: 'missing_configuration' }); const u = new URL(config.mqttBrokerUrl); return new Promise(resolve => { const s = net.connect(Number(u.port || 1883), u.hostname); const done = r => { s.destroy(); resolve(r); }; s.setTimeout(timeout, () => done({ status: 'service_unavailable' })); s.on('connect', () => done({ status: 'reachable' })); s.on('error', e => done({ status: 'service_unavailable', error: e.message })); }); }
}
module.exports = { MqttService };
