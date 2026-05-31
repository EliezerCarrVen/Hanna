using Hanna.Core;

namespace Hanna.Services;

internal sealed class GoogleIntegrationService
{
    private readonly AppConfig config;

    public GoogleIntegrationService(AppConfig config)
    {
        this.config = config;
    }

    public object BuildStatus()
    {
        return new
        {
            enabled = config.GoogleIntegrationEnabled,
            clientSecretsPath = config.GoogleClientSecretsPath,
            clientSecretsExists = File.Exists(config.GoogleClientSecretsPath),
            tokenDirectory = config.GoogleTokenDirectory,
            classroomPollMinutes = config.GoogleClassroomPollMinutes,
            note = "Preparado para OAuth de Google Classroom/Calendar/Drive. Requiere crear credenciales OAuth en Google Cloud y completar el flujo de autorización."
        };
    }

    public string GetSetupInstructions()
    {
        return "1) Crea un proyecto en Google Cloud. 2) Activa Google Classroom API, Calendar API y Drive API. 3) Crea credenciales OAuth Desktop. 4) Descarga el JSON como google_client_secret.json en la carpeta de Hanna. 5) Activa GOOGLE_INTEGRATION_ENABLED=true. Hanna ya tiene panel y estructura para tareas/cuadernos.";
    }
}
