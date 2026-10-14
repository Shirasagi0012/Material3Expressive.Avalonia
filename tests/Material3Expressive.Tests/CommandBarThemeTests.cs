using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;
using MaterialSymbols;
using System.Runtime.InteropServices;
using Xunit;

namespace Material3Expressive.Tests;

public class CommandBarThemeTests
{
    [AvaloniaTheory]
    [InlineData("MaterialCommandBarStackedTheme", false)]
    [InlineData("MaterialCommandBarStackedTheme", true)]
    [InlineData("MaterialCommandBarInlineTheme", false)]
    [InlineData("MaterialCommandBarInlineTheme", true)]
    [InlineData("MaterialCommandBarIconTheme", false)]
    [InlineData("MaterialCommandBarIconTheme", true)]
    public void Overflow_UsesStandardMenuLayoutAndShape_WithExistingSelectionTracks(string themeKey, bool dark)
    {
        using var clock = new MotionTestClockScope();
        var primary = new CommandBarToggleButton { Label = "Pin", Icon = Symbol(), IsChecked = true };
        var overflow = new CommandBarToggleButton { Label = "Pin", Icon = Symbol(), IsChecked = true };
        var textOnly = new CommandBarButton { Label = "Read only" };
        var bar = new CommandBar { PrimaryCommands = [primary], SecondaryCommands = [overflow, textOnly] };
        var window = Show(bar, themeKey, dark ? ThemeVariant.Dark : ThemeVariant.Light, includeFluentTheme: true);
        try
        {
            window.Height = 300;
            var reference = new MenuItem
            {
                Header = "Pin",
                Icon = Symbol(),
                Theme = (ControlTheme)window.FindResource("MaterialVerticalMenuItemTheme")!
            };
            window.Content = null;
            window.Content = new StackPanel { Children = { bar, reference } };
            window.UpdateLayout();
            bar.IsOpen = true;
            window.UpdateLayout();

            var layout = Part<CommandBarItemLayout>(overflow, "CommandLayout");
            var icon = Part<ContentPresenter>(overflow, "PART_Icon");
            var label = Part<ContentPresenter>(overflow, "PART_Label");
            var ripple = Part<MaterialSurface>(overflow, "PART_IndicatorRipple");
            var indicator = Part<Border>(overflow, "PART_Indicator");
            var referenceSurface = Part<MaterialSurface>(reference, "PART_ItemSurface");
            var referenceIcon = Part<Grid>(reference, "PART_LeadingIcon");
            var referenceLabel = Part<ContentPresenter>(reference, "PART_HeaderPresenter");
            var text = label.GetVisualDescendants().OfType<TextBlock>().Single();
            var referenceText = referenceLabel.GetVisualDescendants().OfType<TextBlock>().Single();

            Assert.Equal(reference.Margin, overflow.Margin);
            Assert.Equal(referenceSurface.Padding, layout.MenuContentPadding);
            Assert.Equal(referenceIcon.Bounds.Width, icon.Bounds.Width);
            Assert.Equal(20, icon.Bounds.Width);
            Assert.Equal(referenceLabel.Bounds.Left - referenceIcon.Bounds.Right, label.Bounds.Left - icon.Bounds.Right);
            Assert.Equal(8, label.Bounds.Left - icon.Bounds.Right);
            Assert.Equal(referenceText.FontSize, text.FontSize);
            Assert.Equal(referenceText.FontWeight, text.FontWeight);
            Assert.Equal(referenceSurface.Bounds.Height, ripple.Bounds.Height);
            Assert.Equal(44, ripple.Bounds.Height);
            Assert.Equal(48, layout.Bounds.Height);
            Assert.Equal(referenceSurface.GetValue(ButtonSurfaceState.CheckedCornerRadiusProperty), indicator.CornerRadius);
            Assert.True(indicator.CornerRadius.TopLeft < ripple.Bounds.Height / 2);
            Assert.Equal(layout.MenuSelectedItemShape, ripple.CornerRadius);
            Assert.Equal(Part<Border>(primary, "PART_Indicator").Background, indicator.Background);
            AssertCommandColors(overflow);

            var textOnlyLayout = Part<CommandBarItemLayout>(textOnly, "CommandLayout");
            Assert.False(Part<ContentPresenter>(textOnly, "PART_Icon").IsVisible);
            Assert.Equal(textOnlyLayout.MenuContentPadding.Left, Part<ContentPresenter>(textOnly, "PART_Label").Bounds.Left);

            if (Environment.GetEnvironmentVariable("M3E_COMMAND_BAR_CAPTURE") is { Length: > 0 } directory)
            {
                Directory.CreateDirectory(directory);
                using var bitmap = window.CaptureRenderedFrame()!;
                bitmap.Save(Path.Combine(directory, $"{themeKey}-{window.ActualThemeVariant}-overflow.png"), new PngBitmapEncoderOptions());
            }

            var pointer = overflow.TranslatePoint(new Rect(overflow.Bounds.Size).Center, window)!.Value;
            window.MouseMove(pointer);
            overflow.Focus(NavigationMethod.Pointer);
            using var beforePress = window.CaptureRenderedFrame()!;
            window.MouseDown(pointer, MouseButton.Left);
            Assert.True(overflow.IsPressed);
            for (var frame = 0; frame < 5; frame++)
                clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            using var duringPress = window.CaptureRenderedFrame()!;
            Assert.False(Pixels(beforePress).AsSpan().SequenceEqual(Pixels(duringPress)),
                "The overflow menu's rounded rectangle produced no visible ripple.");
            Assert.Equal(1, MaterialInteraction.GetOrCreate(overflow).RippleCount);
            window.MouseUp(pointer, MouseButton.Left);
            Assert.False(overflow.IsChecked);
            primary.IsChecked = false;
            for (var frame = 0; frame < 5; frame++)
            {
                clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
                window.UpdateLayout();
                var primaryLayout = Part<CommandBarItemLayout>(primary, "CommandLayout");
                Assert.Equal(primaryLayout.IndicatorSize, layout.IndicatorSize);
                Assert.Equal(primaryLayout.IndicatorOpacity, layout.IndicatorOpacity);
                Assert.Equal(primaryLayout.AnimatedIconColor, layout.AnimatedIconColor);
                Assert.Equal(primaryLayout.AnimatedTextColor, layout.AnimatedTextColor);
            }

            window.Resources[Material3Expressive.Tokens.MaterialTokens.Comp.Menu.SelectedItemShape] = new CornerRadius(8);
            window.UpdateLayout();
            Assert.Equal(new CornerRadius(8), indicator.CornerRadius);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
            bar.IsOpen = false;
        }
        finally
        {
            window.Close();
        }
        Assert.Equal(0, clock.PendingFrameCount);

        static MaterialSymbol Symbol() => new() { Symbol = MaterialSymbolCodepoint.PushPin };
    }

