using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace AlbionDpsMeter.Services;

public class ImageController
{
    private static readonly string ImageCacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AlbionDpsMeter", "ImageCache");

    private const string RenderBaseUrl = "https://render.albiononline.com/v1/item/";

    private static readonly ConcurrentDictionary<string, WeakReference<BitmapImage>> _cache = new();
    private static readonly ConcurrentDictionary<string, bool> _downloading = new();
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly DispatcherQueue _dispatcherQueue;

    public ImageController(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;
    }

    public async Task<BitmapImage?> GetItemImageAsync(string? uniqueName)
    {
        if (string.IsNullOrEmpty(uniqueName))
            return null;

        // Check memory cache
        if (_cache.TryGetValue(uniqueName, out var weakRef) && weakRef.TryGetTarget(out var cached))
            return cached;

        // Check local file cache
        Directory.CreateDirectory(ImageCacheDir);
        var localPath = Path.Combine(ImageCacheDir, $"{SanitizeFileName(uniqueName)}.png");

        if (File.Exists(localPath))
        {
            try
            {
                var image = await LoadImageFromFileAsync(localPath);
                if (image != null)
                {
                    _cache[uniqueName] = new WeakReference<BitmapImage>(image);
                    return image;
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed to load cached image for {Name}", uniqueName);
            }
        }

        // Download from render API
        if (!_downloading.TryAdd(uniqueName, true))
            return null; // Already downloading

        try
        {
            var url = $"{RenderBaseUrl}{Uri.EscapeDataString(uniqueName)}?size=64&quality=1";
            Log.Debug("Downloading item image: {Url}", url);
            var bytes = await _http.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(localPath, bytes);

            var image = await LoadImageFromBytesAsync(bytes);
            if (image != null)
            {
                _cache[uniqueName] = new WeakReference<BitmapImage>(image);
                return image;
            }
            Log.Warning("Failed to create BitmapImage from downloaded bytes for {Name}", uniqueName);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to download image for {Name}", uniqueName);
        }
        finally
        {
            _downloading.TryRemove(uniqueName, out _);
        }

        return null;
    }

    private async Task<BitmapImage?> LoadImageFromBytesAsync(byte[] bytes)
    {
        var tcs = new TaskCompletionSource<BitmapImage?>();

        if (!_dispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                var image = new BitmapImage();
                using var ms = new InMemoryRandomAccessStream();
                using var writer = new DataWriter(ms.GetOutputStreamAt(0));
                writer.WriteBytes(bytes);
                await writer.StoreAsync();
                ms.Seek(0);
                await image.SetSourceAsync(ms);
                tcs.TrySetResult(image);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed to create BitmapImage from bytes");
                tcs.TrySetResult(null);
            }
        }))
        {
            Log.Warning("Failed to enqueue BitmapImage creation on dispatcher");
            return null;
        }

        return await tcs.Task;
    }

    private async Task<BitmapImage?> LoadImageFromFileAsync(string path)
    {
        try
        {
            var bytes = await File.ReadAllBytesAsync(path);
            return await LoadImageFromBytesAsync(bytes);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to load image from {Path}", path);
            return null;
        }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
