using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace BetterScreenshots.Editor;

internal static class ImageOperations
{
    public static void Redact(Bitmap bitmap, Rectangle rectangle, Color color)
    {
        using var g = Graphics.FromImage(bitmap); using var brush = new SolidBrush(color); g.FillRectangle(brush, Clip(bitmap, rectangle));
    }
    public static void Pixelate(Bitmap bitmap, Rectangle rectangle, int blockSize = 10)
    {
        var clip = Clip(bitmap, rectangle); if (clip.Width < 1 || clip.Height < 1) return;
        using var source = bitmap.Clone(clip, PixelFormat.Format32bppPArgb);
        using var g = Graphics.FromImage(bitmap);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
        var smallWidth = Math.Max(1, clip.Width / blockSize); var smallHeight = Math.Max(1, clip.Height / blockSize);
        using var small = new Bitmap(smallWidth, smallHeight);
        using (var sg = Graphics.FromImage(small)) sg.DrawImage(source, new Rectangle(0, 0, smallWidth, smallHeight));
        g.DrawImage(small, clip, new Rectangle(0, 0, smallWidth, smallHeight), GraphicsUnit.Pixel);
    }
    public static void Blur(Bitmap bitmap, Rectangle rectangle, int radius = 8)
    {
        var clip = Clip(bitmap, rectangle); if (clip.Width < 1 || clip.Height < 1) return;
        using var source = bitmap.Clone(clip, PixelFormat.Format32bppPArgb);
        var data = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
        var input = new byte[Math.Abs(data.Stride) * source.Height]; Marshal.Copy(data.Scan0, input, 0, input.Length); source.UnlockBits(data);
        using var result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppPArgb);
        var outData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
        var output = new byte[input.Length];
        for (var y = 0; y < source.Height; y++) for (var x = 0; x < source.Width; x++)
        {
            var b = 0; var g = 0; var r = 0; var a = 0; var count = 0;
            for (var yy = Math.Max(0, y - radius); yy <= Math.Min(source.Height - 1, y + radius); yy++) for (var xx = Math.Max(0, x - radius); xx <= Math.Min(source.Width - 1, x + radius); xx++)
            { var i = yy * data.Stride + xx * 4; b += input[i]; g += input[i + 1]; r += input[i + 2]; a += input[i + 3]; count++; }
            var index = y * outData.Stride + x * 4; output[index] = (byte)(b / count); output[index + 1] = (byte)(g / count); output[index + 2] = (byte)(r / count); output[index + 3] = (byte)(a / count);
        }
        Marshal.Copy(output, 0, outData.Scan0, output.Length); result.UnlockBits(outData);
        using var graphics = Graphics.FromImage(bitmap); graphics.DrawImageUnscaled(result, clip.Location);
    }
    private static Rectangle Clip(Bitmap bitmap, Rectangle rect) => Rectangle.Intersect(new Rectangle(0, 0, bitmap.Width, bitmap.Height), rect);
}
