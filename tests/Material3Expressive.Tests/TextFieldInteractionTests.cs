using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Styles;
using Xunit;

namespace Material3Expressive.Tests;

public class TextFieldInteractionTests
{
    [AvaloniaFact]
    public void OutlinedReenableInitializesBorderAtCurrentFocusTarget()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var window = Show(source, "MaterialOutlinedTextFieldTheme", out var box, out _);
        var layout = box.GetVisualDescendants().OfType<TextFieldLayout>().Single();
        source.Emit(new MaterialInteractionEvent.Focus());
        Advance(clock, window);
        box.IsEnabled = false;
        Advance(clock, window);
        Assert.Equal(1, layout.IndicatorThickness);
        box.IsEnabled = true;
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        window.UpdateLayout();
        Assert.Equal(2, layout.IndicatorThickness);
        window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData("MaterialOutlinedTextFieldTheme")]
    [InlineData("MaterialOutlinedSecureTextFieldTheme")]
    public void OutlineTracksContainerAndAnimatedCutoutWithoutAddingContentPadding(string theme)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var window = Show(source, theme, out var box, out _);
        var layout = box.GetVisualDescendants().OfType<TextFieldLayout>().Single();
        var decoration = box.GetVisualDescendants().OfType<TextFieldDecoration>().Single();
        var outline = decoration.GetVisualDescendants().OfType<SurfaceOutline>().Single();
        var originalSize = box.Bounds.Size;
        box.CornerRadius = new CornerRadius(.1, 4, 12, 0);
        source.Emit(new MaterialInteractionEvent.Focus());
        for (var i = 0; i < 5; i++)
        {
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            window.UpdateLayout();
            Assert.Equal(layout.ContainerBounds, outline.Bounds);
            Assert.Equal((double)layout.IndicatorThickness, outline.BorderThickness.Top);
            Assert.Equal(box.CornerRadius, outline.CornerRadius);
            var clip = Assert.IsType<GeometryGroup>(decoration.Clip);
            Assert.Equal(FillRule.EvenOdd, clip.FillRule);
            if (layout.CutoutBounds.Width > 0)
                Assert.Equal(layout.CutoutBounds, Assert.IsType<RectangleGeometry>(clip.Children[1]).Rect);
            else Assert.Single(clip.Children);
            Assert.Equal(originalSize, box.Bounds.Size);
        }
        Assert.True(layout.CutoutBounds.Width > 0);
        window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void FilledIndicatorRetainsMomentumWhenFocusReverses()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var window = Show(source, "MaterialFilledTextFieldTheme", out var box, out _);
        var layout = box.GetVisualDescendants().OfType<TextFieldLayout>().Single();
        layout.SpatialAnimation = new(.5f, 100f);
        var focus = new MaterialInteractionEvent.Focus();
        source.Emit(focus);
        for (var frame = 0; frame < 5; frame++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        var before = layout.IndicatorThickness;
        source.Emit(new MaterialInteractionEvent.Unfocus(focus));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.True(layout.IndicatorThickness > before,
            $"IndicatorLineNode must retain its spring momentum: {before} -> {layout.IndicatorThickness}.");
        Advance(clock, window);
        Assert.Equal(1, layout.IndicatorThickness);
        window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData("MaterialFilledTextFieldTheme")]
    [InlineData("MaterialOutlinedTextFieldTheme")]
    [InlineData("MaterialFilledSecureTextFieldTheme")]
    [InlineData("MaterialOutlinedSecureTextFieldTheme")]
    public void ExternalFocusUsesIdentityAndDrivesDecorationWithoutEditingFocus(string theme)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var window = Show(source, theme, out var box, out _);
        var layout = box.GetVisualDescendants().OfType<TextFieldLayout>().Single();
        var restingColor = ((ISolidColorBrush)box.BorderBrush!).Color;
        var first = new MaterialInteractionEvent.Focus();
        var second = new MaterialInteractionEvent.Focus();
        source.Emit(first);
        Advance(clock, window);
        Assert.False(box.IsFocused);
        Assert.Equal(1, layout.LabelProgress);
        Assert.Equal(2, layout.IndicatorThickness);
        Assert.NotEqual(restingColor, ((ISolidColorBrush)box.BorderBrush!).Color);
        source.Emit(second);
        source.Emit(new MaterialInteractionEvent.Unfocus(first));
        source.Emit(new MaterialInteractionEvent.Unfocus(new MaterialInteractionEvent.Focus()));
        Advance(clock, window);
        Assert.Equal(1, layout.LabelProgress);
        source.Emit(new MaterialInteractionEvent.Unfocus(second));
        Advance(clock, window);
        Assert.Equal(0, layout.LabelProgress);
        Assert.Equal(1, layout.IndicatorThickness);
        Assert.Equal(restingColor, ((ISolidColorBrush)box.BorderBrush!).Color);
        window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void NativeFocusPairsAndSourceReplacementDoesNotReplayFocus()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var events = new List<MaterialInteractionEvent>();
        source.Interaction += events.Add;
        var window = Show(source, "MaterialFilledTextFieldTheme", out var box, out var other);
        box.Focus();
        var focus = Assert.IsType<MaterialInteractionEvent.Focus>(Assert.Single(events));
        var next = new MaterialInteractionSource();
        var nextEvents = new List<MaterialInteractionEvent>();
        next.Interaction += nextEvents.Add;
        MaterialTextField.SetInteractionSource(box, next);
        Assert.Same(focus, Assert.IsType<MaterialInteractionEvent.Unfocus>(events[1]).Start);
        Assert.True(box.IsFocused);
        Assert.Empty(nextEvents);
        // collectIsFocusedAsState retains its remembered value until any new source event.
        Assert.True(TextFieldThemeDefaults.GetIsFocused(box));
        next.Emit(new MaterialInteractionEvent.HoverEnter());
        Assert.False(TextFieldThemeDefaults.GetIsFocused(box));
        other.Focus();
        box.Focus();
        Assert.IsType<MaterialInteractionEvent.Focus>(nextEvents[^1]);
        box.IsEnabled = false;
        Assert.IsType<MaterialInteractionEvent.Unfocus>(nextEvents[^1]);
        Assert.False(TextFieldThemeDefaults.GetIsFocused(box));
        window.Close();
    }

    [AvaloniaFact]
    public void PointerAndKeyboardDoNotTurnTextEditingIntoButtonFeedback()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var events = new List<MaterialInteractionEvent>();
        source.Interaction += events.Add;
        var window = Show(source, "MaterialFilledTextFieldTheme", out var box, out _);
        var center = box.TranslatePoint(new Point(box.Bounds.Width / 2, box.Bounds.Height / 2), window)!.Value;
        window.MouseMove(center);
        Assert.Empty(events);
        window.MouseDown(center, MouseButton.Left);
        var press = Assert.Single(events.OfType<MaterialInteractionEvent.Press>());
        window.MouseUp(center, MouseButton.Left);
        Assert.Same(press, Assert.Single(events.OfType<MaterialInteractionEvent.Release>()).Start);
        events.Clear();
        window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.None, null);
        window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.None, null);
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.None, null);
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.None, null);
        Assert.Empty(events);
        window.MouseDown(center, MouseButton.Left);
        press = Assert.Single(events.OfType<MaterialInteractionEvent.Press>());
        box.IsEnabled = false;
        Assert.Same(press, Assert.Single(events.OfType<MaterialInteractionEvent.CancelPress>()).Start);
        window.MouseUp(center, MouseButton.Left);
        Assert.Empty(events.OfType<MaterialInteractionEvent.Release>());
        window.Close();
    }

    [AvaloniaFact]
    public void DetachUnsubscribesAndReattachUsesOnlyFutureExternalEvents()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var window = Show(source, "MaterialOutlinedTextFieldTheme", out var box, out _);
        source.Emit(new MaterialInteractionEvent.Focus());
        var panel = (StackPanel)window.Content!;
        panel.Children.Remove(box);
        source.Emit(new MaterialInteractionEvent.Focus());
        Assert.False(TextFieldThemeDefaults.GetIsFocused(box));
        panel.Children.Add(box);
        window.UpdateLayout();
        Assert.False(TextFieldThemeDefaults.GetIsFocused(box));
        source.Emit(new MaterialInteractionEvent.Focus());
        Assert.True(TextFieldThemeDefaults.GetIsFocused(box));
        window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void FilledIndicatorSourceRestartPreservesPixelsUntilItsOwnInvalidation()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var window = Show(source, "MaterialFilledTextFieldTheme", out var box, out _);
        var layout = box.GetVisualDescendants().OfType<TextFieldLayout>().Single();
        source.Emit(new MaterialInteractionEvent.Focus());
        Advance(clock, window);
        var focusedColor = ((ISolidColorBrush)box.BorderBrush!).Color;
        var next = new MaterialInteractionSource();
        MaterialTextField.SetInteractionSource(box, next);
        next.Emit(new MaterialInteractionEvent.HoverEnter());
        Advance(clock, window);
        Assert.Equal(0, layout.LabelProgress);
        Assert.Equal(2, layout.IndicatorThickness);
        Assert.Equal(focusedColor, ((ISolidColorBrush)box.BorderBrush!).Color);
        // IndicatorLineNode.update invalidates on enabled/error changes and uses its reset
        // focus boolean. A source restart alone deliberately does not cause this repaint.
        MaterialTextField.SetIsError(box, true);
        Advance(clock, window);
        Assert.Equal(1, layout.IndicatorThickness);
        MaterialTextField.SetIsError(box, false);
        Advance(clock, window);
        Assert.NotEqual(focusedColor, ((ISolidColorBrush)box.BorderBrush!).Color);
        window.Close();
    }

    [AvaloniaFact]
    public void ReentrantSourceReplacementCannotReconnectTheSupersededSource()
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var window = Show(source, "MaterialFilledTextFieldTheme", out var box, out var other);
        box.Focus();
        var skipped = new MaterialInteractionSource();
        var current = new MaterialInteractionSource();
        source.Interaction += interaction =>
        {
            if (interaction is MaterialInteractionEvent.Unfocus) MaterialTextField.SetInteractionSource(box, current);
        };
        MaterialTextField.SetInteractionSource(box, skipped);
        other.Focus();
        var events = new List<MaterialInteractionEvent>();
        current.Interaction += events.Add;
        box.Focus();
        Assert.IsType<MaterialInteractionEvent.Focus>(Assert.Single(events));
        window.Close();
    }

    private static MotionTestWindow Show(MaterialInteractionSource source, string theme, out TextBox box, out Control other)
    {
        var window = new MotionTestWindow { Width = 420, Height = 200 };
        window.Styles.Add(new FluentTheme());
        window.Styles.Add(new MaterialTheme());
        box = new TextBox { Width = 280, Height = 80, Theme = (ControlTheme)window.FindResource(theme)! };
        MaterialTextField.SetLabel(box, new Border { Width = 40, Height = 12, Background = Brushes.Red });
        MaterialTextField.SetInteractionSource(box, source);
        other = new Border { Width = 12, Height = 12, Focusable = true };
        window.Content = new StackPanel { Children = { other, box } };
        window.Show();
        window.UpdateLayout();
        other.Focus();
        return window;
    }

    private static void Advance(MotionTestClockScope clock, Window window)
    {
        for (var i = 0; i < 80; i++)
        {
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            window.UpdateLayout();
        }
    }
}
