namespace SecureAccess.Web;

/// <summary>Shared timing for modal close animations (see wwwroot/css/modal.css).</summary>
public static class ModalAnimation
{
    public const int CloseMs = 220;

    public static async Task CloseAsync(
        Func<bool> isClosing,
        Action<bool> setClosing,
        Action onClosed,
        Action render)
    {
        if (isClosing()) return;
        setClosing(true);
        render();
        await Task.Delay(CloseMs);
        onClosed();
        setClosing(false);
    }
}
