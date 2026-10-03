using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class DividerTests
{
    [AvaloniaTheory]
    [InlineData(Orientation.Horizontal)]
    [InlineData(Orientation.Vertical)]
    public void NativeSeparator_UsesImplicitThemeAndAutomaticThickness(Orientation orientation)
    {
        var divider = new Separator(); MaterialDivider.SetOrientation(divider, orientation);
        var window = Show(divider);
        Assert.Equal(orientation == Orientation.Horizontal ? new Size(160, 1) : new Size(1, 120), divider.Bounds.Size);
        Assert.False(divider.Focusable); Assert.False(divider.IsHitTestVisible);
        Assert.False(divider.ClipToBounds);
        Assert.Equal(MaterialDivider.GetColor(divider), Part(divider).Color);
        var color = Part(divider).Color;
        divider.IsEnabled = false; window.UpdateLayout();
        Assert.Equal(color, Part(divider).Color);
        Assert.Equal(orientation == Orientation.Horizontal ? new Size(160, 1) : new Size(1, 120), divider.Bounds.Size);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(Orientation.Horizontal)]
    [InlineData(Orientation.Vertical)]
    public void ThicknessAndDirection_InvalidateLayoutWithoutReplacingSeparator(Orientation orientation)
    {
        var divider = new Separator(); MaterialDivider.SetOrientation(divider, orientation);
        var window = Show(divider);
        MaterialDivider.SetThickness(divider, 4); window.UpdateLayout();
        Assert.Equal(orientation == Orientation.Horizontal ? new Size(160, 4) : new Size(4, 120), divider.Bounds.Size);
        MaterialDivider.SetThickness(divider, 0); window.UpdateLayout();
        Assert.Equal(orientation == Orientation.Horizontal ? new Size(160, 0) : new Size(0, 120), divider.Bounds.Size);
        MaterialDivider.SetOrientation(divider, orientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal);
        MaterialDivider.SetThickness(divider, 2); window.UpdateLayout();
        Assert.Equal(orientation == Orientation.Horizontal ? new Size(2, 120) : new Size(160, 2), divider.Bounds.Size);
        window.Close();
    }

    [AvaloniaFact]
    public void TokenChangesAndBindings_ReachTheActualCanvasAndRespectLocalValues()
    {
        var divider = new Separator(); var window = Show(divider);
        window.Resources[Sys.Color.OutlineVariant] = Colors.Red;
        Assert.Equal(Colors.Red, Part(divider).Color);
        window.Resources[Comp.Divider.Thickness] = 3d; window.UpdateLayout();
        Assert.Equal(3, Part(divider).Thickness); Assert.Equal(3, divider.Bounds.Height);
        window.Resources[Comp.Divider.Color] = Colors.Green;
        Assert.Equal(Colors.Green, Part(divider).Color);
        divider.Bind(MaterialDivider.ColorProperty, new Binding(nameof(Border.Background))
        {
            Source = new Border { Background = Brushes.Blue },
            Converter = new Avalonia.Data.Converters.FuncValueConverter<IBrush?, Color>(brush => ((ISolidColorBrush)brush!).Color)
        });
        Assert.Equal(Colors.Blue, Part(divider).Color);
        window.Resources[Comp.Divider.Color] = Colors.Yellow;
        Assert.Equal(Colors.Blue, Part(divider).Color);
        divider.ClearValue(MaterialDivider.ColorProperty);
        Assert.Equal(Colors.Yellow, Part(divider).Color);
        window.Close();
    }

    [AvaloniaFact]
    public void ExplicitCrossSizeAndTightConstraints_DoNotChangeStrokeWidth()
    {
        var divider = new Separator { Height = 20 };
        MaterialDivider.SetThickness(divider, 4);
        var window = Show(divider);
        Assert.Equal(20, divider.Bounds.Height); Assert.Equal(4, Part(divider).Thickness);
        divider.ClearValue(Control.HeightProperty);
        ((Grid)window.Content!).Height = 2; window.UpdateLayout();
        Assert.Equal(2, divider.Bounds.Height); Assert.Equal(4, Part(divider).Thickness);
        window.Close();
    }

    [AvaloniaFact]
    public void Xaml_AttachedParametersAndNamedThemeResolve()
    {
        var divider = new DividerFixture();
        var window = Show(divider);
        Assert.Equal(Orientation.Vertical, Part(divider).Orientation);
        Assert.Equal(2.5, Part(divider).Thickness);
        Assert.Equal(Color.Parse("#126E60"), Part(divider).Color);
        Assert.True(divider.TryFindResource("MaterialDividerTheme", out var theme));
        Assert.IsType<Avalonia.Styling.ControlTheme>(theme);
        window.Close();
    }

    static DividerPresenter Part(Separator divider) => divider.GetVisualDescendants().OfType<DividerPresenter>().Single();
    static Window Show(Separator divider)
    {
        var window = new Window { Width = 200, Height = 160 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Width = 160, Height = 120, Children = { divider } };
        window.Show(); window.UpdateLayout(); return window;
    }
}

public sealed partial class DividerFixture : Separator
{
    protected override Type StyleKeyOverride => typeof(Separator);
    public DividerFixture() => AvaloniaXamlLoader.Load(this);
}
