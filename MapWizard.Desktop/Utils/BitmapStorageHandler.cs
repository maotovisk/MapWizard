using System;
using System.IO;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace MapWizard.Desktop.Utils;

/// <summary>
/// Identifies one page (slot) of a <see cref="BitmapStorageHandler"/>.
/// </summary>
internal readonly record struct BitmapHandler(int Index);

/// <summary>
/// Fixed-size, round-robin cache of decoded and resized thumbnails. The pixels live in unmanaged
/// pages (<see cref="PageAllocator"/>) so the cache itself does not occupy GC heap space.
/// </summary>
public sealed class BitmapStorageHandler : IDisposable
{
    /// <summary>Width of a cached thumbnail, in pixels.</summary>
    public const int BitmapWidth = 512;

    /// <summary>Height of a cached thumbnail, in pixels.</summary>
    public const int BitmapHeight = 256;

    /// <summary>Bytes per row of a page: a 512 pixel row of 4 byte pixels.</summary>
    public const int BitmapStride = BitmapWidth * 4;

    /// <summary>Size of a single page, in bytes.</summary>
    public const int PageBytes = BitmapStride * BitmapHeight;

    /// <summary>Number of pages, i.e. of cached thumbnails.</summary>
    public const int StorageSize = 20;

    private static readonly PixelSize TargetSize = new(BitmapWidth, BitmapHeight);
    private static readonly Vector TargetDpi = new(96, 96);

    private static readonly Lazy<BitmapStorageHandler> SharedInstance = new(static () =>
    {
        var handler = new BitmapStorageHandler();
        handler.Init();
        return handler;
    });

    /// <summary>
    /// Process-wide cache, used by the UI (the map picker loads every visible card background through it).
    /// Its pages are committed on first use and handed back to the OS at exit.
    /// </summary>
    public static BitmapStorageHandler Shared => SharedInstance.Value;

    // Thumbnails are a 2:1 letterbox, so a source that is not 2:1 is center-cropped (cover) rather than
    // stretched. MediumQuality keeps the resize cheap; the decode itself is already scaled by the codec.
    private const BitmapInterpolationMode Interpolation = BitmapInterpolationMode.MediumQuality;

    private readonly object gate = new();
    private readonly IntPtr[] pages = new IntPtr[StorageSize];
    private readonly BitmapStorageInfo[] infos = new BitmapStorageInfo[StorageSize];
    private int position;
    private bool initialized;

    /// <summary>
    /// Allocates all pages. Must be called before <see cref="Load"/>, e.g. during application startup.
    /// </summary>
    public void Init()
    {
        lock (gate)
        {
            if (initialized)
                return;

            for (var i = 0; i < pages.Length; i++)
                pages[i] = PageAllocator.Alloc(PageBytes);

            initialized = true;
        }
    }

    /// <summary>
    /// Returns the 512x256 thumbnail of <paramref name="path"/>, decoding it into the next free page on a
    /// miss and replaying the page on a hit. The returned bitmap is owned by the caller and stays valid
    /// after its page is recycled, because <see cref="Bitmap"/> copies the pixels out of the page.
    /// </summary>
    public Bitmap Load(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        lock (gate)
        {
            if (!initialized)
                throw new InvalidOperationException($"{nameof(Init)} must be called before {nameof(Load)}.");

            for (var i = 0; i < infos.Length; i++)
            {
                if (infos[i].Path == path)
                    return CreateBitmap(new BitmapHandler(i));
            }

            var handle = new BitmapHandler(position);
            position = position + 1 == StorageSize ? 0 : position + 1;

            using (var decoded = DecodeCoveringPage(path))
            {
                var format = decoded.Format ?? throw new NotSupportedException($"Unable to read pixels of '{path}'.");
                var alphaFormat = decoded.AlphaFormat ?? throw new NotSupportedException($"Unable to read pixels of '{path}'.");

                // Center-crop the decoded image down to the 2:1 page and blit it straight into the page.
                var crop = new PixelRect(
                    (decoded.PixelSize.Width - BitmapWidth) / 2,
                    (decoded.PixelSize.Height - BitmapHeight) / 2,
                    BitmapWidth,
                    BitmapHeight);
                decoded.CopyPixels(crop, pages[handle.Index], PageBytes, BitmapStride);

                infos[handle.Index] = new BitmapStorageInfo(path, format, alphaFormat);
            }

            return CreateBitmap(handle);
        }
    }

    /// <summary>
    /// Decodes <paramref name="path"/> at the thumbnail size. The codec scales during the decode, so the
    /// full resolution image is never materialized. Aspect ratio is preserved and the result is guaranteed
    /// to be at least as large as a page, either exactly <see cref="BitmapWidth"/> wide or exactly
    /// <see cref="BitmapHeight"/> tall.
    /// </summary>
    private static Bitmap DecodeCoveringPage(string path)
    {
        using var stream = File.OpenRead(path);

        var decoded = Bitmap.DecodeToWidth(stream, BitmapWidth, Interpolation);
        if (decoded.PixelSize.Height >= BitmapHeight)
            return decoded;

        // Wider than 2:1 (a panorama), where the page width is the limiting side: decode by height instead.
        decoded.Dispose();
        stream.Position = 0;
        return Bitmap.DecodeToHeight(stream, BitmapHeight, Interpolation);
    }

    /// <summary>
    /// Materializes an Avalonia bitmap over a page. Avalonia has no public zero-copy bitmap that renders
    /// from caller-owned memory, so this is one memcpy of the page.
    /// </summary>
    private Bitmap CreateBitmap(BitmapHandler handle)
    {
        var info = infos[handle.Index];
        return new Bitmap(info.Format, info.AlphaFormat, pages[handle.Index], TargetSize, TargetDpi, BitmapStride);
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (!initialized)
                return;

            for (var i = 0; i < pages.Length; i++)
            {
                PageAllocator.Free(pages[i], PageBytes);
                pages[i] = IntPtr.Zero;
            }

            Array.Clear(infos);
            position = 0;
            initialized = false;
        }
    }

    /// <summary>State of one page: which file it holds and how its pixels are encoded.</summary>
    private readonly struct BitmapStorageInfo(string path, PixelFormat format, AlphaFormat alphaFormat)
    {
        /// <summary>Source path, or null when the page has never been filled.</summary>
        public string? Path { get; } = path;

        public PixelFormat Format { get; } = format;

        public AlphaFormat AlphaFormat { get; } = alphaFormat;
    }
}
