// Icon.kt and ui/draw/PainterModifier.kt at AndroidX f3ed195f0c9e / Core a9e47e8aa04c.
// Copyright 2021 The Android Open Source Project; Apache-2.0.
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

/// <summary>A bitmap or vector icon, fitted to its bounds and tinted with the inherited content color.</summary>
public class MaterialIcon : Control
{
    public static readonly StyledProperty<IImage?> SourceProperty = AvaloniaProperty.Register<MaterialIcon, IImage?>(nameof(Source));
    public static readonly StyledProperty<IBrush?> ForegroundProperty = TextElement.ForegroundProperty.AddOwner<MaterialIcon>();
    public static readonly StyledProperty<bool> IsTintEnabledProperty = AvaloniaProperty.Register<MaterialIcon, bool>(nameof(IsTintEnabled), true);
    public static readonly StyledProperty<bool> AutoMirrorProperty = AvaloniaProperty.Register<MaterialIcon, bool>(nameof(AutoMirror));
    public static readonly StyledProperty<string?> ContentDescriptionProperty = AvaloniaProperty.Register<MaterialIcon, string?>(nameof(ContentDescription));
    internal static readonly StyledProperty<double> DefaultSizeProperty = AvaloniaProperty.Register<MaterialIcon, double>(nameof(DefaultSize));

