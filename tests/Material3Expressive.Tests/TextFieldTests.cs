using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Styles;
using Xunit;

namespace Material3Expressive.Tests;

public class TextFieldTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void DisabledErrorPrecedenceAndAccessibilityPreserveApplicationOverrides(bool outlined)
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box, outlined);
        MaterialTextField.SetSupportingText(box, "Help");
        Assert.Equal("Label", AutomationProperties.GetName(box));
        Assert.Equal("Help", AutomationProperties.GetHelpText(box));
        MaterialTextField.SetIsError(box, true);
        MaterialTextField.SetErrorMessage(box, "Required");
        Assert.Equal("Required", AutomationProperties.GetHelpText(box));
        AutomationProperties.SetName(box, "Application label");
        AutomationProperties.SetHelpText(box, "Application help");
        MaterialTextField.SetLabel(box, "Other label");
        MaterialTextField.SetIsError(box, false);
        Assert.Equal("Application label", AutomationProperties.GetName(box));
        Assert.Equal("Application help", AutomationProperties.GetHelpText(box));
        MaterialTextField.SetIsError(box, true);
        box.IsEnabled = false;
        Settle(clock, window);
        Assert.InRange(((ISolidColorBrush)box.BorderBrush!).Color.A, outlined ? (byte)30 : (byte)96, outlined ? (byte)31 : (byte)98);
        Assert.Equal(outlined ? 0 : 255, ((ISolidColorBrush)box.Background!).Color.A);
        Assert.Equal(1, Part<TextFieldLayout>(box, "PART_Decoration").IndicatorThickness);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void CustomPaddingSlotsAndAboveLabelUseTemplateLayout()
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box);
        MaterialTextField.SetLabelPosition(box, TextFieldLabelPosition.Above);
        MaterialTextField.SetLabel(box, new Border { Width = 40, Height = 12, Background = Brushes.Red });
        MaterialTextField.SetPlaceholder(box, new Border { Width = 32, Height = 12, Background = Brushes.Blue });
        MaterialTextField.SetSupportingText(box, new Border { Width = 60, Height = 12, Background = Brushes.Green });
        box.Padding = new Thickness(20, 12);
        window.UpdateLayout(); Settle(clock, window);
        var layout = Part<TextFieldLayout>(box, "PART_Decoration");
        Assert.Equal(1, layout.LabelProgress);
        Assert.Equal(16, layout.ContainerBounds.Y);
        Assert.Equal(20, Part<Border>(box, "Editor").Padding.Left);
        Assert.True(Part<ContentPresenter>(box, "Supporting").Bounds.Top >= layout.ContainerBounds.Bottom);
        Assert.True(Part<ContentPresenter>(box, "Placeholder").IsVisible);
        MaterialTextField.SetLabelPosition(box, TextFieldLabelPosition.Inside);
        MaterialTextField.SetAlwaysMinimizeLabel(box, false);
        Settle(clock, window);
        Assert.False(Part<ContentPresenter>(box, "Placeholder").IsVisible);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void EmptyBlurKeepsAutoHeightWhileLabelPositionOvershoots(bool outlined, bool affixes)
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box, outlined);
        try
        {
            MaterialTextField.SetLabel(box, "姓名");
            MaterialTextField.SetSupportingText(box, "辅助文字");
            if (affixes)
            {
                MaterialTextField.SetPrefix(box, "¥");
                MaterialTextField.SetSuffix(box, "CNY");
            }
            var panel = (StackPanel)window.Content!;
            var following = new Border { Height = 8 };
            panel.Children.Add(following);
            Settle(clock, window);
            var height = box.Bounds.Height;
            var followingY = following.Bounds.Y;
            var restingLabelY = Part<ContentPresenter>(box, "Label").Bounds.Y;
            box.Focus(); Settle(clock, window);
            panel.Children[0].Focus(); window.UpdateLayout();
            var layout = Part<TextFieldLayout>(box, "PART_Decoration");
            var label = Part<ContentPresenter>(box, "Label");
            var overshot = false;
            var positionOvershot = false;
            for (var i = 0; i < 70; i++)
            {
                clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout();
                overshot |= layout.LabelProgress < 0;
                positionOvershot |= layout.LabelProgress < 0 && label.Bounds.Y > restingLabelY;
                Assert.Equal(height, box.Bounds.Height);
                Assert.Equal(followingY, following.Bounds.Y);
                Assert.InRange(label.FontSize, 12, 16);
            }
            Assert.True(overshot, "The spatial spring must retain its overshoot.");
            Assert.True(positionOvershot, "Label placement must still use the raw spring progress.");
            Assert.Equal(0, layout.LabelProgress);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false, "long-label")]
    [InlineData(true, "long-label")]
    [InlineData(false, "multiline")]
    [InlineData(true, "multiline")]
    [InlineData(false, "typography")]
    [InlineData(true, "typography")]
    public void BlurRefocusPreservesContentDrivenSizeAndTypographyBounds(bool outlined, string scenario)
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box, outlined);
        try
        {
            var layout = Part<TextFieldLayout>(box, "PART_Decoration");
            if (scenario == "long-label")
                MaterialTextField.SetLabel(box, new TextBlock
                {
                    Text = "A long label that wraps across multiple lines in a narrow field",
                    TextWrapping = TextWrapping.Wrap, MaxWidth = 140,
                });
            if (scenario == "multiline") { box.AcceptsReturn = true; box.MinLines = 3; }
            if (scenario == "typography")
            {
                layout.LargeTextStyle = layout.LargeTextStyle! with { FontSize = 20, LineHeight = 30 };
                layout.SmallTextStyle = layout.SmallTextStyle! with { FontSize = 14, LineHeight = 20 };
            }
            Settle(clock, window);
            var restingHeight = box.Bounds.Height;
            box.Focus(); Settle(clock, window);
            ((StackPanel)window.Content!).Children[0].Focus(); window.UpdateLayout();
            var label = Part<ContentPresenter>(box, "Label");
            var sawOvershoot = false;
            for (var i = 0; i < 70; i++)
            {
                clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout();
                Assert.InRange(label.FontSize, scenario == "typography" ? 14 : 12, scenario == "typography" ? 20 : 16);
                if (layout.LabelProgress < 0)
                {
                    sawOvershoot = true;
                    Assert.True(box.Bounds.Height <= restingHeight,
                        $"Overshoot expanded {scenario} from {restingHeight} to {box.Bounds.Height}");
                    break;
                }
            }
            Assert.True(sawOvershoot);
            // Reverse while velocity is nonzero, then verify both endpoint sizes remain reachable.
            box.Focus(); Settle(clock, window);
            Assert.Equal(1, layout.LabelProgress);
            ((StackPanel)window.Content!).Children[0].Focus(); Settle(clock, window);
            Assert.Equal(restingHeight, box.Bounds.Height);
            if (scenario == "multiline")
            {
                box.MinLines = 5; window.UpdateLayout();
                Assert.True(box.Bounds.Height > restingHeight, "Real content sizing must remain responsive.");
            }
        }
        finally { window.Close(); }
    }

    private static MotionTestWindow Show(out TextBox box, bool outlined = false)
    {
        var window = new MotionTestWindow { Width = 420, Height = 220, RequestedThemeVariant = ThemeVariant.Light };
        window.Styles.Add(new FluentTheme());
        window.Styles.Add(new MaterialTheme());

        box = new TextBox { Theme = (ControlTheme)window.FindResource(outlined ? "MaterialOutlinedTextFieldTheme" : "MaterialFilledTextFieldTheme")!,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
        MaterialTextField.SetLabel(box, "Label");
        box.PlaceholderText = "Placeholder";
        // A separate focus target lets us start with an unfocused editor.
        window.Content = new StackPanel { Margin = new Thickness(24), Spacing = 16,
            Children = { new Button { Content = "Focus target" }, box } };
        window.Show();
        return window;
    }

    private static T Part<T>(TextBox box, string name) where T : Control =>
        box.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);
    private static void Settle(MotionTestClockScope clock, Window window)
    {
        for (var i = 0; i < 70; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); }
    }

    [AvaloniaFact]
    public void MinimizedStringLabelUsesBodySmallLineHeight()
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box);
        box.Text = "Value";
        Settle(clock, window);
        var label = Part<ContentPresenter>(box, "Label");
        Assert.True(label.DesiredSize.Height == 16,
            $"Label height {label.DesiredSize.Height}; child {label.Child?.DesiredSize}; lineHeight {label.Child?.GetValue(TextBlock.LineHeightProperty)}; fontSize {label.FontSize}");
        window.Close();
    }

    [AvaloniaFact]
    public void DisableKeepsContinuousDecorationAndDynamicSlotsTrackNativeContent()
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box);
        box.Focus(); Settle(clock, window);
        MaterialTextField.SetPrefix(box, "$ ");
        MaterialTextField.SetSuffix(box, "USD");
        box.InnerLeftContent = new Border { Width = 24, Height = 24 };
        window.UpdateLayout();
        Assert.True(Part<ContentPresenter>(box, "Prefix").IsVisible);
        Assert.True(Part<ContentPresenter>(box, "Suffix").IsVisible);
        Assert.True(Part<ContentPresenter>(box, "Leading").IsVisible);
        MaterialTextField.SetPrefix(box, null);
        window.UpdateLayout();
        Assert.False(Part<ContentPresenter>(box, "Prefix").IsVisible);
        var layout = Part<TextFieldLayout>(box, "PART_Decoration");
        box.IsEnabled = false;
        window.UpdateLayout();
        Assert.Equal(1, layout.LabelProgress); // Maintainer chose continuous AndroidX decoration.
        for (var i = 0; i < 4; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); }
        Assert.InRange(layout.LabelProgress, 0.01f, 0.99f);
        Settle(clock, window);
        Assert.Equal(0, layout.LabelProgress);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeEditor_InputSelectionUndoClipboardAndAutomationRemainAvailable(bool outlined)
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box, outlined);
        Assert.IsType<TextBox>(box);
        box.Focus();
        window.KeyTextInput("hello");
        Assert.Equal("hello", box.Text);
        box.SelectAll();
        window.KeyTextInput("你好");
        Assert.Equal("你好", box.Text);
        box.Undo(); Assert.Equal("hello", box.Text);
        box.Redo(); Assert.Equal("你好", box.Text);
        box.SelectAll();
        box.Copy(); Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        box.Cut(); Avalonia.Threading.Dispatcher.UIThread.RunJobs(); Assert.Equal("", box.Text);
        box.Paste(); Avalonia.Threading.Dispatcher.UIThread.RunJobs(); Assert.Equal("你好", box.Text);
        var peer = ControlAutomationPeer.CreatePeerForElement(box)!;
        Assert.Equal(AutomationControlType.Edit, peer.GetAutomationControlType());
        Assert.IsAssignableFrom<IValueProvider>(peer);
        box.IsReadOnly = true;
        window.KeyTextInput("ignored"); Assert.Equal("你好", box.Text);
        box.IsReadOnly = false;
        box.IsEnabled = false;
        window.KeyTextInput("ignored"); Assert.Equal("你好", box.Text);
    }

    [AvaloniaFact]
    public void NativeImeClientRemainsConnectedToTemplatePresenter()
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box);
        box.Focus();
        var request = new TextInputMethodClientRequestedEventArgs { RoutedEvent = InputElement.TextInputMethodClientRequestedEvent };
        box.RaiseEvent(request);
        Assert.NotNull(request.Client);
        request.Client.SetPreeditText("ni", 2);
        Assert.Equal("ni", Part<TextPresenter>(box, "PART_TextPresenter").PreeditText);
        Assert.True(string.IsNullOrEmpty(box.Text));
        Assert.Equal(0, Part<ContentPresenter>(box, "Placeholder").Opacity);
        request.Client.SetPreeditText(null, null);
        window.KeyTextInput("你"); Assert.Equal("你", box.Text);
        // This verifies Avalonia's IME client contract, not a Windows IME candidate window.
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void DecorationAnimatesAndThemeReplacementPreservesTextButLosesNativeUndoHistory(bool outlined)
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box, outlined);
        Settle(clock, window);
        Assert.Equal(16, Part<ContentPresenter>(box, "Label").FontSize);
        Assert.Equal(56, Part<TextFieldLayout>(box, "PART_Decoration").ContainerBounds.Height);
        box.Focus();
        for (var i = 0; i < 5; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); }
        Assert.NotEqual(16, Part<ContentPresenter>(box, "Label").FontSize);
        Settle(clock, window);
        Assert.Equal(12, Part<ContentPresenter>(box, "Label").FontSize);
        Assert.Equal(2, Part<TextFieldLayout>(box, "PART_Decoration").IndicatorThickness);
        window.KeyTextInput("retained");
        Assert.True(box.CanUndo, "Undo available before template switch");
        box.SelectionStart = 1; box.SelectionEnd = 4;
        var theme = window.FindResource(outlined ? "MaterialFilledTextFieldTheme" : "MaterialOutlinedTextFieldTheme");
        box.Theme = (ControlTheme)theme!;
        window.UpdateLayout();
        Assert.Equal("retained", box.Text);
        Assert.Equal(1, box.SelectionStart); Assert.Equal(4, box.SelectionEnd);
        Assert.False(box.CanUndo); // Also reproduced below with two plain Fluent-based themes.
        window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void PlainNativeTextBoxAlsoLosesUndoHistoryWhenReplacingTheme()
    {
        using var clock = new MotionTestClockScope();
        var window = new MotionTestWindow { Width = 400, Height = 160 };
        window.Styles.Add(new FluentTheme());
        var native = (ControlTheme)window.FindResource(typeof(TextBox))!;
        var box = new TextBox { Theme = new ControlTheme(typeof(TextBox)) { BasedOn = native } };
        window.Content = box; window.Show(); box.Focus(); window.KeyTextInput("native");
        Assert.True(box.CanUndo);
        box.Theme = new ControlTheme(typeof(TextBox)) { BasedOn = native };
        window.UpdateLayout();
        Assert.Equal("native", box.Text);
        Assert.False(box.CanUndo);
    }

    [AvaloniaFact]
    public void OutlineCutsActualHoleAndMultilineKeepsNativeScrolling()
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box, true);
        box.Focus(); Settle(clock, window);
        var stroke = Part<TextFieldLayout>(box, "PART_Decoration");
        Assert.True(stroke.CutoutBounds.Width > 0);
        Assert.True(stroke.CutoutBounds.Contains(new Point(20, stroke.ContainerBounds.Y + 1)));
        Assert.False(stroke.CutoutBounds.Contains(new Point(200, stroke.ContainerBounds.Y + 1)));
        box.AcceptsReturn = true; box.TextWrapping = TextWrapping.Wrap; box.MinLines = 2; box.MaxLines = 3;
        window.KeyTextInput("a\nb\nc\nd\ne\nf"); window.UpdateLayout();
        var scroll = Part<ScrollViewer>(box, "PART_ScrollViewer");
        Assert.True(scroll.Extent.Height > scroll.Viewport.Height);
        Save(window, "outlined-multiline.png");
    }

    [AvaloniaFact]
    public void SupportingTextInsideTemplateParticipatesInNativeEditorHitTesting()
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box);
        MaterialTextField.SetSupportingText(box, "Supporting content");
        box.Text = "abcdef"; window.UpdateLayout();
        var helper = Part<ContentPresenter>(box, "Supporting");
        var point = helper.TranslatePoint(new Point(helper.Padding.Left + 12, helper.Padding.Top + 8), window)!.Value;
        window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left);
        Assert.True(box.IsFocused);
        // Evidence for the owner boundary: helper text becomes part of TextBox's input surface.
        Assert.NotEqual(0, box.CaretIndex);
        Settle(clock, window);
        Save(window, "filled-supporting.png");
    }

    private static void Save(Window window, string name)
    {
        var directory = Path.GetFullPath("../../../../../artifacts/text-field", AppContext.BaseDirectory);
        Directory.CreateDirectory(directory);
        using var bitmap = window.CaptureRenderedFrame();
        Assert.NotNull(bitmap);
        bitmap.Save(Path.Combine(directory, name), PngBitmapEncoderOptions.Default);
    }

    [AvaloniaFact]
    public void FocusReversalThemeColorsAndDetachKeepNativeEditingState()
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box);
        box.Focus(); window.KeyTextInput("abc");
        box.SelectAll(); window.KeyTextInput("xyz");
        Assert.True(box.CanUndo);
        window.RequestedThemeVariant = ThemeVariant.Dark;
        Settle(clock, window);
        Assert.True(box.CanUndo);
        box.Undo(); Assert.Equal("abc", box.Text);
        var parent = (StackPanel)window.Content!;
        parent.Children.Remove(box);
        parent.Children.Add(box);
        box.Focus(); Settle(clock, window);
        window.KeyTextInput("!"); Assert.Contains("!", box.Text);
        box.Clear();
        parent.Children[0].Focus();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        box.Focus();
        Settle(clock, window);
        Assert.Equal(12, Part<ContentPresenter>(box, "Label").FontSize);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void CaptureRepresentativeStatesAndTrailingButtonInteraction()
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var first);
        window.Width = 760; window.Height = 580;
        var stack = (StackPanel)window.Content!;
        stack.Children.Remove(first);

        var grid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("*,*"),
            RowDefinitions = RowDefinitions.Parse("Auto,Auto,Auto,Auto,Auto"), ColumnSpacing = 24, RowSpacing = 24 };
        TextBox? search = null; Button? clear = null;
        for (var row = 0; row < 5; row++)
        for (var column = 0; column < 2; column++)
        {
            var theme = window.FindResource(column == 0 ? "MaterialFilledTextFieldTheme" : "MaterialOutlinedTextFieldTheme");
            var box = new TextBox { Theme = (ControlTheme)theme!, PlaceholderText = "Placeholder" };
            MaterialTextField.SetLabel(box, row switch { 0 => "Empty", 1 => "Amount", 2 => "Error", 3 => "Disabled", _ => "Search slot" });
            if (row is 1 or 2 or 3) box.Text = "Text";
            if (row == 1) { MaterialTextField.SetPrefix(box, "$ "); MaterialTextField.SetSuffix(box, "USD"); }
            if (row == 2) { MaterialTextField.SetIsError(box, true); MaterialTextField.SetSupportingText(box, "Check this value"); }
            if (row == 3) box.IsEnabled = false;
            if (row == 4)
            {
                box.InnerLeftContent = new TextBlock { Text = "⌕", FontSize = 24 };
                var button = new Button { Content = "×", Focusable = false, MinWidth = 32 };
                button.Click += (_, _) => box.Clear();
                box.InnerRightContent = button;
                if (column == 1) { search = box; clear = button; }
            }
            Grid.SetRow(box, row); Grid.SetColumn(box, column); grid.Children.Add(box);
        }
        stack.Children.Add(grid); window.UpdateLayout(); Settle(clock, window);
        foreach (var disabled in grid.Children.OfType<TextBox>().Where(b => !b.IsEnabled))
            Assert.InRange(Assert.IsAssignableFrom<ISolidColorBrush>(disabled.Foreground).Color.A, (byte)96, (byte)98);
        Assert.InRange(Assert.IsAssignableFrom<ISolidColorBrush>(search!.SelectionBrush).Color.A, (byte)101, (byte)103);
        Save(window, "states-light.png");
        search!.Focus(); window.KeyTextInput("search");
        var p = clear!.TranslatePoint(new Point(clear!.Bounds.Width / 2, clear.Bounds.Height / 2), window)!.Value;
        window.MouseDown(p, MouseButton.Left); window.MouseUp(p, MouseButton.Left);
        Assert.True(string.IsNullOrEmpty(search.Text));
        Assert.True(search!.IsFocused);
        window.RequestedThemeVariant = ThemeVariant.Dark; Settle(clock, window);
        Save(window, "states-dark-focused.png");
    }
}
