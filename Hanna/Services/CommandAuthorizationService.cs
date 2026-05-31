using Hanna.Core;

namespace Hanna.Services;

internal sealed class CommandAuthorizationService
{
    private readonly AppConfig config;

    public CommandAuthorizationService(AppConfig config)
    {
        this.config = config;
    }

    public bool CanExecute(long chatId, string role, string action, string detail, out string reason)
    {
        reason = "";
        if (!config.RbacEnabled)
            return true;

        role = NormalizeRole(role);
        if (chatId == config.LocalChatId || role is "dueno" or "admin" or "administrador")
            return true;

        bool sensitive = IsSensitive(action, detail);
        if (!sensitive)
            return true;

        if (role is "tecnico" or "técnico")
        {
            bool allowedTech = action.Contains("memory", StringComparison.OrdinalIgnoreCase)
                || action.Contains("phase", StringComparison.OrdinalIgnoreCase)
                || action.Contains("engine", StringComparison.OrdinalIgnoreCase)
                || detail.Contains("estado", StringComparison.OrdinalIgnoreCase)
                || detail.Contains("diagnost", StringComparison.OrdinalIgnoreCase);
            if (allowedTech)
                return true;
        }

        reason = "RBAC: tu rol no tiene permiso para ejecutar esta acción sensible. Requiere Admin/Dueño.";
        return false;
    }

    private static bool IsSensitive(string action, string detail)
    {
        string text = (action + " " + detail).ToLowerInvariant();
        return text.Contains("shutdown")
            || text.Contains("apagar hanna")
            || text.Contains("cerrar hanna")
            || text.Contains("delete")
            || text.Contains("borrar")
            || text.Contains("eliminar")
            || text.Contains("archivo")
            || text.Contains("file")
            || text.Contains("rclone")
            || text.Contains("backup")
            || text.Contains("openrouter")
            || text.Contains("phase")
            || text.Contains("fase")
            || text.Contains("engine")
            || text.Contains("motor");
    }

    private static string NormalizeRole(string role)
    {
        role = (role ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(role)) return "usuario";
        return role;
    }
}
