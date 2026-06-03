const readline = require('readline');
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
if (require.main === module) main().catch(e => { console.error('Hanna startup error:', e.message); process.exit(1); });
module.exports = { main };
