using System.Runtime.InteropServices;
using SkiaSharp;

namespace Material3Expressive.VisualTesting.Model;

/// <summary>A tightly packed premultiplied RGBA image used by the visual comparison engine.</summary>
public sealed record RenderedImage(int Width, int Height, byte[] Pixels)
{
    public int PixelCount => Width * Height;

    public static RenderedImage DecodePng(ReadOnlySpan<byte> png)
    {
        using var source = SKBitmap.Decode(png.ToArray())
            ?? throw new InvalidDataException("The PNG could not be decoded.");
        using var converted = new SKBitmap(
            new SKImageInfo(source.Width, source.Height, SKColorType.Rgba8888, SKAlphaType.Premul));

        using (var canvas = new SKCanvas(converted))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(source, 0, 0);
            canvas.Flush();
        }

        var pixels = new byte[converted.Width * converted.Height * 4];
        var sourceAddress = converted.GetPixels();
        for (var y = 0; y < converted.Height; y++)
        {
            Marshal.Copy(
                sourceAddress + (y * converted.RowBytes),
                pixels,
                y * converted.Width * 4,
                converted.Width * 4);
        }

        return new RenderedImage(converted.Width, converted.Height, pixels);
    }

    public byte[] EncodePng()
    {
        var info = new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(info);
        var destination = bitmap.GetPixels();

        for (var y = 0; y < Height; y++)
        {
            Marshal.Copy(
                Pixels,
                y * Width * 4,
                destination + (y * bitmap.RowBytes),
                Width * 4);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return encoded.ToArray();
    }
}
