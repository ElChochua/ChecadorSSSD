using System;

namespace ChecadorSSSD.Services;

public static class AppMessenger
{
    public static event EventHandler? ImagesChanged;

    public static void NotifyImagesChanged()
    {
        ImagesChanged?.Invoke(null, EventArgs.Empty);
    }
}
