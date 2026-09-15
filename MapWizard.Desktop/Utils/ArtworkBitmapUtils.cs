using System.IO;
using Avalonia.Media.Imaging;

namespace MapWizard.Desktop.Utils;

internal static class ArtworkBitmapUtils
{
    public static Bitmap DecodePreview(string path, int width)
    {
        using var stream = File.OpenRead(path);
        return Bitmap.DecodeToWidth(stream, width);
    }
}
