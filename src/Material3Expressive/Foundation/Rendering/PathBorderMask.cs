// Border.kt drawGenericBorder: offscreen doubled stroke and scaled inverse-path clear.
// Copyright The Android Open Source Project; Apache-2.0. Fixed baseline in docs/upstream-baseline.json.
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;

namespace Material3Expressive.Foundation.Rendering;

internal sealed class PathBorderMask : IDisposable
{
    private SKBitmap? _bitmap;
    internal WriteableBitmap Update(Size size, Point origin, CornerRadius corners, float stroke, float density, bool fill, Size layoutSize, Color color)
    {
        var width = (float)size.Width * density;
        var height = (float)size.Height * density;
        var left = (float)origin.X * density;
        var top = (float)origin.Y * density;
        using var rounded = new SKRoundRect();
        rounded.SetRectRadii(new SKRect(left, top, left + width, top + height),
            [new((float)corners.TopLeft * density, (float)corners.TopLeft * density),
             new((float)corners.TopRight * density, (float)corners.TopRight * density),
             new((float)corners.BottomRight * density, (float)corners.BottomRight * density),
             new((float)corners.BottomLeft * density, (float)corners.BottomLeft * density)]);
        using var builder = new SKPathBuilder();
        builder.AddRoundRect(rounded, SKPathDirection.Clockwise);
        using var path = builder.Detach();
        return Update(path, stroke, density, fill, layoutSize, color);
    }

    // The caller owns the physical-pixel path. Focus borders additionally erase the inset stroke.
    internal WriteableBitmap Update(SKPath path, float stroke, float density, bool fill, Size layoutSize, Color color, float inset = 0)
    {
        var bounds = path.Bounds;
        var width = bounds.Width;
        var height = bounds.Height;
        var left = bounds.Left;
        var top = bounds.Top;
        var pixels = new PixelSize((int)MathF.Ceiling(width), (int)MathF.Ceiling(height));
        // BorderCache checks the original layout size, but allocates the path bounds.
        // On shrink it keeps the larger buffer. Its pixels outside the current source rectangle
        // participate in filtering at fractional origins; a fresh cropped bitmap is not equivalent.
        if (_bitmap is null || layoutSize.Width * density > _bitmap.Width || layoutSize.Height * density > _bitmap.Height)
        {
            _bitmap?.Dispose();
            _bitmap = new SKBitmap(new SKImageInfo(pixels.Width, pixels.Height, SKColorType.Alpha8, SKAlphaType.Premul));
            _bitmap.Erase(SKColors.Transparent);
        }
        var bitmap = _bitmap;
        using var canvas = new SKCanvas(bitmap);
        using (var clear = new SKPaint { BlendMode = SKBlendMode.Clear })
            canvas.DrawRect(0, 0, pixels.Width, pixels.Height, clear);
        canvas.Translate(-left, -top);
        using var paint = new SKPaint { IsAntialias = true, Color = SKColors.White,
            Style = fill ? SKPaintStyle.Fill : SKPaintStyle.Stroke, StrokeWidth = (stroke + inset) * 2 };
        canvas.DrawPath(path, paint);
        if (!fill)
        {
            if (inset > 0)
            {
                paint.StrokeWidth = inset * 2; paint.BlendMode = SKBlendMode.Clear;
                canvas.DrawPath(path, paint);
            }
            using var rectBuilder = new SKPathBuilder();
            rectBuilder.AddRect(new SKRect(left, top, left + width, top + height));
            using var rectangle = rectBuilder.Detach();
            using var inverse = rectangle.Op(path, SKPathOp.Difference)
                ?? throw new InvalidOperationException("Unable to create the inverse border path.");
            // BorderCache draws with the requested integer bitmap size; scale around its center.
            canvas.Scale((pixels.Width + 1f) / pixels.Width, (pixels.Height + 1f) / pixels.Height,
                pixels.Width / 2f, pixels.Height / 2f);
            paint.Style = SKPaintStyle.Fill; paint.BlendMode = SKBlendMode.Clear;
            canvas.DrawPath(inverse, paint);
        }
        canvas.Flush();
        var result = new WriteableBitmap(new PixelSize(bitmap.Width, bitmap.Height), new Vector(96 * density, 96 * density), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using var target = result.Lock();
        var alpha = new byte[bitmap.RowBytes];
        var row = new byte[bitmap.Width * 4];
        for (var y = 0; y < bitmap.Height; y++)
        {
            Marshal.Copy(bitmap.GetPixels() + y * bitmap.RowBytes, alpha, 0, alpha.Length);
            for (var x = 0; x < bitmap.Width; x++)
            {
                var a = (byte)((alpha[x] * color.A + 127) / 255);
                row[x * 4] = (byte)((color.B * a + 127) / 255);
                row[x * 4 + 1] = (byte)((color.G * a + 127) / 255);
                row[x * 4 + 2] = (byte)((color.R * a + 127) / 255);
                row[x * 4 + 3] = a;
            }
            Marshal.Copy(row, 0, target.Address + y * target.RowBytes, row.Length);
        }
        return result;
    }
    public void Dispose() { _bitmap?.Dispose(); _bitmap = null; }
}
