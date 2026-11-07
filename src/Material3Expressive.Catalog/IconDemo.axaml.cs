using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ComposeAnimation;

namespace Material3Expressive.Catalog;

public partial class IconDemo : Window
{
    private readonly WriteableBitmap _bitmap;
    public IconDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
        _bitmap = new WriteableBitmap(new PixelSize(24, 24), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        using (var pixels = _bitmap.Lock())
        {
            var data = new byte[pixels.RowBytes * 24];
            for (var y = 0; y < 24; y++) for (var x = 0; x < 24; x++)
                if (x is >= 4 and < 20 && y is >= 4 and < 20 && (x < 10 || y < 10))
                {
                    var index = y * pixels.RowBytes + x * 4;
                    data[index] = 255; data[index + 3] = 255;
                }
            Marshal.Copy(data, 0, pixels.Address, data.Length);
        }
        BitmapIcon.Source = _bitmap;
        Closed += (_, _) => _bitmap.Dispose();
    }
    private void OnDirection(object? sender, RoutedEventArgs e) => DirectionHost.FlowDirection =
        DirectionHost.FlowDirection == FlowDirection.LeftToRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
}
