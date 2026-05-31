namespace Hanna.Services;

internal sealed class CommunicationService
{
    public string SendMessagePlaceholder(string text)
    {
        return "La skill de mensajes está preparada, pero falta conectar una API real de WhatsApp/Telegram externo. Por seguridad no enviaré mensajes sin configuración.";
    }
}
