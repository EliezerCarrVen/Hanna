const crypto = require('crypto');
function sha256(value) { return crypto.createHash('sha256').update(String(value)).digest('hex'); }
function guid() { return crypto.randomUUID ? crypto.randomUUID() : crypto.randomBytes(16).toString('hex'); }
function constantTimeEqual(a, b) {
  const aa = Buffer.from(String(a)); const bb = Buffer.from(String(b));
  return aa.length === bb.length && crypto.timingSafeEqual(aa, bb);
}
module.exports = { sha256, guid, constantTimeEqual };
