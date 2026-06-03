class TelegramSecurityService {
  constructor(adminId = process.env.TELEGRAM_ADMIN_ID || '') { this.adminId = adminId; }
  isAdmin(userId) { return !this.adminId || String(userId) === String(this.adminId); }
  isSensitive(text) { return /^\/(docker|mqtt publicar|wol enviar|clamav escanear|vault importar|motor cambiar|fase cambiar)/i.test(String(text || '')); }
  authorize(text, userId) { if (this.isSensitive(text) && !this.isAdmin(userId)) return { ok: false, status: 'forbidden', message: 'Comando sensible restringido al administrador.' }; return { ok: true, status: 'ok' }; }
}
module.exports = { TelegramSecurityService };
