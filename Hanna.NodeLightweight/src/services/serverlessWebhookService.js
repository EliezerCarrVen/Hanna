class ServerlessWebhookService { status() { return { status: 'missing_configuration', message: 'No hay endpoint webhook configurado', dry_run: true }; } }
module.exports = { ServerlessWebhookService };
