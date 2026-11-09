using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation;
using Xunit;

namespace Material3Expressive.Tests;

public class TonalElevationEnvironmentTests
{
    [AvaloniaFact]
    public void DisabledSubtreeKeepsElevationAndShadowAndCanReenableBelow()
    {
        var inner = new MaterialSurface { TonalElevation = 3, ShadowElevation = 6 };
        var outer = new MaterialSurface { TonalElevation = 1, Child = new Border { Child = inner } };
        var root = new Grid { Children = { outer } };
        using var clock = new MotionTestClockScope(); var window = DialogTests.Show(root);
        try
        {
            var tinted = Color(inner);
            MaterialTonalElevation.SetIsEnabled(root, false);
            Assert.Equal(Raw(inner), Color(inner)); Assert.Equal(Raw(outer), Color(outer));
            Assert.Equal(4, inner.AbsoluteTonalElevation); Assert.Equal(6, inner.ShadowElevation);
            MaterialTonalElevation.SetIsEnabled(inner, true);
            Assert.Equal(tinted, Color(inner)); Assert.Equal(Raw(outer), Color(outer));
            outer.TonalElevation = 5;
            Assert.Equal(8, inner.AbsoluteTonalElevation); Assert.NotEqual(tinted, Color(inner));
            inner.ClearValue(MaterialTonalElevation.IsEnabledProperty);
            Assert.Equal(Raw(inner), Color(inner));
            root.ClearValue(MaterialTonalElevation.IsEnabledProperty);
            Assert.NotEqual(Raw(inner), Color(inner)); Assert.True(MaterialTonalElevation.GetIsEnabled(inner));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ReparentingAndResourceChangesReadTheCurrentEnvironment()
    {
        var child = new MaterialSurface { TonalElevation = 3 };
        var disabled = new Border { Child = child }; var enabled = new Border();
        MaterialTonalElevation.SetIsEnabled(disabled, false);
        using var clock = new MotionTestClockScope(); var window = DialogTests.Show(new StackPanel { Children = { disabled, enabled } });
        try
        {
            Assert.Equal(Raw(child), Color(child));
            child.Resources[Tokens.MaterialTokens.Sys.Color.Surface] = Colors.Beige;
            Assert.Equal(Colors.Beige, Color(child));
            disabled.Child = null; enabled.Child = child; window.UpdateLayout();
            Assert.True(MaterialTonalElevation.GetIsEnabled(child)); Assert.NotEqual(Raw(child), Color(child));
            enabled.Child = null; disabled.Child = child; window.UpdateLayout();
            Assert.False(MaterialTonalElevation.GetIsEnabled(child)); Assert.Equal(Colors.Beige, Color(child));
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("clickable")] [InlineData("selectable")] [InlineData("toggleable")] [InlineData("bottom-bar")]
    public void ComponentTemplatesConsumeTheAmbientGate(string kind)
    {
        Control control = kind switch
        {
            "clickable" => new ClickableSurface { TonalElevation = 3 },
            "selectable" => new SelectableSurface { TonalElevation = 3 },
            "toggleable" => new ToggleableSurface { TonalElevation = 3 },
            _ => new BottomAppBar { TonalElevation = 3, Background = new SolidColorBrush(Colors.Beige) }
        };
        control.Resources[Tokens.MaterialTokens.Sys.Color.Surface] = Colors.Beige;
        using var clock = new MotionTestClockScope(); var window = DialogTests.Show(control);
        try
        {
            var surface = control.GetVisualDescendants().OfType<MaterialSurface>().First();
            Assert.NotEqual(Raw(surface), Color(surface));
            MaterialTonalElevation.SetIsEnabled(control, false);
            Assert.Equal(Raw(surface), Color(surface));
            MaterialTonalElevation.SetIsEnabled(control, true);
            Assert.NotEqual(Raw(surface), Color(surface));
        }
        finally { window.Close(); }
    }

    private static Color Color(MaterialSurface surface) => Assert.IsAssignableFrom<ISolidColorBrush>(surface.EffectiveBackground).Color;
    private static Color Raw(MaterialSurface surface) => Assert.IsAssignableFrom<ISolidColorBrush>(surface.Background).Color;
}
