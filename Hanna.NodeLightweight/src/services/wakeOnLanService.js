const dgram = require('dgram'); const { config } = require('../core/config');
class WakeOnLanService {
  isValidMac(mac) { return /^([0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}$/.test(String(mac || '')); }
  status() { return { status: 'available', dry_run: true, note: 'WOL no envía paquetes salvo dry_run=false explícito.' }; }
  buildPacket(mac) { if (!this.isValidMac(mac)) throw new Error('invalid_mac'); const bytes = mac.replace(/[:-]/g, '').match(/.{2}/g).map(x => parseInt(x, 16)); return Buffer.concat([Buffer.alloc(6, 0xff), ...Array.from({ length: 16 }, () => Buffer.from(bytes))]); }
  async send(mac, options = {}) {
    const dry = options.dry_run !== undefined ? options.dry_run : true;
    if (!this.isValidMac(mac)) return { ok: false, status: 'invalid_mac', dry_run: dry };
    if (dry) return { ok: true, status: 'dry_run', dry_run: true, mac };
    return new Promise(resolve => { const socket = dgram.createSocket('udp4'); socket.once('error', e => { socket.close(); resolve({ ok: false, status: 'missing_hardware_or_network', error: e.message }); }); socket.bind(() => { socket.setBroadcast(true); socket.send(this.buildPacket(mac), 9, options.broadcast || '255.255.255.255', err => { socket.close(); resolve(err ? { ok: false, status: 'missing_hardware_or_network', error: err.message } : { ok: true, status: 'sent', dry_run: false }); }); }); });
  }
}
module.exports = { WakeOnLanService };
