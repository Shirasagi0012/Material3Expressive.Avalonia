// PasswordInputTransformation and SecureTextFieldController from the pinned Foundation
// BasicSecureTextField.kt. Native TextBox remains the editor and owns selection/undo.
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Logging;
using ComposeAnimation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Material3Expressive.Components;

public enum TextObfuscationMode
{
    Visible,
    RevealLastTyped,
    Hidden,
    /// <summary>The pinned desktop platform does not reveal typed characters.</summary>
    System
}

/// <summary>A native editor with Material password presentation and protected export channels.</summary>
public class SecureTextField : TextBox
{
    public static readonly StyledProperty<TextObfuscationMode> TextObfuscationModeProperty =
        AvaloniaProperty.Register<SecureTextField, TextObfuscationMode>(nameof(TextObfuscationMode), TextObfuscationMode.RevealLastTyped);

    public TextObfuscationMode TextObfuscationMode
    {
        get => GetValue(TextObfuscationModeProperty);
        set => SetValue(TextObfuscationModeProperty, value);
    }

    private SecureTextPresenter? _presenter;
    private AnimationChildScope? _revealScope;
    private CancellationTokenSource? _hideCancellation;
    private SecureTextInputMethodClient? _inputClient;
    private int _revealIndex = -1;
    internal int RevealIndex => _revealIndex;

    static SecureTextField()
    {
        PasswordCharProperty.OverrideDefaultValue<SecureTextField>('•');
        MaterialSecureTextField.IsSecureProperty.OverrideMetadata<SecureTextField>(
            new StyledPropertyMetadata<bool>(coerce: (_, _) => true));
        // Native Paste checks Handled only after the complete route. Intercept here
        // so application handlers (including ancestors) can still cancel the edit.
        PastingFromClipboardEvent.RouteFinished.Subscribe(new SecurePasteObserver());
    }

