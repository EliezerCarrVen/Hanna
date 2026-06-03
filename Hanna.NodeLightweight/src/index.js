const readline = require('readline');
const { SafeLogService } = require('./services/safeLogService');
const logger = new SafeLogService();

process.on('uncaughtException', (error) => {
  logger.write('CRITICAL_CRASH', { error: error.message, stack: error.stack });
  console.error('Error crítico interceptado. Hanna sigue viva (o se reiniciará).', error.message);
});

process.on('unhandledRejection', (reason) => {
  const message = reason && reason.message ? reason.message : String(reason);
  logger.write('UNHANDLED_PROMISE', { reason: message });
  console.error('Promesa no manejada interceptada por Hanna.', message);
});
const { StartupService } = require('./services/startupService');
const { CommandRouter } = require('./cli/commandRouter');

async function main() {
  new StartupService().ensureDataLayout();
  const router = new CommandRouter();
  const args = process.argv.slice(2);
  const onceIndex = args.indexOf('--once');
  if (onceIndex >= 0) {
    const command = args.slice(onceIndex + 1).join(' ') || '/status';
    const output = await router.run(command, { source: 'cli', mode: command.toLowerCase().startsWith('/json ') ? 'json' : 'human' });
    console.log(output);
    return;
  }
  console.log('Hanna.NodeLightweight conversacional listo. Escribe /help o /salir.');
  const rl = readline.createInterface({ input: process.stdin, output: process.stdout, prompt: 'hanna> ' });
  rl.prompt();
  rl.on('line', async line => {
    const out = await router.run(line, { source: 'cli', mode: line.trim().toLowerCase().startsWith('/json ') ? 'json' : 'human' });
    console.log(out);
    if (line.trim() === '/salir') rl.close(); else rl.prompt();
  });
}
if (require.main === module) main().catch(error => { logger.write('STARTUP_ERROR', { error: error.message, stack: error.stack }); console.error('Hanna startup error:', error.message); process.exit(1); });
module.exports = { main };
