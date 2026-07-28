using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.VisualTesting.Model;
using Material3Expressive.VisualTesting.Parity;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialSkiaShadow;

namespace Material3Expressive.VisualTesting.Cases;

/// <summary>
/// Renderer-level elevation scenes. These deliberately avoid Material components so a shadow
/// mismatch cannot be confused with a component token, layout, or interaction-state mismatch.
/// </summary>
internal static class ElevationParityCases
{
    private static readonly PixelSize s_viewport = new(240, 180);
    private static readonly Size s_roundedRectangleSize = new(96, 64);
    private static readonly Size s_circleSize = new(72, 72);
    private static readonly Size s_fractionalPillSize = new(90.5, 56);

    public static IReadOnlyList<VisualCase> All { get; } =
    [
        RoundedRectangle(1, VisualTheme.Light),
        FractionalPill(1, VisualTheme.Light),
        FractionalPillShadowComponent(ambient: true, spot: false),
        FractionalPillShadowComponent(ambient: false, spot: true),
        RoundedRectangle(3, VisualTheme.Light),
        RoundedRectangle(6, VisualTheme.Light),
        Rectangle(12, VisualTheme.Light),
        RoundedRectangleScale(6, 1.5, VisualTheme.Light),
        RoundedRectangle(6, VisualTheme.Dark),
        RoundedRectangleOpacity(6, 0.5, VisualTheme.Light),
        Circle(6, VisualTheme.Light),
        CircleShadowComponent(6, VisualTheme.Light, ambient: true, spot: false),
        CircleShadowComponent(6, VisualTheme.Light, ambient: false, spot: true),
        CircleShadowComponent(6, VisualTheme.Light, ambient: true, spot: false, drawOccluder: false),
        CircleShadowComponent(6, VisualTheme.Light, ambient: false, spot: true, drawOccluder: false),
        RoundedRectangleShadowComponent(6, VisualTheme.Light, ambient: true, spot: false),
        RoundedRectangleShadowComponent(6, VisualTheme.Light, ambient: false, spot: true),
        RoundedRectangleShadowComponent(6, VisualTheme.Light, ambient: true, spot: false, drawOccluder: false),
        RoundedRectangleShadowComponent(6, VisualTheme.Light, ambient: false, spot: true, drawOccluder: false),
        ParentClippedRoundedRectangle(6, VisualTheme.Light)
    ];

    private static VisualCase RoundedRectangle(double elevation, VisualTheme theme) =>
        Scene(
            $"foundation/elevation/rounded-rectangle/level{elevation:0}/{ThemeName(theme)}",
            theme,
            context => Surface(
                context,
                s_roundedRectangleSize,
                new CornerRadius(20),
                elevation));

    private static VisualCase Rectangle(double elevation, VisualTheme theme) =>
        Scene(
            $"foundation/elevation/rectangle/level{elevation:0}/{ThemeName(theme)}",
            theme,
            context => Surface(
                context,
                s_roundedRectangleSize,
                default,
                elevation));

    private static VisualCase RoundedRectangleOpacity(
        double elevation,
        double opacity,
        VisualTheme theme) =>
        Scene(
            $"foundation/elevation/rounded-rectangle/level{elevation:0}/opacity-{opacity * 100:0}/{ThemeName(theme)}",
            theme,
            context => Surface(
                context,
                s_roundedRectangleSize,
                new CornerRadius(20),
                elevation,
                opacity: opacity));

    private static VisualCase RoundedRectangleScale(
        double elevation,
        double scale,
        VisualTheme theme) =>
        Scene(
            $"foundation/elevation/rounded-rectangle/level{elevation:0}/scale-{scale:0.0}/{ThemeName(theme)}",
            theme,
            context => Surface(
                context,
                s_roundedRectangleSize,
                new CornerRadius(20),
                elevation));

    private static VisualCase Circle(double elevation, VisualTheme theme) =>
        Scene(
            $"foundation/elevation/circle/level{elevation:0}/{ThemeName(theme)}",
            theme,
            context => Surface(
                context,
                s_circleSize,
                new CornerRadius(s_circleSize.Width / 2),
                elevation));

    private static VisualCase FractionalPill(double elevation, VisualTheme theme) =>
        Scene(
            $"foundation/elevation/pill/level{elevation:0}/fractional-width/{ThemeName(theme)}",
            theme,
            context => Surface(
                context,
                s_fractionalPillSize,
                new CornerRadius(s_fractionalPillSize.Height / 2),
                elevation),
            new PixelSize(240, 96));

