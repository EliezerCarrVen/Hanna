const fs = require('fs'); const path = require('path'); const crypto = require('crypto');
const { paths } = require('../core/paths'); const { config } = require('../core/config');
const { ensureDir } = require('../utils/fsSafe'); const { guid } = require('../utils/crypto');
class VaultEncryptionService {
  status() { return config.vaultPassword ? { status: 'configured' } : { status: 'missing_configuration', message: 'HANNA_VAULT_PASSWORD no está configurado' }; }
  derive(password, salt) { return crypto.pbkdf2Sync(password, salt, 210000, 32, 'sha256'); }
  encryptText(plain, password = config.vaultPassword) {
    if (!password) return { ok: false, status: 'missing_configuration' };
    const salt = crypto.randomBytes(16); const iv = crypto.randomBytes(12); const key = this.derive(password, salt);
    const cipher = crypto.createCipheriv('aes-256-gcm', key, iv); const ciphertext = Buffer.concat([cipher.update(String(plain), 'utf8'), cipher.final()]);
    return { ok: true, alg: 'AES-256-GCM', kdf: 'PBKDF2-SHA256', salt: salt.toString('hex'), iv: iv.toString('hex'), tag: cipher.getAuthTag().toString('hex'), ciphertext: ciphertext.toString('hex') };
  }
  decryptText(payload, password = config.vaultPassword) {
    if (!password) return { ok: false, status: 'missing_configuration' };
    const key = this.derive(password, Buffer.from(payload.salt, 'hex')); const decipher = crypto.createDecipheriv('aes-256-gcm', key, Buffer.from(payload.iv, 'hex'));
    decipher.setAuthTag(Buffer.from(payload.tag, 'hex')); return Buffer.concat([decipher.update(Buffer.from(payload.ciphertext, 'hex')), decipher.final()]).toString('utf8');
  }
  writeEncrypted(plain, password = config.vaultPassword) {
    const payload = this.encryptText(plain, password); if (!payload.ok) return payload;
    ensureDir(paths.vaultDirs.bovedas); const file = path.join(paths.vaultDirs.bovedas, `${guid()}.vault.json`); fs.writeFileSync(file, JSON.stringify(payload, null, 2)); return { ok: true, file };
  }
}
module.exports = { VaultEncryptionService };
