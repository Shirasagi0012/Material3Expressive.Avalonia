using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.VisualTesting.Model;
using MaterialColorUtilities.Avalonia.Tokens;

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

    public static IReadOnlyList<VisualCase> All { get; } =
    [
        RoundedRectangle(1, VisualTheme.Light),
        RoundedRectangle(3, VisualTheme.Light),
        RoundedRectangle(6, VisualTheme.Light),
        RoundedRectangle(6, VisualTheme.Dark),
        Circle(6, VisualTheme.Light),
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

    private static VisualCase Circle(double elevation, VisualTheme theme) =>
        Scene(
            $"foundation/elevation/circle/level{elevation:0}/{ThemeName(theme)}",
            theme,
            context => Surface(
                context,
                s_circleSize,
                new CornerRadius(s_circleSize.Width / 2),
                elevation));

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
        double elevation)
    {
        var shadowColor = ((SolidColorBrush)CaseLayout.Brush(context, SysColorToken.Shadow)).Color;
        return new ElevationSurface
        {
            Name = "component",
            Width = size.Width,
            Height = size.Height,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Corners = corners,
            Fill = CaseLayout.Brush(context, SysColorToken.SurfaceContainerLow),
            Shadows = Elevation.ToBoxShadows(elevation, shadowColor)
        };
    }

    private static VisualCase Scene(
        string id,
        VisualTheme theme,
        Func<VisualCaseContext, Control> createContent) =>
        new(
            id,
            s_viewport,
            theme,
            context => new Grid { Children = { createContent(context) } },
            RenderScaling: 1.0);

    private static string ThemeName(VisualTheme theme) => theme.IsDark ? "dark" : "light";

    private sealed class ElevationSurface : Control
    {
        public CornerRadius Corners { get; init; }

        public IBrush? Fill { get; init; }

        public BoxShadows Shadows { get; init; }

        public override void Render(DrawingContext context)
        {
            MaterialContainer.Draw(context, Bounds.Size, Corners, Fill, shadows: Shadows);
        }
    }
}
