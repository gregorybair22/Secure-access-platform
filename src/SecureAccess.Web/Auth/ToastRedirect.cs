namespace SecureAccess.Web.Auth;

/// <summary>
/// Builds redirect URLs with toast query parameters read by wwwroot/js/toast-flash.js.
/// </summary>
public static class ToastRedirect
{
    public const string TypeQuery = "toast";
    public const string MessageQuery = "toastMsg";

    public static string WithToast(string path, string type, string message)
    {
        var separator = path.Contains('?') ? '&' : '?';
        return $"{path}{separator}{TypeQuery}={Uri.EscapeDataString(type)}&{MessageQuery}={Uri.EscapeDataString(message)}";
    }
}
