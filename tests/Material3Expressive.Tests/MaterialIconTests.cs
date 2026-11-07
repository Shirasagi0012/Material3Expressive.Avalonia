using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Components;
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public class MaterialIconTests
{
    [AvaloniaTheory]
    [InlineData(32, 16, 100, 100, 32, 16)]
    [InlineData(32, 16, 20, 100, 20, 10)]
    [InlineData(32, 16, 100, 8, 16, 8)]
    [InlineData(double.NaN, double.NaN, 100, 100, 24, 24)]
    [InlineData(double.PositiveInfinity, double.PositiveInfinity, 18, 30, 18, 24)]
    [InlineData(0, 0, 100, 100, 0, 0)]
    public void IntrinsicSizing_UsesFitAndFallback(double width, double height, double maxWidth, double maxHeight, double expectedWidth, double expectedHeight)
    {
        var icon = new MaterialIcon { Source = new TestImage(new Size(width, height)) };
        var root = new Panel { Width = maxWidth, Height = maxHeight, Children = { icon } };
        var window = DialogTests.Show(root);
        try { Assert.Equal(new Size(expectedWidth, expectedHeight), icon.Bounds.Size); }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ExplicitDimensions_InheritedTint_AndMutableDrawingRemainLive()
    {
        var image = new DrawingImage(new GeometryDrawing { Brush = Brushes.Blue, Geometry = new RectangleGeometry(new Rect(0, 0, 32, 16)) });
        var icon = new MaterialIcon { Source = image };
        var root = new StackPanel { Children = { icon } };
        TextElement.SetForeground(root, Brushes.Red);
        var window = DialogTests.Show(root);
        try
        {
            Assert.Equal(Brushes.Red, icon.Foreground);
            Assert.Equal(new Size(32, 16), icon.Bounds.Size);
            image.Drawing = new GeometryDrawing { Brush = Brushes.Green, Geometry = new RectangleGeometry(new Rect(0, 0, 40, 20)) };
            window.UpdateLayout(); Assert.Equal(new Size(40, 20), icon.Bounds.Size);
            icon.Width = 80; window.UpdateLayout(); Assert.Equal(new Size(80, 20), icon.Bounds.Size);
            icon.MinHeight = 60; window.UpdateLayout(); Assert.Equal(new Size(80, 60), icon.Bounds.Size);
            icon.Foreground = Brushes.Gold; TextElement.SetForeground(root, Brushes.Blue);
            Assert.Equal(Brushes.Gold, icon.Foreground);
            icon.ClearValue(MaterialIcon.ForegroundProperty); Assert.Equal(Brushes.Blue, icon.Foreground);
            root.Children.Clear();
            image.Drawing = new GeometryDrawing { Geometry = new RectangleGeometry(new Rect(0, 0, 20, 10)) };
            icon.ClearValue(Control.WidthProperty); icon.ClearValue(Control.MinHeightProperty);
            root.Children.Add(icon); window.UpdateLayout(); Assert.Equal(new Size(20, 10), icon.Bounds.Size);
            icon.Source = null; window.UpdateLayout(); Assert.Equal(new Size(24, 24), icon.Bounds.Size);
            window.Resources[MaterialTokens.Comp.IconButtonSmall.IconSize] = 28d; window.UpdateLayout();
            Assert.Equal(new Size(28, 28), icon.Bounds.Size);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Description_ControlsImageSemanticsWithoutMakingItInteractive()
    {
        var icon = new MaterialIcon();
        var peer = ControlAutomationPeer.CreatePeerForElement(icon)!;
        Assert.Equal(AutomationControlType.Image, peer.GetAutomationControlType());
        Assert.False(peer.IsControlElement()); Assert.False(peer.IsContentElement());
        icon.ContentDescription = "Download";
        Assert.Equal("Download", peer.GetName()); Assert.True(peer.IsContentElement());
        AutomationProperties.SetName(icon, "Save file"); Assert.Equal("Save file", peer.GetName());
        Assert.False(icon.Focusable);
        Assert.Null(peer.GetProvider<Avalonia.Automation.Provider.IInvokeProvider>());
    }

    private sealed class TestImage(Size size) : IImage
    {
        public Size Size => size;
        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect) => context.FillRectangle(Brushes.Red, destRect);
    }
}
