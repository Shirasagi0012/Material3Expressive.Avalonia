using SkiaSharp;

namespace Material3Expressive.Parity;

public static class PixelData
{
    // Used at the bundle boundary when the comparison will decode the PNG once.
    public static void Validate(string directory, RasterObservation raster)
    {
        var path = Path.GetFullPath(Path.Combine(directory, raster.Image));
        if (!path.StartsWith(Path.GetFullPath(directory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Pixel path outside bundle.");
        using var stream = File.OpenRead(path);
        using var codec = SKCodec.Create(stream) ?? throw new InvalidDataException("Invalid PNG.");
        if (codec.EncodedFormat != SKEncodedImageFormat.Png || codec.Info.Width != raster.Width || codec.Info.Height != raster.Height)
            throw new InvalidDataException("PNG format or dimensions differ from observations.");
    }

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
        var path = Path.Combine(directory, image);
        var decoded = DecodePng(path);
        // Persist the comparison pixels themselves: sRGB, 8 bit, straight alpha.
        // A raw PNG decoder in the browser must not need ICC/gamma conversion.
        using var colorSpace = SKColorSpace.CreateSrgb();
        using var bitmap = new SKBitmap(new SKImageInfo(decoded.Width, decoded.Height,
            SKColorType.Rgba8888, SKAlphaType.Unpremul, colorSpace));
        System.Runtime.InteropServices.Marshal.Copy(decoded.Bytes, 0, bitmap.GetPixels(), decoded.Bytes.Length);
        using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, png.ToArray());
        return new(id, image, decoded.Width, decoded.Height);
    }

    public static byte[] Read(string directory, RasterObservation raster)
    {
        var path = Path.GetFullPath(Path.Combine(directory, raster.Image));
        if (!path.StartsWith(Path.GetFullPath(directory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Pixel path outside bundle.");
        var decoded = DecodePng(path);
        if (decoded.Width != raster.Width || decoded.Height != raster.Height)
            throw new InvalidDataException("PNG dimensions differ from observations.");
        return decoded.Bytes;
    }
}
