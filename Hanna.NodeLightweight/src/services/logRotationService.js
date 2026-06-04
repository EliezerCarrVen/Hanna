const fs = require('fs');
class LogRotationService { rotateIfLarge(file, maxBytes = 5 * 1024 * 1024) { if (fs.existsSync(file) && fs.statSync(file).size > maxBytes) fs.renameSync(file, `${file}.${Date.now()}.bak`); return { status: 'ok' }; } }
module.exports = { LogRotationService };