    [Content]
    public IImage? Source { get => GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    public IBrush? Foreground { get => GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }
    /// <summary>False preserves the source colors, corresponding to an unspecified upstream tint.</summary>
    public bool IsTintEnabled { get => GetValue(IsTintEnabledProperty); set => SetValue(IsTintEnabledProperty, value); }
    /// <summary>Mirrors directional artwork in RTL. Ordinary bitmap and vector icons are not mirrored.</summary>
    public bool AutoMirror { get => GetValue(AutoMirrorProperty); set => SetValue(AutoMirrorProperty, value); }
    /// <summary>Localized accessible image description. Null marks a decorative icon.</summary>
    public string? ContentDescription { get => GetValue(ContentDescriptionProperty); set => SetValue(ContentDescriptionProperty, value); }
    internal double DefaultSize { get => GetValue(DefaultSizeProperty); set => SetValue(DefaultSizeProperty, value); }

    private DrawingImage? _observedSource;
    static MaterialIcon()
    {
        AffectsMeasure<MaterialIcon>(SourceProperty, DefaultSizeProperty);
        AffectsRender<MaterialIcon>(SourceProperty, ForegroundProperty, IsTintEnabledProperty, AutoMirrorProperty, FlowDirectionProperty);
    }
    protected override bool BypassFlowDirectionPolicies => true;

    protected override Size MeasureOverride(Size availableSize)
    {
        var intrinsic = Source?.Size ?? new Size(double.NaN, double.NaN);
        var noIntrinsic = double.IsNaN(intrinsic.Width) && double.IsNaN(intrinsic.Height);
        var defaultSize = noIntrinsic || double.IsInfinity(intrinsic.Width) && double.IsInfinity(intrinsic.Height);
        if (defaultSize)
            return new Size(Math.Min(DefaultSize, availableSize.Width), Math.Min(DefaultSize, availableSize.Height));

        var density = (float)LayoutHelper.GetLayoutScale(this);
        float Px(double value) => (float)value * density;
        float Min(double explicitSize, double minimum) => double.IsNaN(explicitSize) ? Px(minimum) : Px(explicitSize);
        var minWidth = Math.Min(Px(availableSize.Width), Min(Width, MinWidth));
        var minHeight = Math.Min(Px(availableSize.Height), Min(Height, MinHeight));
        float Constrain(float value, float minimum, double maximum) => Math.Clamp(value, minimum, Px(maximum));
        var width = Constrain(double.IsFinite(intrinsic.Width) ? MathUtils.RoundToInt(Px(intrinsic.Width)) : minWidth, minWidth, availableSize.Width);
        var height = Constrain(double.IsFinite(intrinsic.Height) ? MathUtils.RoundToInt(Px(intrinsic.Height)) : minHeight, minHeight, availableSize.Height);
        var sourceWidth = double.IsFinite(intrinsic.Width) ? Px(intrinsic.Width) : width;
        var sourceHeight = double.IsFinite(intrinsic.Height) ? Px(intrinsic.Height) : height;
        var scale = width == 0 || height == 0 ? 0 : Math.Min(width / sourceWidth, height / sourceHeight);
        var scaledWidth = scale == 0 ? 0 : sourceWidth * scale;
        var scaledHeight = scale == 0 ? 0 : sourceHeight * scale;
        return new Size(Constrain(MathUtils.RoundToInt(scaledWidth), minWidth, availableSize.Width) / density,
            Constrain(MathUtils.RoundToInt(scaledHeight), minHeight, availableSize.Height) / density);
    }

    public override void Render(DrawingContext context)
    {
        if (Source is not { } source || Bounds.Width <= 0 || Bounds.Height <= 0) return;
        var density = (float)LayoutHelper.GetLayoutScale(this);
        var intrinsic = source.Size;
        var width = (float)Bounds.Width * density;
        var height = (float)Bounds.Height * density;
        var sourceWidth = double.IsFinite(intrinsic.Width) ? (float)intrinsic.Width * density : width;
        var sourceHeight = double.IsFinite(intrinsic.Height) ? (float)intrinsic.Height * density : height;
        if (sourceWidth <= 0 || sourceHeight <= 0) return;
        var scale = Math.Min(width / sourceWidth, height / sourceHeight);
        var scaledWidth = sourceWidth * scale;
        var scaledHeight = sourceHeight * scale;
        // PainterModifier aligns rounded physical-pixel sizes, but draws the unrounded size.
        var x = MathUtils.RoundToInt((MathUtils.RoundToInt(width) - MathUtils.RoundToInt(scaledWidth)) / 2f) / density;
        var y = MathUtils.RoundToInt((MathUtils.RoundToInt(height) - MathUtils.RoundToInt(scaledHeight)) / 2f) / density;
        var destination = new Rect(x, y, scaledWidth / density, scaledHeight / density);
        var sourceSize = new Size(sourceWidth / density, sourceHeight / density);
        using var mirror = context.PushTransform(AutoMirror && FlowDirection == FlowDirection.RightToLeft
            ? new Matrix(-1, 0, 0, 1, Bounds.Width, 0) : Matrix.Identity);
        var drawable = new SizedImage(source, sourceSize);
        if (!IsTintEnabled)
            context.DrawImage(drawable, new Rect(sourceSize), destination);
        else if (Foreground is { } tint)
        {
            // SrcIn: preserve only source alpha, including partial transparency. The wrapper
            // supplies finite bounds for custom IImage implementations without intrinsic size.
            // Keep the mask tile at the integer-pixel control bounds. Using the fractional
            // fitted image as the tile bounds rounds and rescales its alpha a second time.
            var bounds = new Rect(Bounds.Size);
            var drawing = new DrawingGroup();
            drawing.Children.Add(new GeometryDrawing { Brush = Brushes.Transparent, Geometry = new RectangleGeometry(bounds) });
            drawing.Children.Add(new ImageDrawing { ImageSource = drawable, Rect = destination });
            var mask = new DrawingBrush(drawing)
            {
                Stretch = Stretch.Fill,
                SourceRect = new RelativeRect(bounds, RelativeUnit.Absolute),
                DestinationRect = new RelativeRect(bounds, RelativeUnit.Absolute)
            };
            using (context.PushOpacityMask(mask, bounds)) context.FillRectangle(tint, bounds);
        }
    }

    private sealed class SizedImage(IImage source, Size size) : IImage
    {
        public Size Size => size;
        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
        {
            // Bitmap.Draw consumes source pixels, although IImage.Size is expressed in DIP.
            if (source is Bitmap bitmap)
                sourceRect = new Rect(sourceRect.X * bitmap.PixelSize.Width / size.Width, sourceRect.Y * bitmap.PixelSize.Height / size.Height,
                    sourceRect.Width * bitmap.PixelSize.Width / size.Width, sourceRect.Height * bitmap.PixelSize.Height / size.Height);
            source.Draw(context, sourceRect, destRect);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SourceProperty) ObserveSource();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) { base.OnAttachedToVisualTree(e); ObserveSource(); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_observedSource is { } source) source.Invalidated -= SourceInvalidated;
        _observedSource = null;
        base.OnDetachedFromVisualTree(e);
    }
    private void ObserveSource()
    {
        if (_observedSource is { } previous) previous.Invalidated -= SourceInvalidated;
        _observedSource = this.IsAttachedToVisualTree() ? Source as DrawingImage : null;
        if (_observedSource is { } source) source.Invalidated += SourceInvalidated;
    }
    private void SourceInvalidated(object? sender, EventArgs e) { InvalidateMeasure(); InvalidateVisual(); }

    protected override AutomationPeer OnCreateAutomationPeer() => new IconAutomationPeer(this);
    private sealed class IconAutomationPeer(MaterialIcon owner) : ControlAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(MaterialIcon);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Image;
        protected override string? GetNameCore() => base.GetNameCore() ?? owner.ContentDescription;
        protected override bool IsControlElementCore() => owner.ContentDescription is not null;
        protected override bool IsContentElementCore() => owner.ContentDescription is not null;
    }
}
