const { paths } = require('../core/paths'); const { MarkdownVaultService } = require('./markdownVaultService'); const { appendJsonl, ensureFile } = require('../utils/fsSafe');
class VaultIndexService { index() { ensureFile(paths.vaultIndex); const files = new MarkdownVaultService().list(); for (const file of files) appendJsonl(paths.vaultIndex, { timestamp: new Date().toISOString(), file }); return { status: 'ok', indexed: files.length }; } status() { return { file: paths.vaultIndex, exists: require('fs').existsSync(paths.vaultIndex) }; } }
module.exports = { VaultIndexService };
