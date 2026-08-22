using System.Drawing;
using System.Drawing.Imaging;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

namespace BetterScreenshots.Ocr;

public sealed record OcrResult(string Text, bool Available, string? Error = null);

/// <summary>Uses Windows' installed OCR language packs. No screen data is sent anywhere.</summary>
public sealed class OcrService
{
    public async Task<OcrResult> ReadAsync(Bitmap bitmap, CancellationToken cancellationToken = default)
    {
        try
        {
            var engine = OcrEngine.TryCreateFromUserProfileLanguages();
            if (engine is null) return new OcrResult("", false, "Windows OCR is unavailable. Install an OCR language pack in Windows Settings.");
            using var png = new MemoryStream();
            bitmap.Save(png, ImageFormat.Png);
            using var stream = new InMemoryRandomAccessStream();
            stream.WriteAsync(CryptographicBuffer.CreateFromByteArray(png.ToArray())).GetAwaiter().GetResult();
            stream.Seek(0);
            var decoder = await BitmapDecoder.CreateAsync(stream);
            using var softwareBitmap = await decoder.GetSoftwareBitmapAsync();
            cancellationToken.ThrowIfCancellationRequested();
            var result = await engine.RecognizeAsync(softwareBitmap);
            return new OcrResult(result.Text?.Trim() ?? "", true);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return new OcrResult("", false, ex.Message); }
    }
}
