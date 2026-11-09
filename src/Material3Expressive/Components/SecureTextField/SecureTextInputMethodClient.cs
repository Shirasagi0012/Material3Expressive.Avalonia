using Avalonia;
using Avalonia.Input.TextInput;

namespace Material3Expressive.Components;

// Preserve the native client for composition/caret coordination, but never disclose
// surrounding password content. Clients are told explicitly that it is unavailable.
internal sealed class SecureTextInputMethodClient : TextInputMethodClient, IDisposable
{
    internal TextInputMethodClient Inner { get; }
    internal SecureTextInputMethodClient(TextInputMethodClient inner)
    {
        Inner = inner;
        inner.TextViewVisualChanged += VisualChanged;
        inner.CursorRectangleChanged += CursorChanged;
        inner.SelectionChanged += SelectionUpdated;
        inner.ResetRequested += Reset;
        inner.InputPaneActivationRequested += Activate;
    }
    public override Visual TextViewVisual => Inner.TextViewVisual;
    public override bool SupportsPreedit => Inner.SupportsPreedit;
    public override bool SupportsSurroundingText => false;
    public override string SurroundingText => string.Empty;
    public override Rect CursorRectangle => Inner.CursorRectangle;
    public override TextSelection Selection { get => default; set { } }
    public override void SetPreeditText(string? text) => Inner.SetPreeditText(text);
    public override void SetPreeditText(string? text, int? cursorPos) => Inner.SetPreeditText(text, cursorPos);
    public override void ExecuteContextMenuAction(ContextMenuAction action)
    {
        if (action is not ContextMenuAction.Copy and not ContextMenuAction.Cut) Inner.ExecuteContextMenuAction(action);
    }
    private void VisualChanged(object? sender, EventArgs e) => RaiseTextViewVisualChanged();
    private void CursorChanged(object? sender, EventArgs e) => RaiseCursorRectangleChanged();
    private void SelectionUpdated(object? sender, EventArgs e) => RaiseSelectionChanged();
    private void Reset(object? sender, EventArgs e) => RequestReset();
    private void Activate(object? sender, EventArgs e) => RaiseInputPaneActivationRequested();
    public void Dispose()
    {
        Inner.TextViewVisualChanged -= VisualChanged;
        Inner.CursorRectangleChanged -= CursorChanged;
        Inner.SelectionChanged -= SelectionUpdated;
        Inner.ResetRequested -= Reset;
        Inner.InputPaneActivationRequested -= Activate;
    }
}
