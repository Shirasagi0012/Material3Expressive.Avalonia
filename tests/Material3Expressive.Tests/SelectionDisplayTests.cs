using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Xunit;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Tests;

public class SelectionDisplayTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void DisplayHasNoInteractiveMinimumFocusOrAutomationAction(bool radio)
    {
        using var clock = new MotionTestClockScope();
        TemplatedControl display = radio ? new RadioButtonDisplay() : new CheckBoxDisplay { IsChecked = null };
        var grid = new Grid { Children = { display } };
        MaterialInteractiveSize.SetMinimum(grid, 100);
        var window = DialogTests.Show(grid);
        try
        {
            Assert.Equal(new Size(24, 24), display.Bounds.Size);
            Assert.False(display.Focus()); Assert.False(display.IsHitTestVisible);
            var peer = ControlAutomationPeer.CreatePeerForElement(display);
            Assert.False(peer is IToggleProvider); Assert.False(peer is ISelectionItemProvider); Assert.False(peer is IInvokeProvider);
            Assert.False(MaterialInteraction.GetIsInteractive(display));
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ParentOwnsTheClick_WhileBindingDrivesTheDisplay(bool radio)
    {
        using var clock = new MotionTestClockScope();
        TemplatedControl display = radio ? new RadioButtonDisplay() : new CheckBoxDisplay();
        var parent = new ToggleableSurface { Content = display, Width = 120, Height = 56,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var requests = 0;
        parent.CheckedChangeRequested += (_, e) => { requests++; parent.IsChecked = !parent.IsChecked; };
        display.Bind(radio ? RadioButtonDisplay.IsCheckedProperty : CheckBoxDisplay.IsCheckedProperty,
            new Avalonia.Data.Binding(nameof(ToggleableSurface.IsChecked)) { Source = parent });
        var window = DialogTests.Show(parent);
        try
        {
            var point = display.TranslatePoint(new Point(12, 12), window)!.Value;
            window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left);
            Assert.Equal(1, requests); Assert.True(parent.IsChecked);
            Assert.True(radio ? ((RadioButtonDisplay)display).IsChecked : ((CheckBoxDisplay)display).IsChecked);
            Assert.False(display.IsFocused);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void RequiredIndicatorOverflowsTightConstraints_AndTokensRemainLive(bool radio)
    {
        using var clock = new MotionTestClockScope();
        TemplatedControl display = radio ? new RadioButtonDisplay() : new CheckBoxDisplay();
        display.Width = 12; display.Height = 12;
        var window = DialogTests.Show(display);
        try
        {
            Control Drawing() => display.GetVisualDescendants().Single(c => radio ? c is RadioButtonIndicator : c is CheckBoxIndicator) as Control ?? throw new InvalidOperationException();
            var drawing = Drawing();
            Assert.Equal(new Size(20, 20), drawing.Bounds.Size);
            Assert.Equal(new Point(-4, -4), drawing.TranslatePoint(default, display));
            display.ClearValue(Layoutable.WidthProperty); display.ClearValue(Layoutable.HeightProperty);
            window.Resources[radio ? Comp.RadioButton.IndicatorSize : Comp.CheckBox.IndicatorSize] = 28d;
            window.Resources[radio ? Comp.RadioButton.IndicatorPadding : Comp.CheckBox.IndicatorPadding] = new Thickness(3);
            window.UpdateLayout();
            Assert.Equal(new Size(34, 34), display.Bounds.Size);
            Assert.Equal(new Size(28, 28), Drawing().Bounds.Size);
        }
        finally { window.Close(); }
    }
}