    public SecureTextField()
    {
        MaterialSecureTextField.SetIsSecure(this, true);
        AddHandler(TextInputMethodClientRequestedEvent, ProtectInputClient, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _presenter = e.NameScope.Find<SecureTextPresenter>("PART_TextPresenter");
        _presenter?.RefreshObfuscation();
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        var before = Text ?? string.Empty;
        var insertion = Math.Min(SelectionStart, SelectionEnd);
        var removed = Math.Abs(SelectionEnd - SelectionStart);
        var handled = e.Handled;
        base.OnTextInput(e);
        if (handled || IsReadOnly || (Text == before && (removed == 0 || SelectionStart != SelectionEnd))) return;
        var inserted = (Text?.Length ?? 0) - before.Length + removed;
        ApplyUserChange(inserted == 1 && SelectionStart == SelectionEnd ? insertion : -1);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var before = Text;
        var keys = Application.Current?.PlatformSettings?.HotkeyConfiguration;
        var undoOrRedo = keys is not null && keys.Undo.Concat(keys.Redo).Any(key => key.Matches(e));
        // TextLine's backspace refinement examines its safe UTF-16 placeholder text,
        // after the glyph-cluster step. Keep it from subdividing a masked surrogate
        // pair; the native deletion/undo path still performs the actual edit.
        if (e.Key == Key.Back && !e.Handled && !IsReadOnly && SelectionStart == SelectionEnd
            && string.IsNullOrEmpty(_presenter?.PreeditText) && before is not null
            && CaretIndex >= 2 && char.IsSurrogatePair(before, CaretIndex - 2))
        {
            SetCurrentValue(SelectionStartProperty, CaretIndex - 2);
        }
        base.OnKeyDown(e);
        // Deletion is an input transformation. Upstream undo/redo restores state directly;
        // navigation and selection also must not hide an otherwise live reveal.
        if (Text != before && !undoOrRedo) ApplyUserChange(-1);
    }

    private void ApplyUserChange(int insertion)
    {
        if (insertion < 0 || TextObfuscationMode != TextObfuscationMode.RevealLastTyped)
        {
            HideRevealedCharacter();
            return;
        }
        // Upstream deliberately leaves the deadline unchanged for replacement at the
        // same insertion index; it is not a debounce of every TextChanged notification.
        if (_revealIndex == insertion) return;
        CancelHide();
        _revealIndex = insertion;
        _presenter?.RefreshObfuscation();
        if (_revealScope is null)
        {
            HideRevealedCharacter();
            return;
        }
        var cancellation = _hideCancellation = new CancellationTokenSource();
        var scope = _revealScope;
        _ = scope.Launch(async _ =>
        {
            if (await scope.DelayAsync(TimeSpan.FromMilliseconds(1500), cancellation.Token)
                && ReferenceEquals(_hideCancellation, cancellation)) HideRevealedCharacter();
        }, "SecureTextField.HideLastTyped");
    }

    private void HideRevealedCharacter()
    {
        CancelHide();
        if (_revealIndex == -1) return;
        _revealIndex = -1;
        _presenter?.RefreshObfuscation();
    }

    private void CancelHide()
    {
        var cancellation = _hideCancellation;
        _hideCancellation = null;
        cancellation?.Cancel();
        cancellation?.Dispose();
    }

    private async void PasteSecure(RoutedEventArgs e)
    {
        if (e.Handled) return;
        e.Handled = true;
        if (IsReadOnly || !IsEffectivelyEnabled || TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard) return;
        string? text;
        try { text = await clipboard.TryGetTextAsync(); }
        catch (Exception error) when (error is TimeoutException or OperationCanceledException or UnauthorizedAccessException or COMException)
        {
            Logger.TryGet(LogEventLevel.Warning, "Control")?.Log(this, "Failed to read text from clipboard: {Error}", error);
            return;
        }
        if (string.IsNullOrEmpty(text) || IsReadOnly || !IsEffectivelyEnabled) return;
        // Re-enter the same native editing path, including sanitization, selection,
        // maximum length and undo grouping. A one-character paste also reveals upstream.
        SnapshotUndoRedo(this, true);
        OnTextInput(new TextInputEventArgs { Text = text, RoutedEvent = TextInputEvent });
    }

    // Avalonia 12.1.5 Paste explicitly starts an undo transaction before HandleTextInput.
    // Its public SelectedText/TextInput routes alone use the character-count grouping.
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "SnapshotUndoRedo")]
    private static extern void SnapshotUndoRedo(TextBox owner, bool ignoreChangeCount);

    // One process-lifetime subscription holds no controls or editing state.
    private sealed class SecurePasteObserver : IObserver<RoutedEventArgs>
    {
        public void OnNext(RoutedEventArgs e)
        {
            if (e.Source is SecureTextField owner) owner.PasteSecure(e);
        }
        public void OnCompleted() { }
        public void OnError(Exception error) => throw error;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextObfuscationModeProperty)
        {
            if (TextObfuscationMode is TextObfuscationMode.Hidden or TextObfuscationMode.Visible)
                HideRevealedCharacter();
            _presenter?.RefreshObfuscation();
        }
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        HideRevealedCharacter();
        base.OnLostFocus(e);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _revealScope = AnimationScope.Get(this).CreateChild("SecureTextField.Reveal");
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        HideRevealedCharacter();
        _revealScope?.Dispose();
        _revealScope = null;
        _inputClient?.Dispose();
        _inputClient = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void ProtectInputClient(object? sender, TextInputMethodClientRequestedEventArgs e)
    {
        if (e.Client is null || e.Client is SecureTextInputMethodClient) return;
        if (_inputClient?.Inner != e.Client)
        {
            _inputClient?.Dispose();
            _inputClient = new SecureTextInputMethodClient(e.Client);
        }
        e.Client = _inputClient;
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new SecureTextFieldAutomationPeer(this);

    private sealed class SecureTextFieldAutomationPeer(SecureTextField owner) : ControlAutomationPeer(owner), IValueProvider
    {
        public bool IsReadOnly => owner.IsReadOnly;
        // Password content is never an automation export channel, including Visible mode.
        public string Value => string.Empty;
        public void SetValue(string? value)
        {
            if (IsReadOnly || !owner.IsEffectivelyEnabled) return;
            owner.SelectAll();
            if (string.IsNullOrEmpty(value))
            {
                owner.SelectedText = string.Empty;
                owner.HideRevealedCharacter();
            }
            else owner.OnTextInput(new TextInputEventArgs { Text = value, RoutedEvent = TextInputEvent });
        }
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Edit;
        protected override string? GetPlaceholderTextCore() => owner.PlaceholderText;
    }
}
