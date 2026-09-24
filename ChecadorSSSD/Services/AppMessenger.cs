using System;

namespace ChecadorSSSD.Services;

public static class AppMessenger
{
    public static event EventHandler? ImagesChanged;
    public static event EventHandler? PersonasChanged;

    public static void NotifyImagesChanged() => ImagesChanged?.Invoke(null, EventArgs.Empty);
    public static void NotifyPersonasChanged() => PersonasChanged?.Invoke(null, EventArgs.Empty);
}
