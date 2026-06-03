const { appendJsonl, readJsonl, ensureFile } = require('../utils/fsSafe');
class JsonlStoreService {
  constructor(file) { this.file = file; ensureFile(file); }
  append(entry) { appendJsonl(this.file, entry); return entry; }
  read(limit = 50) { return readJsonl(this.file, limit); }
}
module.exports = { JsonlStoreService };
