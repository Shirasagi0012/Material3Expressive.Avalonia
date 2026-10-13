using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Diagnostics;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public class NativeToolTipTests
{
    [AvaloniaFact]
    public void NativeService_OpensTheMaterialThemeAndClosesOnPointerPress()
    {
        var anchor = Anchor("Save changes");
        using var host = new Host(anchor);
        Assert.Equal(400, ToolTip.GetShowDelay(anchor));
        Assert.Equal(100, ToolTip.GetBetweenShowDelay(anchor));
        Assert.Equal(PlacementMode.Top, ToolTip.GetPlacement(anchor));
        Assert.Equal(-4, ToolTip.GetVerticalOffset(anchor));
        Assert.True(ToolTip.GetShouldUseOverlayLayer(anchor));
        Assert.True(ToolTip.GetServiceEnabled(anchor));

        // Use the real raw-input path; no animation root or TooltipState is installed.
        ToolTip.SetShowDelay(anchor, 0);
        host.Window.MouseMove(host.Center);
        host.Flush();
        var tip = host.Tip;
        Assert.True(ToolTip.GetIsOpen(anchor));
        Assert.Single(tip.GetVisualAncestors().OfType<OverlayPopupHost>());
        Assert.Equal(1, tip.Opacity);
        Assert.True(tip.Transitions is null || tip.Transitions.Count == 0);
        Assert.Equal("Save changes", tip.GetVisualDescendants().OfType<TextBlock>().Single().Text);
        var origin = anchor.TranslatePoint(default, host.Window)!.Value;
        var tipOrigin = tip.TranslatePoint(default, host.Window)!.Value;
        Assert.Equal(origin.Y - tip.Bounds.Height - 4, tipOrigin.Y);

        host.Window.MouseDown(host.Center, MouseButton.Left);
        host.Flush();
        Assert.False(ToolTip.GetIsOpen(anchor));
        Assert.False(tip.IsAttachedToVisualTree());
        host.Window.MouseUp(host.Center, MouseButton.Left);
    }

    [AvaloniaTheory]
    [InlineData(0, 0, 40, 24)]
    [InlineData(64, 16, 80, 24)]
    [InlineData(300, 8, 200, 24)]
    [InlineData(8, 48, 40, 56)]
    public void PopupContent_UsesMaterialSizeAndPadding(double width, double height, double expectedWidth, double expectedHeight)
    {
        var content = new IntrinsicContent(width, height);
        var anchor = Anchor(content);
        using var host = new Host(anchor);
        ToolTip.SetIsOpen(anchor, true);
        host.Flush();
        Assert.Equal(new Size(expectedWidth, expectedHeight), host.Tip.Bounds.Size);
        Assert.Equal(new Point(8, 4), content.TranslatePoint(default, host.Tip));
        Assert.Equal(new CornerRadius(4), host.Tip.CornerRadius);
    }

    [AvaloniaFact]
    public void Content_TracksThemeTokensAndPreservesInlineAndLocalOverrides()
    {
        var bold = new Run("Title") { FontWeight = FontWeight.SemiBold };
        var text = new TextBlock { Inlines = new InlineCollection { bold, new LineBreak(), new Run("Description") }, TextWrapping = TextWrapping.Wrap };
        var anchor = Anchor(text);
        using var host = new Host(anchor);
        ToolTip.SetIsOpen(anchor, true);
        host.Flush();
        Assert.Equal(12, text.FontSize);
        Assert.Equal("Roboto", text.FontFamily.Name);
        Assert.Equal(16, text.LineHeight);
        Assert.Equal(.4, text.LetterSpacing);
        Assert.Equal(FontWeight.SemiBold, bold.FontWeight);
        Assert.True(host.Tip.Bounds.Height > 24);

        host.Window.Resources[Sys.Color.InverseSurface] = Colors.Blue;
        host.Window.Resources[Sys.Color.InverseOnSurface] = Colors.Yellow;
        host.Window.Resources[Sys.TypeScale.BodySmall.Size] = 14d;
        host.Flush();
        Assert.Equal(Colors.Blue, ((ISolidColorBrush)host.Tip.Background!).Color);
        Assert.Equal(Colors.Yellow, ((ISolidColorBrush)text.Foreground!).Color);
        Assert.Equal(14, text.FontSize);

        text.Foreground = Brushes.Red;
        text.FontSize = 18;
        host.Window.Resources[Sys.Color.InverseOnSurface] = Colors.Green;
        host.Window.Resources[Sys.TypeScale.BodySmall.Size] = 15d;
        host.Flush();
        Assert.Equal(Colors.Red, ((ISolidColorBrush)text.Foreground!).Color);
        Assert.Equal(18, text.FontSize);
        Assert.Equal(FontWeight.SemiBold, bold.FontWeight);
    }

    [AvaloniaFact]
    public void ExplicitTipAndAnchorValues_OverrideDefaultsUsingNativeConstraints()
    {
        var tip = new ToolTip { Content = new IntrinsicContent(400, 8), Width = 300, Background = Brushes.Blue };
        var anchor = Anchor(tip);
        ToolTip.SetPlacement(anchor, PlacementMode.Bottom);
        ToolTip.SetVerticalOffset(anchor, 4);
        ToolTip.SetShowDelay(anchor, 600);
        using var host = new Host(anchor);
        ToolTip.SetIsOpen(anchor, true);
        host.Flush();
        Assert.Same(tip, host.Tip);
        Assert.Equal(PlacementMode.Bottom, ToolTip.GetPlacement(anchor));
        Assert.Equal(4, ToolTip.GetVerticalOffset(anchor));
        Assert.Equal(600, ToolTip.GetShowDelay(anchor));
        Assert.Equal(Colors.Blue, ((ISolidColorBrush)tip.Background!).Color);
        Assert.Equal(200, tip.Bounds.Width);
        tip.MaxWidth = 320;
        host.Flush();
        Assert.Equal(300, tip.Bounds.Width);
        tip.Width = 20;
        host.Flush();
        Assert.Equal(40, tip.Bounds.Width);
        tip.MinWidth = 0;
        host.Flush();
        Assert.Equal(20, tip.Bounds.Width);
    }

    [AvaloniaFact]
    public void BoundTipContent_UpdatesThroughTheNativeService()
    {
        var source = new TextBlock { Text = "First" };
        var anchor = Anchor(null);
        using var binding = anchor.Bind(ToolTip.TipProperty, source.GetObservable(TextBlock.TextProperty));
        using var host = new Host(anchor);
        ToolTip.SetShowDelay(anchor, 0);
        host.Window.MouseMove(host.Center);
        host.Flush();
        var tip = host.Tip;
        source.Text = "Second";
        host.Flush();
        Assert.Same(tip, host.Tip);
        Assert.Equal("Second", tip.GetVisualDescendants().OfType<TextBlock>().Single().Text);
        source.Text = null;
        host.Flush();
        Assert.False(ToolTip.GetIsOpen(anchor));
    }

    static Border Anchor(object? tip)
    {
        var anchor = new Border { Width = 40, Height = 24, Background = Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        ToolTip.SetTip(anchor, tip);
        return anchor;
    }

    sealed class Host : IDisposable
    {
        private readonly Border _anchor;
        // An ordinary Window intentionally has no AnimationScope.Initialize call.
        public Window Window { get; } = new() { Width = 400, Height = 240 };
        public Point Center => _anchor.TranslatePoint(new Point(20, 12), Window)!.Value;
        public ToolTip Tip => Assert.IsType<ToolTip>(_anchor.GetValue(ToolTipDiagnostics.ToolTipProperty));

        public Host(Border anchor)
        {
            _anchor = anchor;
            Window.Styles.Add(new FluentTheme());
            Window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
            Window.Content = new Grid { Children = { anchor } };
            Window.Show();
            Flush();
        }
        public void Flush()
        {
            Dispatcher.UIThread.RunJobs();
            Window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }
        public void Dispose() { Window.Close(); Dispatcher.UIThread.RunJobs(); }
    }

    sealed class IntrinsicContent(double width, double height) : Control
    {
        protected override Size MeasureOverride(Size availableSize) => new Size(width, height).Constrain(availableSize);
    }
}
