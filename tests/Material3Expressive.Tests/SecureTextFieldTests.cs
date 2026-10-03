using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Styles;
using Xunit;

namespace Material3Expressive.Tests;

public class SecureTextFieldTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativePresentationAndRevealPreserveEditorSelectionAndUndo(bool outlined)
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box, outlined);
        box.Focus(); window.KeyTextInput("p@ss"); window.UpdateLayout();
        var presenter = box.GetVisualDescendants().OfType<TextPresenter>().Single();
        Assert.Equal('\u2022', box.PasswordChar);
        Assert.True(MaterialSecureTextField.GetIsSecure(box));
        Assert.Equal("••••", DisplayText(presenter));
        box.SelectAll(); box.RevealPassword = true; window.UpdateLayout();
        Assert.Equal("p@ss", DisplayText(presenter));
        Assert.Equal(0, box.SelectionStart); Assert.Equal(4, box.SelectionEnd);
        box.PasswordChar = '*'; box.RevealPassword = false; window.UpdateLayout();
        Assert.Equal("****", DisplayText(presenter));
        Assert.True(box.CanUndo);
        box.Undo(); Assert.True(string.IsNullOrEmpty(box.Text));
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DirectCallsAndPlatformShortcutsCannotExportEvenWhileVisible(bool outlined, bool reveal)
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box, outlined);
        var clipboard = window.Clipboard!;
        await clipboard.SetTextAsync("sentinel");
        box.Focus(); box.Text = "secret"; box.RevealPassword = reveal; box.SelectAll();
        box.Copy(); box.Cut();
        var keys = Application.Current!.PlatformSettings!.HotkeyConfiguration;
        foreach (var gesture in keys.Copy.Concat(keys.Cut))
        {
            var modifiers = RawInputModifiers.None;
            if (gesture.KeyModifiers.HasFlag(KeyModifiers.Control)) modifiers |= RawInputModifiers.Control;
            if (gesture.KeyModifiers.HasFlag(KeyModifiers.Shift)) modifiers |= RawInputModifiers.Shift;
            if (gesture.KeyModifiers.HasFlag(KeyModifiers.Alt)) modifiers |= RawInputModifiers.Alt;
            if (gesture.KeyModifiers.HasFlag(KeyModifiers.Meta)) modifiers |= RawInputModifiers.Meta;
            window.KeyPress(gesture.Key, modifiers, PhysicalKey.None, null);
            window.KeyRelease(gesture.Key, modifiers, PhysicalKey.None, null);
            Assert.Equal("secret", box.Text);
        }
        Assert.Equal("sentinel", await clipboard.TryGetTextAsync());
        Assert.Equal("secret", box.Text);
        Assert.Equal(0, box.SelectionStart); Assert.Equal(6, box.SelectionEnd);
        var flyout = Assert.IsType<MenuFlyout>(box.ContextFlyout);
        Assert.Equal(new[] { "Paste", "Select all" }, flyout.Items.OfType<MenuItem>().Select(item => item.Header));
        // The policy protects the base TextBox entry points, not just our context menu.
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PasteSelectionReplacementSingleLineAndUndoStayNative(bool outlined)
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box, outlined);
        box.Focus(); window.KeyTextInput("abc"); box.SelectAll();
        await window.Clipboard!.SetTextAsync("xyz\nnext");
        box.Paste();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Assert.Equal("xyz", box.Text); // TextBox retains only the first pasted line.
        box.Undo(); Assert.Equal("abc", box.Text);
        box.Redo(); Assert.Equal("xyz", box.Text);
        box.IsReadOnly = true; window.KeyTextInput("!"); box.Paste();
        Assert.Equal("xyz", box.Text);
        box.IsReadOnly = false; box.IsEnabled = false; window.KeyTextInput("!");
        Assert.Equal("xyz", box.Text);
        window.Close();
    }

    [AvaloniaFact]
    public async Task RestrictionsSurviveRevealThemeAndDetachAndReleaseToOrdinaryTextBox()
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box);
        box.AcceptsReturn = true; box.AcceptsTab = true; box.MinLines = 3; box.MaxLines = 5;
        box.TextWrapping = TextWrapping.Wrap;
        Assert.False(box.AcceptsReturn); Assert.False(box.AcceptsTab);
        Assert.Equal(1, box.MinLines); Assert.Equal(1, box.MaxLines);
        Assert.Equal(TextWrapping.NoWrap, box.TextWrapping);
        var options = TextInputOptions.FromStyledElement(box);
        Assert.Equal(TextInputContentType.Password, options.ContentType);
        Assert.True(options.IsSensitive); Assert.False(options.ShowSuggestions);
        Assert.False(options.AutoCapitalization); Assert.False(options.Multiline);
        box.Text = "secret"; box.RevealPassword = true; box.SelectAll();
        var panel = (StackPanel)window.Content!;
        panel.Children.Remove(box); panel.Children.Add(box);
        window.RequestedThemeVariant = ThemeVariant.Dark;
        await window.Clipboard!.SetTextAsync("sentinel");
        box.Copy(); box.Cut(); Assert.Equal("sentinel", await window.Clipboard!.TryGetTextAsync());
        Assert.Equal("secret", box.Text);
        // Replacing Theme removes the secure policy, while preserving the caller's local properties.
        box.Theme = (ControlTheme)window.FindResource("MaterialFilledTextFieldTheme")!;
        window.UpdateLayout();
        Assert.False(MaterialSecureTextField.GetIsSecure(box));
        Assert.True(box.AcceptsReturn); Assert.True(box.AcceptsTab);
        Assert.Equal(3, box.MinLines); Assert.Equal(5, box.MaxLines);
        Assert.Equal(TextWrapping.Wrap, box.TextWrapping);
        box.RevealPassword = true; box.SelectAll(); box.Copy();
        Assert.Equal("secret", await window.Clipboard!.TryGetTextAsync());
        window.Close();
    }

    [AvaloniaFact]
    public void AttachedRestrictionsPreserveLiveBindingsAndCanBeReactivated()
    {
        var source = new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        var box = new TextBox();
        using var acceptsReturn = box.Bind(TextBox.AcceptsReturnProperty, source.GetObservable(TextBox.AcceptsReturnProperty));
        using var wrapping = box.Bind(TextBox.TextWrappingProperty, source.GetObservable(TextBox.TextWrappingProperty));
        Assert.True(box.AcceptsReturn);
        MaterialSecureTextField.SetIsSecure(box, true);
        Assert.False(box.AcceptsReturn); Assert.Equal(TextWrapping.NoWrap, box.TextWrapping);
        source.AcceptsReturn = false; source.TextWrapping = TextWrapping.NoWrap;
        MaterialSecureTextField.SetIsSecure(box, false);
        Assert.False(box.AcceptsReturn); Assert.Equal(TextWrapping.NoWrap, box.TextWrapping);
        source.AcceptsReturn = true; source.TextWrapping = TextWrapping.Wrap;
        Assert.True(box.AcceptsReturn); Assert.Equal(TextWrapping.Wrap, box.TextWrapping);
        MaterialSecureTextField.SetIsSecure(box, true);
        Assert.False(box.AcceptsReturn);
        MaterialSecureTextField.SetIsSecure(box, false);
        Assert.True(box.AcceptsReturn);
    }

    [AvaloniaFact]
    public void NativeAutomationAndImeBoundariesRemainExplicit()
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box);
        box.Text = "secret";
        Assert.Equal("Password", AutomationProperties.GetName(box));
        var peer = ControlAutomationPeer.CreatePeerForElement(box);
        Assert.Equal("secret", Assert.IsAssignableFrom<IValueProvider>(peer).Value);
        var request = new TextInputMethodClientRequestedEventArgs { RoutedEvent = InputElement.TextInputMethodClientRequestedEvent };
        box.RaiseEvent(request);
        Assert.NotNull(request.Client);
        Assert.True(request.Client.SupportsSurroundingText);
        Assert.Equal("secret", request.Client.SurroundingText);
        // These are known gaps of the chosen plain TextBox API, not a secure-peer acceptance test.
        window.Close();
    }

    [AvaloniaFact]
    public void CaptureStatesAndNativeSupplementaryCharacterMasking()
    {
        using var clock = new MotionTestClockScope();
        var window = Show(out var box, true);
        box.Text = "a😀"; window.UpdateLayout();
        Assert.Equal("•••", DisplayText(box.GetVisualDescendants().OfType<TextPresenter>().Single()));
        var directory = Path.GetFullPath("../../../../../artifacts/secure-text-field", AppContext.BaseDirectory);
        Directory.CreateDirectory(directory);
        void Save(string name)
        {
            // Text and state changes animate the shared decoration. Capture its final layout,
            // rather than a floating label that has not yet received any animation frames.
            for (var i = 0; i < 70; i++)
            {
                clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout();
            }
            using var bitmap = window.CaptureRenderedFrame();
            Assert.NotNull(bitmap); bitmap.Save(Path.Combine(directory, name), PngBitmapEncoderOptions.Default);
        }
        Save("outlined-hidden.png"); box.RevealPassword = true; Save("outlined-visible.png");
        MaterialTextField.SetIsError(box, true); Save("outlined-error.png");
        box.IsEnabled = false; Save("outlined-disabled.png");
        window.Close();
    }

    private static string DisplayText(TextPresenter presenter) => string.Concat(presenter.TextLayout.TextLines
        .SelectMany(line => line.TextRuns).OfType<ShapedTextRun>().Select(run => run.Text.ToString()));

    private static Window Show(out TextBox box, bool outlined = false)
    {
        var window = new MotionTestWindow { Width = 420, Height = 180 };
        window.Styles.Add(new FluentTheme()); window.Styles.Add(new MaterialTheme());
        box = new TextBox { Theme = (ControlTheme)window.FindResource(outlined
            ? "MaterialOutlinedSecureTextFieldTheme" : "MaterialFilledSecureTextFieldTheme")! };
        MaterialTextField.SetLabel(box, "Password");
        window.Content = new StackPanel { Margin = new Thickness(24), Children = { box } };
        window.Show(); window.UpdateLayout(); return window;
    }
}
