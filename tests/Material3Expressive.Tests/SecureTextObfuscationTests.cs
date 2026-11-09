using Avalonia;
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
using Avalonia.Media.TextFormatting;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using Material3Expressive.Styles;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class SecureTextObfuscationTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void LastTypedUsesExactDeadlineAndNewIndexRestartsIt(bool outlined)
    {
        using var scene = new Scene(outlined);
        scene.Type("a"); Assert.Equal("a", scene.Display);
        scene.Advance(1000); scene.Type("b"); Assert.Equal("•b", scene.Display);
        scene.Advance(1499); Assert.Equal("•b", scene.Display);
        scene.Advance(1); Assert.Equal("••", scene.Display);
        Assert.Equal("ab", scene.Field.Text);
    }

    [AvaloniaFact]
    public void ReplacementAtSameIndexKeepsDeadlineWhileSelectionAloneKeepsReveal()
    {
        using var scene = new Scene();
        scene.Type("a"); scene.Advance(1000); scene.Field.SelectAll();
        Assert.Equal("a", scene.Display);
        scene.Type("b"); Assert.Equal("b", scene.Display);
        scene.Advance(499); Assert.Equal("b", scene.Display);
        scene.Advance(1); Assert.Equal("•", scene.Display);
        scene.Field.SelectAll(); scene.Type("b"); Assert.Equal("b", scene.Display);
        scene.Advance(1500); Assert.Equal("•", scene.Display);
    }

    [AvaloniaFact]
    public void UserEditsAndProgramWritesHaveDistinctRevealSemantics()
    {
        using var scene = new Scene();
        scene.Type("ab"); Assert.Equal("••", scene.Display);
        scene.Type("c"); Assert.Equal("••c", scene.Display);
        scene.Field.Text = "xyz"; Assert.Equal("••z", scene.Display);
        scene.Field.CaretIndex = 3;
        scene.Window.KeyPress(Key.Back, RawInputModifiers.None, PhysicalKey.None, null);
        scene.Window.KeyRelease(Key.Back, RawInputModifiers.None, PhysicalKey.None, null);
        Assert.Equal("••", scene.Display);
    }

    [AvaloniaTheory]
    [InlineData(TextObfuscationMode.Hidden, "••")]
    [InlineData(TextObfuscationMode.System, "••")]
    [InlineData(TextObfuscationMode.Visible, "ab")]
    [InlineData(TextObfuscationMode.RevealLastTyped, "•b")]
    public void ModesAreIndependentFromNativeRevealPassword(TextObfuscationMode mode, string display)
    {
        using var scene = new Scene();
        scene.Field.TextObfuscationMode = mode;
        scene.Field.RevealPassword = true;
        scene.Type("a"); scene.Type("b"); Assert.Equal(display, scene.Display);
    }

    [AvaloniaFact]
    public void FocusLossModeChangesAndDetachCancelReveal()
    {
        using var scene = new Scene();
        scene.Type("a"); scene.Other.Focus(); Assert.Equal("•", scene.Display);
        scene.Field.Focus(); scene.Type("b"); Assert.Equal("•b", scene.Display);
        scene.Field.TextObfuscationMode = TextObfuscationMode.Hidden; Assert.Equal("••", scene.Display);
        scene.Field.TextObfuscationMode = TextObfuscationMode.RevealLastTyped; Assert.Equal("••", scene.Display);
        scene.Type("c"); scene.Panel.Children.Remove(scene.Field);
        Dispatcher.UIThread.RunJobs(); Assert.Equal(0, scene.Time.ActiveTimerCount);
        scene.Panel.Children.Add(scene.Field); scene.Window.UpdateLayout(); Assert.Equal("•••", scene.Display);
    }

    [AvaloniaFact]
    public async Task SingleCharacterPasteUsesSameInputRuleAndNeverExports()
    {
        using var scene = new Scene();
        await scene.Window.Clipboard!.SetTextAsync("x"); scene.Field.Paste(); Dispatcher.UIThread.RunJobs();
        Assert.Equal("x", scene.Display);
        await scene.Window.Clipboard!.SetTextAsync("yz\nignored"); scene.Field.Paste(); Dispatcher.UIThread.RunJobs();
        Assert.Equal("•••", scene.Display); Assert.Equal("xyz", scene.Field.Text);
        scene.Field.Undo(); Assert.Equal("x", scene.Field.Text);
        scene.Field.Redo(); Assert.Equal("xyz", scene.Field.Text);
        scene.Field.TextObfuscationMode = TextObfuscationMode.Visible;
        scene.Field.SelectAll(); await scene.Window.Clipboard!.SetTextAsync("sentinel");
        scene.Field.Copy(); scene.Field.Cut();
        Assert.Equal("sentinel", await scene.Window.Clipboard!.TryGetTextAsync());
        Assert.Equal("xyz", scene.Field.Text);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PasteRemainsCancelableByControlAndAncestorHandlers(bool ancestor)
    {
        using var scene = new Scene();
        scene.Type("a");
        await scene.Window.Clipboard!.SetTextAsync("b");
        var invoked = false;
        (ancestor ? (InputElement)scene.Panel : scene.Field).AddHandler(TextBox.PastingFromClipboardEvent, (_, e) =>
        {
            invoked = true;
            e.Handled = true;
        });
        scene.Field.Paste(); Dispatcher.UIThread.RunJobs();
        Assert.True(invoked);
        Assert.Equal("a", scene.Field.Text);
        Assert.Equal("a", scene.Display);
        scene.Advance(1500); Assert.Equal("•", scene.Display);
    }

    [AvaloniaFact]
    public void AutomationAndImeNeverExportEvenInVisibleMode()
    {
        using var scene = new Scene();
        MaterialSecureTextField.SetIsSecure(scene.Field, false);
        Assert.True(MaterialSecureTextField.GetIsSecure(scene.Field));
        scene.Type("ab"); scene.Field.TextObfuscationMode = TextObfuscationMode.Visible;
        var peer = ControlAutomationPeer.CreatePeerForElement(scene.Field)!;
        var value = Assert.IsAssignableFrom<IValueProvider>(peer);
        Assert.Equal(string.Empty, value.Value);
        var changes = new List<object?>();
        peer.PropertyChanged += (_, e) => changes.Add(e.NewValue);
        scene.Field.Text = "xyz"; Assert.DoesNotContain("xyz", changes);
        var request = new TextInputMethodClientRequestedEventArgs { RoutedEvent = InputElement.TextInputMethodClientRequestedEvent };
        scene.Field.RaiseEvent(request);
        Assert.NotNull(request.Client); Assert.False(request.Client.SupportsSurroundingText);
        Assert.Equal(string.Empty, request.Client.SurroundingText);
        Assert.True(request.Client.SupportsPreedit);
        request.Client.SetPreeditText("q", 1); scene.Window.UpdateLayout();
        Assert.Contains("q", scene.Display);
        request.Client.SetPreeditText(null);
        scene.Field.TextObfuscationMode = TextObfuscationMode.Hidden;
        request.Client.SetPreeditText("q", 1); Assert.Equal("••••", scene.Display);
    }

    [AvaloniaFact]
    public void AutomationWritesAreUserEditsAndRespectReadonlyAndDisabled()
    {
        using var scene = new Scene();
        var value = Assert.IsAssignableFrom<IValueProvider>(ControlAutomationPeer.CreatePeerForElement(scene.Field));
        scene.Type("a"); value.SetValue("test"); Assert.Equal("••••", scene.Display);
        value.SetValue("z"); Assert.Equal("z", scene.Display);
        value.SetValue(string.Empty); Assert.Equal(string.Empty, scene.Display);
        scene.Field.Text = "fixed"; scene.Field.IsReadOnly = true; value.SetValue("new"); Assert.Equal("fixed", scene.Field.Text);
        scene.Field.IsReadOnly = false; scene.Field.IsEnabled = false; value.SetValue("new"); Assert.Equal("fixed", scene.Field.Text);
    }

    [AvaloniaFact]
    public void PreeditInsertionCannotRevealAnOlderCommittedSecretCharacter()
    {
        using var scene = new Scene();
        scene.Field.Text = "abc"; scene.Field.CaretIndex = 3; scene.Type("d");
        Assert.Equal("•••d", scene.Display);
        scene.Field.CaretIndex = 0;
        var request = new TextInputMethodClientRequestedEventArgs { RoutedEvent = InputElement.TextInputMethodClientRequestedEvent };
        scene.Field.RaiseEvent(request);
        request.Client!.SetPreeditText("x", 1);
        Assert.Equal("••••d", scene.Display);
        Assert.Equal("abcd", scene.Field.Text);
        Assert.Equal(string.Empty, request.Client.SurroundingText);
        request.Client.SetPreeditText("😀", 2); Assert.Equal("••••d", scene.Display);
        request.Client.SetPreeditText(null); Assert.Equal("•••d", scene.Display);
        scene.Advance(1500); Assert.Equal("••••", scene.Display);
    }

    [AvaloniaFact]
    public void SystemTransitionAndNativeUndoRedoKeepTheOriginalRevealDeadline()
    {
        using var scene = new Scene();
        scene.Field.Text = "a"; scene.Field.CaretIndex = 1; scene.Type("b");
        scene.Advance(500);
        scene.Window.KeyPress(Key.Z, RawInputModifiers.Control, PhysicalKey.None, null);
        scene.Window.KeyRelease(Key.Z, RawInputModifiers.Control, PhysicalKey.None, null);
        Assert.Equal("a", scene.Field.Text); Assert.Equal("•", scene.Display);
        scene.Advance(500);
        scene.Window.KeyPress(Key.Y, RawInputModifiers.Control, PhysicalKey.None, null);
        scene.Window.KeyRelease(Key.Y, RawInputModifiers.Control, PhysicalKey.None, null);
        Assert.Equal("ab", scene.Field.Text); Assert.Equal("•b", scene.Display);
        scene.Field.TextObfuscationMode = TextObfuscationMode.System; Assert.Equal("•b", scene.Display);
        scene.Advance(499); Assert.Equal("•b", scene.Display);
        scene.Advance(1); Assert.Equal("••", scene.Display);
        scene.Type("c"); Assert.Equal("•••", scene.Display);
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void SupplementaryMaskKeepsNativeCaretHitTestingSelectionAndDeletion(FlowDirection direction)
    {
        using var scene = new Scene();
        scene.Field.FlowDirection = direction;
        scene.Field.TextObfuscationMode = TextObfuscationMode.Hidden;
        scene.Field.Text = "A😀B";
        Assert.Equal("•••", scene.Display);
        var presenter = scene.Field.GetVisualDescendants().OfType<SecureTextPresenter>().Single();
        var line = Assert.Single(presenter.TextLayout.TextLines);
        Assert.Equal(3, line.TextRuns.OfType<ShapedTextRun>().Sum(run => run.ShapedBuffer.Length));
        var before = line.GetDistanceFromCharacterHit(new CharacterHit(1, 0));
        var after = line.GetDistanceFromCharacterHit(new CharacterHit(3, 0));
        Assert.True(Math.Abs(after - before) > 0);
        foreach (var fraction in new[] { 0.2, 0.8 })
        {
            var hit = line.GetCharacterHitFromDistance(before + (after - before) * fraction);
            Assert.Contains(hit.FirstCharacterIndex + hit.TrailingLength, new[] { 1, 3 });
        }
        var next = line.GetNextCaretCharacterHit(new CharacterHit(1, 0));
        Assert.Equal(3, next.FirstCharacterIndex + next.TrailingLength);
        scene.Field.CaretIndex = 1;
        var forward = direction == FlowDirection.LeftToRight ? Key.Right : Key.Left;
        var backward = direction == FlowDirection.LeftToRight ? Key.Left : Key.Right;
        scene.Window.KeyPress(forward, RawInputModifiers.None, PhysicalKey.None, null);
        scene.Window.KeyRelease(forward, RawInputModifiers.None, PhysicalKey.None, null);
        Assert.Equal(3, scene.Field.CaretIndex);
        scene.Window.KeyPress(backward, RawInputModifiers.None, PhysicalKey.None, null);
        scene.Window.KeyRelease(backward, RawInputModifiers.None, PhysicalKey.None, null);
        Assert.Equal(1, scene.Field.CaretIndex);
        scene.Field.SelectionStart = 1; scene.Field.SelectionEnd = 3;
        scene.Window.UpdateLayout();
        var selected = presenter.TextLayout.HitTestTextRange(1, 2).ToArray();
        Assert.Single(selected); Assert.Equal(Math.Abs(after - before), selected[0].Width, precision: 5);
        scene.Field.CaretIndex = 3;
        scene.Window.KeyPress(Key.Back, RawInputModifiers.None, PhysicalKey.None, null);
        scene.Window.KeyRelease(Key.Back, RawInputModifiers.None, PhysicalKey.None, null);
        Assert.Equal("AB", scene.Field.Text);
        scene.Field.Text = "A😀B"; scene.Field.SelectionStart = 1; scene.Field.SelectionEnd = 3;
        scene.Field.TextObfuscationMode = TextObfuscationMode.RevealLastTyped;
        scene.Type("x"); Assert.Equal("AxB", scene.Field.Text); Assert.Equal("•x•", scene.Display);
    }

    private sealed class Scene : IDisposable
    {
        public ManualTimeProvider Time { get; } = new(Dispatcher.UIThread.VerifyAccess);
        public AnimationTestWindow Window { get; }
        public SecureTextField Field { get; } = new();
        public Button Other { get; } = new() { Content = "Other" };
        public StackPanel Panel { get; } = new();
        public string Display
        {
            get
            {
                Window.UpdateLayout();
                return Field.GetVisualDescendants().OfType<SecureTextPresenter>().Single().DisplayedText;
            }
        }
        public Scene(bool outlined = false)
        {
            Window = new AnimationTestWindow(null, Time) { Width = 420, Height = 200 };
            Window.Styles.Add(new MaterialTheme());
            if (outlined) Field.Theme = (ControlTheme)Window.FindResource("MaterialOutlinedSecureTextFieldTheme")!;
            Panel.Children.Add(Field); Panel.Children.Add(Other); Window.Content = Panel;
            Window.Show(); Window.UpdateLayout(); Field.Focus();
        }
        public void Type(string value) { Window.KeyTextInput(value); Window.UpdateLayout(); }
        public void Advance(int milliseconds) { Time.AdvanceBy(TimeSpan.FromMilliseconds(milliseconds)); Dispatcher.UIThread.RunJobs(); }
        public void Dispose()
        {
            Window.Close(); Dispatcher.UIThread.RunJobs();
            Assert.Equal(0, Time.ActiveTimerCount);
            Assert.Null(Window.AnimationRoot.Diagnostics.LastError);
        }
    }
}