    public static IEnumerable<object[]> RippleCases() =>
        from theme in new[] { "MaterialCommandBarStackedTheme", "MaterialCommandBarInlineTheme", "MaterialCommandBarIconTheme" }
        from toggle in new[] { false, true }
        from dark in new[] { false, true }
        select new object[] { theme, toggle, dark };

    [AvaloniaTheory]
    [MemberData(nameof(RippleCases))]
    public void MousePress_PaintsVisibleRippleWithinCapsule(string themeKey, bool toggle, bool dark)
    {
        using var clock = new MotionTestClockScope();
        Button button = toggle
            ? new CommandBarToggleButton { Label = "Pin", Icon = new MaterialSymbol { Symbol = MaterialSymbolCodepoint.PushPin }, IsChecked = true }
            : new CommandBarButton { Label = "Copy", Icon = new MaterialSymbol { Symbol = MaterialSymbolCodepoint.ContentCopy } };
        var bar = new CommandBar { PrimaryCommands = [(ICommandBarElement)button] };
        var window = Show(bar, themeKey, dark ? ThemeVariant.Dark : ThemeVariant.Light, includeFluentTheme: true);
        try
        {
            var ripple = Part<MaterialSurface>(button, "PART_IndicatorRipple");
            var point = button.TranslatePoint(new Rect(button.Bounds.Size).Center, window)!.Value;
            window.MouseMove(point);
            button.Focus(NavigationMethod.Pointer);
            window.UpdateLayout();
            using var quiet = window.CaptureRenderedFrame()!;
            var before = Pixels(quiet);
            var rippleBounds = new Rect(ripple.TranslatePoint(default, window)!.Value, ripple.Bounds.Size);

            window.MouseDown(point, MouseButton.Left);
            Assert.True(button.IsPressed);
            var interaction = MaterialInteraction.GetOrCreate(button);
            Assert.Equal(1, interaction.RippleCount);
            for (var frame = 0; frame < 5; frame++)
                clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            using var pressed = window.CaptureRenderedFrame()!;
            var after = Pixels(pressed);
            var changedPixels = 0;
            for (var y = 0; y < pressed.PixelSize.Height; y++)
            for (var x = 0; x < pressed.PixelSize.Width; x++)
            {
                var offset = (y * pressed.PixelSize.Width + x) * 4;
                if (before.AsSpan(offset, 4).SequenceEqual(after.AsSpan(offset, 4)))
                    continue;
                changedPixels++;
                Assert.True(rippleBounds.Contains(new Point(x + 0.5, y + 0.5)),
                    $"Ripple changed a pixel outside its capsule allocation: ({x}, {y}), bounds={rippleBounds}.");
            }
            Assert.True(changedPixels > 10, $"Mouse press produced no visible ripple: changedPixels={changedPixels}.");
            Assert.Equal(0, interaction.StateLayerOpacity);
            window.MouseUp(point, MouseButton.Left);
            Assert.False(button.IsPressed);

            if (Environment.GetEnvironmentVariable("M3E_COMMAND_BAR_CAPTURE") is { Length: > 0 } directory)
            {
                Directory.CreateDirectory(directory);
                pressed.Save(Path.Combine(directory, $"{themeKey}-{(dark ? "Dark" : "Light")}-{(toggle ? "toggle" : "command")}-pressed.png"),
                    new PngBitmapEncoderOptions());
            }
            for (var frame = 0; frame < 100; frame++)
                clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Equal(0, interaction.RippleCount);
            if (!toggle)
            {
                using var settled = window.CaptureRenderedFrame()!;
                Assert.True(before.AsSpan().SequenceEqual(Pixels(settled)),
                    "A plain command retained painted feedback after its ripple finished.");

                // A click released before the first animation frame must still paint its ripple.
                window.MouseDown(point, MouseButton.Left);
                window.MouseUp(point, MouseButton.Left);
                clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
                using var quickTap = window.CaptureRenderedFrame()!;
                Assert.Equal(1, interaction.RippleCount);
                Assert.False(before.AsSpan().SequenceEqual(Pixels(quickTap)),
                    "A quick mouse click produced no visible ripple after release.");
                for (var frame = 0; frame < 100; frame++)
                    clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
                Assert.Equal(0, interaction.RippleCount);
            }
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally
        {
            window.Close();
        }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void InlineCapsule_ReservesContentPaddingInsideItsBackground()
    {
        using var clock = new MotionTestClockScope();
        var button = new CommandBarToggleButton { Label = "Bold", Icon = new MaterialSymbol { Symbol = MaterialSymbolCodepoint.FormatBold }, IsChecked = true };
        var bar = new CommandBar { PrimaryCommands = [button] };
        var window = Show(bar, "MaterialCommandBarInlineTheme");
        try
        {
            var layout = Part<CommandBarItemLayout>(button, "CommandLayout");
            var ripple = Part<MaterialSurface>(button, "PART_IndicatorRipple");
            var icon = Part<ContentPresenter>(button, "PART_Icon");
            var label = Part<ContentPresenter>(button, "PART_Label");
            Assert.Equal(new Thickness(12, 6), layout.InlineContentPadding);
            Assert.Equal(12, icon.Bounds.Left - ripple.Bounds.Left);
            Assert.Equal(12, ripple.Bounds.Right - label.Bounds.Right);
            Assert.Equal(6, icon.Bounds.Top - ripple.Bounds.Top);
            Assert.Equal(6, ripple.Bounds.Bottom - icon.Bounds.Bottom);
            Assert.Equal(36, ripple.Bounds.Height);
            Assert.Equal(48, bar.Bounds.Height);
        }
        finally
        {
            window.Close();
        }
    }

    private static byte[] Pixels(WriteableBitmap bitmap)
    {
        using var buffer = bitmap.Lock();
        var rowLength = bitmap.PixelSize.Width * 4;
        var pixels = new byte[rowLength * bitmap.PixelSize.Height];
        for (var row = 0; row < bitmap.PixelSize.Height; row++)
            Marshal.Copy(buffer.Address + row * buffer.RowBytes, pixels, row * rowLength, rowLength);
        return pixels;
    }

    [AvaloniaTheory]
    [InlineData("MaterialCommandBarStackedTheme", false)]
    [InlineData("MaterialCommandBarStackedTheme", true)]
    [InlineData("MaterialCommandBarInlineTheme", false)]
    [InlineData("MaterialCommandBarInlineTheme", true)]
    [InlineData("MaterialCommandBarIconTheme", false)]
    [InlineData("MaterialCommandBarIconTheme", true)]
    public void ContentBrushes_FollowStateColors_InBothThemesAndAfterThemeSwitch(string themeKey, bool dark)
    {
        using var clock = new MotionTestClockScope();
        var command = new CommandBarButton { Label = "Copy", Icon = Symbol(MaterialSymbolCodepoint.ContentCopy) };
        var selected = new CommandBarToggleButton { Label = "Pin", Icon = Symbol(MaterialSymbolCodepoint.PushPin), IsChecked = true };
        var unselected = new CommandBarToggleButton { Label = "Bold", Icon = Symbol(MaterialSymbolCodepoint.FormatBold) };
        var disabled = new CommandBarButton { Label = "Delete", Icon = Symbol(MaterialSymbolCodepoint.Delete), IsEnabled = false };
        var disabledSelected = new CommandBarToggleButton { Label = "Lock", Icon = Symbol(MaterialSymbolCodepoint.Lock), IsChecked = true, IsEnabled = false };
        var overflow = new CommandBarToggleButton { Label = "Overflow", Icon = Symbol(MaterialSymbolCodepoint.Info), IsChecked = true };
        var bar = new CommandBar
        {
            IsDynamicOverflowEnabled = false,
            PrimaryCommands = [command, selected, unselected, disabled, disabledSelected],
            SecondaryCommands = [overflow]
        };
        var initialTheme = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        var window = Show(bar, themeKey, initialTheme, includeFluentTheme: true);
        try
        {
            window.Width = 640;
            window.UpdateLayout();
            AssertContentColors();
            var initialColor = Part<CommandBarItemLayout>(command, "CommandLayout").UnselectedTextColor;

            window.RequestedThemeVariant = dark ? ThemeVariant.Light : ThemeVariant.Dark;
            for (var frame = 0; frame < 100; frame++)
                clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            window.UpdateLayout();
            Assert.NotEqual(initialColor, Part<CommandBarItemLayout>(command, "CommandLayout").UnselectedTextColor);
            AssertContentColors();

            bar.IsOpen = true;
            window.UpdateLayout();
            AssertCommandColors(overflow);
            bar.IsOpen = false;
            Assert.Null(window.MotionRoot.Diagnostics.LastError);

            if (Environment.GetEnvironmentVariable("M3E_COMMAND_BAR_CAPTURE") is { Length: > 0 } directory)
            {
                Directory.CreateDirectory(directory);
                using var bitmap = window.CaptureRenderedFrame()!;
                bitmap.Save(Path.Combine(directory, $"{themeKey}-{window.ActualThemeVariant}.png"),
                    new Avalonia.Media.Imaging.PngBitmapEncoderOptions());
            }

            void AssertContentColors()
            {
                foreach (var button in bar.PrimaryCommands.OfType<Button>())
                    AssertCommandColors(button);
            }
        }
        finally
        {
            window.Close();
        }
        Assert.Equal(0, clock.PendingFrameCount);

        static MaterialSymbol Symbol(MaterialSymbolCodepoint codepoint) => new() { Symbol = codepoint };
    }

    private static void AssertCommandColors(Button button)
    {
        var layout = Part<CommandBarItemLayout>(button, "CommandLayout");
        var selected = button is CommandBarToggleButton { IsChecked: true };
        var expectedIconColor = !button.IsEffectivelyEnabled ? layout.DisabledIconColor
            : selected ? layout.SelectedIconColor : layout.UnselectedIconColor;
        var expectedTextColor = !button.IsEffectivelyEnabled ? layout.DisabledTextColor
            : selected ? layout.SelectedTextColor : layout.UnselectedTextColor;
        Assert.Equal(expectedIconColor, layout.AnimatedIconColor);
        Assert.Equal(expectedTextColor, layout.AnimatedTextColor);

        var icon = Part<ContentPresenter>(button, "PART_Icon");
        var label = Part<ContentPresenter>(button, "PART_Label");
        Assert.Equal(expectedIconColor, Assert.IsAssignableFrom<ISolidColorBrush>(icon.Foreground).Color);
        Assert.Equal(expectedTextColor, Assert.IsAssignableFrom<ISolidColorBrush>(label.Foreground).Color);
        var symbol = Assert.IsType<MaterialSymbol>(icon.Content);
        Assert.Equal(expectedIconColor, Assert.IsAssignableFrom<ISolidColorBrush>(TextElement.GetForeground(symbol)).Color);
        if (label.IsVisible)
        {
            var text = label.GetVisualDescendants().OfType<TextBlock>().Single();
            Assert.Equal(expectedTextColor, Assert.IsAssignableFrom<ISolidColorBrush>(text.Foreground).Color);
        }
    }

    [AvaloniaTheory]
    [InlineData("MaterialCommandBarStackedTheme", CommandBarDefaultLabelPosition.Bottom, 60)]
    [InlineData("MaterialCommandBarInlineTheme", CommandBarDefaultLabelPosition.Right, 48)]
    [InlineData("MaterialCommandBarIconTheme", CommandBarDefaultLabelPosition.Collapsed, 48)]
    public void Themes_KeepNativeCommandsAndSwitchToLabelsInOverflow(
        string themeKey, CommandBarDefaultLabelPosition labelPosition, double minimumHeight)
    {
        using var clock = new MotionTestClockScope();
        var primary = new CommandBarButton { Icon = new MaterialSymbol { Symbol = MaterialSymbolCodepoint.ContentCopy }, Label = "Copy" };
        var pinned = new CommandBarToggleButton { Icon = new MaterialSymbol { Symbol = MaterialSymbolCodepoint.PushPin }, Label = "Pin", IsChecked = true };
        var secondary = new CommandBarToggleButton { Icon = new Border { Width = 24, Height = 24 }, Label = "Pin", IsChecked = true };
        var bar = new CommandBar { PrimaryCommands = [primary, pinned], SecondaryCommands = [secondary] };
        var window = Show(bar, themeKey);
        try
        {
            Assert.Equal(labelPosition, primary.LabelPosition);
            Assert.Equal(labelPosition == CommandBarDefaultLabelPosition.Collapsed, primary.IsCompact);
            Assert.True(bar.Bounds.Height >= minimumHeight);
            Assert.Equal(new CornerRadius(0), bar.CornerRadius);
            Assert.Equal(360, bar.Bounds.Width);
            var moreButton = Part<CommandBarButton>(bar, "PART_OverflowButton");
            Assert.Equal(48, moreButton.Bounds.Width);
            Assert.False(Part<ContentPresenter>(moreButton, "PART_Label").IsVisible);
            Assert.Contains(primary, bar.VisiblePrimaryCommands);
            Assert.Contains(secondary, bar.OverflowItems);
            Assert.True(secondary.IsInOverflow);
            Assert.Equal(CommandBarDefaultLabelPosition.Right, secondary.LabelPosition);
            Assert.False(secondary.IsCompact);

            var layout = Part<CommandBarItemLayout>(primary, "CommandLayout");
            var label = Part<ContentPresenter>(primary, "PART_Label");
            Assert.Equal(!primary.IsCompact, label.IsVisible);
            Assert.Equal(0, layout.IndicatorOpacity);
            var originalTextColor = Assert.IsAssignableFrom<ISolidColorBrush>(label.Foreground).Color;
            var interaction = MaterialInteraction.GetOrCreate(primary);
            interaction.StartPress(new Point(20, 20));
            interaction.EndPress();
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Equal(1, interaction.RippleCount);
            Assert.Equal(0, layout.IndicatorOpacity);
            Assert.Equal(originalTextColor, Assert.IsAssignableFrom<ISolidColorBrush>(label.Foreground).Color);

            var executions = 0;
            primary.Click += (_, _) => executions++;
            primary.Focus();
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
            window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
            Assert.Equal(1, executions);
            primary.IsEnabled = false;
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
            window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
            Assert.Equal(1, executions);
            primary.IsEnabled = true;

            // The same native toggle works in the popup; its label must remain visible even in icon mode.
            bar.IsOpen = true;
            window.UpdateLayout();
            Assert.True(Part<ContentPresenter>(secondary, "PART_Label").IsVisible);
            Assert.Equal(1, Part<CommandBarItemLayout>(secondary, "CommandLayout").IndicatorOpacity);
            bar.IsOpen = false;
            window.UpdateLayout();

            if (Environment.GetEnvironmentVariable("M3E_COMMAND_BAR_CAPTURE") is { Length: > 0 } directory)
            {
                Directory.CreateDirectory(directory);
                for (var frame = 0; frame < 100; frame++)
                    clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
                using var bitmap = window.CaptureRenderedFrame()!;
                bitmap.Save(Path.Combine(directory, $"{themeKey}.png"), new Avalonia.Media.Imaging.PngBitmapEncoderOptions());
            }
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally
        {
            window.Close();
        }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void ToggleSelection_AnimatesWithoutResizingRipple_AndRebuildsAtCurrentState()
    {
        using var clock = new MotionTestClockScope();
        var toggle = new CommandBarToggleButton { Icon = new Border { Width = 24, Height = 24 }, Label = "Pin" };
        var bar = new CommandBar { PrimaryCommands = [toggle] };
        var window = Show(bar, "MaterialCommandBarStackedTheme");
        try
        {
            var layout = Part<CommandBarItemLayout>(toggle, "CommandLayout");
            var ripple = Part<MaterialSurface>(toggle, "PART_IndicatorRipple");
            var rippleSize = ripple.Bounds.Size;
            toggle.IsChecked = true;
            for (var frame = 0; frame < 5; frame++)
            {
                clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
                window.UpdateLayout();
            }
            Assert.InRange(layout.IndicatorOpacity, 0.001f, 0.999f);
            Assert.Equal(rippleSize, ripple.Bounds.Size);
            toggle.IsChecked = false;
            for (var frame = 0; frame < 100; frame++)
                clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Equal(0, layout.IndicatorOpacity);
            Assert.Equal(0, layout.IndicatorSize);
            toggle.IsChecked = true;
            window.Content = null;
            window.Content = bar;
            window.UpdateLayout();
            Assert.Equal(1, layout.IndicatorOpacity);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally
        {
            window.Close();
        }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    private static MotionTestWindow Show(
        CommandBar bar, string themeKey, ThemeVariant? themeVariant = null, bool includeFluentTheme = false)
    {
        var window = new MotionTestWindow { Width = 360, Height = 180 };
        if (themeVariant is not null)
            window.RequestedThemeVariant = themeVariant;
        if (includeFluentTheme)
            window.Styles.Add(new FluentTheme());
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        bar.Theme = (ControlTheme)window.FindResource(themeKey)!;
        window.Content = bar;
        window.Show();
        window.UpdateLayout();
        return window;
    }

    private static T Part<T>(Control owner, string name) where T : Control =>
        owner.GetVisualDescendants().OfType<T>().Single(control => control.Name == name);
}
