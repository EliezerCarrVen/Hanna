const crypto = require('crypto'); const { config } = require('../core/config');
class TotpService {
  status() { return config.totpSecret ? { status: 'configured' } : { status: 'missing_configuration' }; }
  hotp(secret, counter, digits = 6, alg = 'sha1') {
    const key = Buffer.from(secret, 'base64'); const buf = Buffer.alloc(8); buf.writeBigUInt64BE(BigInt(counter));
    const hmac = crypto.createHmac(alg, key).update(buf).digest(); const o = hmac[hmac.length - 1] & 0xf;
    const code = ((hmac[o] & 0x7f) << 24 | (hmac[o + 1] & 0xff) << 16 | (hmac[o + 2] & 0xff) << 8 | (hmac[o + 3] & 0xff)) % (10 ** digits);
    return String(code).padStart(digits, '0');
  }
  current(secret = config.totpSecret) { if (!secret) return { status: 'missing_configuration' }; return { status: 'ok', code: this.hotp(secret, Math.floor(Date.now() / 30000)) }; }
}
module.exports = { TotpService };
