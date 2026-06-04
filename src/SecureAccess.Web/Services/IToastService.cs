namespace SecureAccess.Web.Services;

/// <summary>Shows toastr notifications for user-facing events (success, errors, info).</summary>
public interface IToastService
{
    Task SuccessAsync(string message);
    Task ErrorAsync(string message);
    Task InfoAsync(string message);
    Task WarningAsync(string message);
    Task ShowAsync(string type, string message);
}
