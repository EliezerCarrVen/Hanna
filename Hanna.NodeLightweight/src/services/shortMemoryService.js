const { FlatFileMemoryService } = require('./flatFileMemoryService');
class ShortMemoryService extends FlatFileMemoryService {}
module.exports = { ShortMemoryService };
