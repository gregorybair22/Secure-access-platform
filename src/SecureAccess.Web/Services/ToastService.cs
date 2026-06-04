using Microsoft.JSInterop;

namespace SecureAccess.Web.Services;

public sealed class ToastService : IToastService
{
    private readonly IJSRuntime _js;

    public ToastService(IJSRuntime js) => _js = js;

    public Task SuccessAsync(string message) => ShowAsync("success", message);

    public Task ErrorAsync(string message) => ShowAsync("error", message);

    public Task InfoAsync(string message) => ShowAsync("info", message);

    public Task WarningAsync(string message) => ShowAsync("warning", message);

    public async Task ShowAsync(string type, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        try
        {
            await _js.InvokeVoidAsync("saToast.show", type, message);
        }
        catch (JSException)
        {
            // Toastr not loaded (e.g. static render without scripts).
        }
        catch (InvalidOperationException)
        {
            // Circuit not ready for interop.
        }
        catch (JSDisconnectedException)
        {
            // Client disconnected.
        }
    }
}
