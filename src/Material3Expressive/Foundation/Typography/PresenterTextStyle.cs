using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data;

namespace Material3Expressive.Foundation.Typography;

// A default text style for whatever a ContentPresenter hosts.
//
// Text styles follow the logical tree. When a presenter shows a control it did not create (a
// HeaderedContentControl's Header, or a template part bound to it), that control's logical parent
// is the owner rather than the presenter, so MaterialText.DefaultStyle on the presenter never
// reaches it; on the owner it would reach unrelated content too. This provides the style on the
// presenter's child itself, at template priority: a string's generated TextBlock, a DataTemplate's
// root and a control all pick it up, and an app's own Style or local DefaultStyle still wins.
internal static class PresenterTextStyle
{
    public static readonly AttachedProperty<TextStyle?> DefaultStyleProperty =
        AvaloniaProperty.RegisterAttached<ContentPresenter, TextStyle?>("DefaultStyle", typeof(PresenterTextStyle));

    private static readonly AttachedProperty<IDisposable?> AppliedProperty =
        AvaloniaProperty.RegisterAttached<Control, IDisposable?>("Applied", typeof(PresenterTextStyle));

    static PresenterTextStyle()
    {
        DefaultStyleProperty.Changed.AddClassHandler<ContentPresenter>((presenter, _) => Apply(presenter, presenter.Child, presenter.Child));
        ContentPresenter.ChildProperty.Changed.AddClassHandler<ContentPresenter>((presenter, e) =>
        {
            if (presenter.IsSet(DefaultStyleProperty))
                Apply(presenter, e.OldValue as Control, e.NewValue as Control);
        });
    }

    public static TextStyle? GetDefaultStyle(ContentPresenter presenter) => presenter.GetValue(DefaultStyleProperty);
    public static void SetDefaultStyle(ContentPresenter presenter, TextStyle? value) => presenter.SetValue(DefaultStyleProperty, value);

    private static void Apply(ContentPresenter presenter, Control? oldChild, Control? newChild)
    {
        if (oldChild is not null)
        {
            oldChild.GetValue(AppliedProperty)?.Dispose();
            oldChild.ClearValue(AppliedProperty);
        }
        if (newChild is not null && GetDefaultStyle(presenter) is { } style)
            newChild.SetValue(AppliedProperty, newChild.SetValue(MaterialText.DefaultStyleProperty, style, BindingPriority.Template));
    }
}