    private static VisualCase FractionalPillShadowComponent(bool ambient, bool spot) =>
        Scene(
            $"foundation/elevation/pill/level1/fractional-width/{(ambient ? "ambient-only" : "spot-only")}/light",
            VisualTheme.Light,
            context => Surface(
                context,
                s_fractionalPillSize,
                new CornerRadius(s_fractionalPillSize.Height / 2),
                1,
                ambient,
                spot),
            new PixelSize(240, 96));

    private static VisualCase CircleShadowComponent(
        double elevation,
        VisualTheme theme,
        bool ambient,
        bool spot,
        bool drawOccluder = true) =>
        Scene(
            $"foundation/elevation/circle/level{elevation:0}/{(ambient ? "ambient-only" : "spot-only")}/{(drawOccluder ? "" : "shadow-only/")}{ThemeName(theme)}",
            theme,
            context => Surface(
                context,
                s_circleSize,
                new CornerRadius(s_circleSize.Width / 2),
                elevation,
                ambient,
                spot,
                drawOccluder));

    private static VisualCase RoundedRectangleShadowComponent(
        double elevation,
        VisualTheme theme,
        bool ambient,
        bool spot,
        bool drawOccluder = true) =>
        Scene(
            $"foundation/elevation/rounded-rectangle/level{elevation:0}/{(ambient ? "ambient-only" : "spot-only")}/{(drawOccluder ? "" : "shadow-only/")}{ThemeName(theme)}",
            theme,
            context => Surface(
                context,
                s_roundedRectangleSize,
                new CornerRadius(20),
                elevation,
                ambient,
                spot,
                drawOccluder));

    private static VisualCase ParentClippedRoundedRectangle(double elevation, VisualTheme theme) =>
        Scene(
            $"foundation/elevation/rounded-rectangle/level{elevation:0}/parent-clipped/{ThemeName(theme)}",
            theme,
            context => new Border
            {
                Width = 112,
                Height = 80,
                ClipToBounds = true,
                Child = Surface(
                    context,
                    s_roundedRectangleSize,
                    new CornerRadius(20),
                    elevation)
            });

    private static Control Surface(
        VisualCaseContext context,
        Size size,
        CornerRadius corners,
        double elevation,
        bool ambient = true,
        bool spot = true,
        bool drawOccluder = true,
        double opacity = 1)
    {
        var shadowColor = ((SolidColorBrush)CaseLayout.Brush(context, SysColorToken.Shadow)).Color;
        var fill = drawOccluder ? CaseLayout.Brush(context, SysColorToken.Surface) : null;
        var surface = new ElevationSurface
        {
            Name = "component",
            Width = size.Width,
            Height = size.Height,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            LayerOpacity = opacity,
            Corners = corners,
            Fill = opacity >= 1 ? fill : null,
            Elevation = elevation,
            AmbientShadowColor = ambient ? shadowColor : Colors.Transparent,
            SpotShadowColor = spot ? shadowColor : Colors.Transparent
        };
        if (fill is null || opacity >= 1)
            return surface;

        var layer = new Grid
        {
            Width = size.Width,
            Height = size.Height,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                surface,
                new Border
                {
                    Background = fill,
                    CornerRadius = corners,
                    Opacity = opacity
                }
            }
        };
        RenderOptions.SetRequiresFullOpacityHandling(layer, true);
        return layer;
    }

    private static VisualCase Scene(
        string id,
        VisualTheme theme,
        Func<VisualCaseContext, Control> createContent,
        PixelSize? viewport = null) =>
        new(
            id,
            viewport ?? s_viewport,
            theme,
            context => new Grid { Children = { createContent(context) } },
            RenderScaling: 1.0);

    private static string ThemeName(VisualTheme theme) => theme.IsDark ? "dark" : "light";

    private sealed class ElevationSurface : Control, IParityShadowProbe
    {
        public CornerRadius Corners { get; init; }

        public IBrush? Fill { get; init; }

        public double Elevation { get; init; }

        public Color AmbientShadowColor { get; init; }

        public Color SpotShadowColor { get; init; }

        public double LayerOpacity { get; init; } = 1;

        public ComposeShadowShape ShadowShape =>
            ComposeShadowShape.RoundedRectangle(new Rect(Bounds.Size), Corners);

        public ComposeShadowParameters ShadowParameters => new(
            Elevation,
            AmbientShadowColor,
            SpotShadowColor,
            LayerOpacity);

        public override void Render(DrawingContext context)
        {
            ComposeShadowRenderer.Draw(
                context,
                this,
                ShadowShape,
                ShadowParameters);
            if (Fill is not null)
                MaterialContainer.Draw(context, Bounds.Size, Corners, Fill);
        }
    }
}
