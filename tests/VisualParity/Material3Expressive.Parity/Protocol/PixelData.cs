using SkiaSharp;

namespace Material3Expressive.Parity;

public static class PixelData
{
    public static (int Width, int Height, byte[] Bytes) DecodePng(string path)
    {
        using var stream = File.OpenRead(path);
        using var codec = SKCodec.Create(stream) ?? throw new InvalidDataException("Invalid PNG.");
        if (codec.EncodedFormat != SKEncodedImageFormat.Png) throw new InvalidDataException("Raster artifact must be PNG.");
        using var colorSpace = SKColorSpace.CreateSrgb();
        using var bitmap = new SKBitmap(new SKImageInfo(codec.Info.Width, codec.Info.Height,
            SKColorType.Rgba8888, SKAlphaType.Unpremul, colorSpace));
        if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success)
            throw new InvalidDataException("PNG decoding failed.");
        var bytes = new byte[checked(bitmap.Width * bitmap.Height * 4)];
        System.Runtime.InteropServices.Marshal.Copy(bitmap.GetPixels(), bytes, 0, bytes.Length);
        return (bitmap.Width, bitmap.Height, bytes);
    }

    public static RasterObservation FromPng(string directory, string id, string image)
    {
        var decoded = DecodePng(Path.Combine(directory, image));
        var pixels = Path.ChangeExtension(image, ".rgba").Replace('\\', '/');
        File.WriteAllBytes(Path.Combine(directory, pixels), decoded.Bytes);
        return new(id, image, pixels, decoded.Width, decoded.Height);
    }

    public static byte[] Read(string directory, RasterObservation raster)
    {
        var path = Path.GetFullPath(Path.Combine(directory, raster.Pixels));
        if (!path.StartsWith(Path.GetFullPath(directory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Pixel path outside bundle.");
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length != checked(raster.Width * raster.Height * 4)) throw new InvalidDataException("Pixel buffer size mismatch.");
        return bytes;
    }
}