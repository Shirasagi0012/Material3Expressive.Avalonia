using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Material3Expressive.Foundation.Typography;

namespace Material3Expressive.Components;

// GroupBox header helpers.
//
// Typography: PART_HeaderPresenter is a HeaderedContentControl host, so the header's root
// element is a logical child of the GroupBox, not of the presenter, and a DefaultStyle on the
// presenter never reaches it (text styles follow the logical parent). A DefaultStyle on the
// GroupBox would reach the content too. So the header style is provided on the header's root
// element itself, at template priority: a string header's generated TextBlock, a HeaderTemplate's
// root, or a header control all pick it up, and an app's own Style/DefaultStyle still wins.
internal sealed class GroupBoxHeader : AvaloniaObject, IValueConverter
{
    public static readonly AttachedProperty<TextStyle?> TextStyleProperty =
        AvaloniaProperty.RegisterAttached<GroupBoxHeader, ContentPresenter, TextStyle?>("TextStyle");

    private static readonly AttachedProperty<IDisposable?> AppliedProperty =
        AvaloniaProperty.RegisterAttached<GroupBoxHeader, Control, IDisposable?>("Applied");

    // Header presence: a null or empty-string Header reserves no space (no row, no gap).
    public static GroupBoxHeader IsPresent { get; } = new();

    static GroupBoxHeader()
    {
        TextStyleProperty.Changed.AddClassHandler<ContentPresenter>((presenter, _) => Apply(presenter, presenter.Child, presenter.Child));
        ContentPresenter.ChildProperty.Changed.AddClassHandler<ContentPresenter>((presenter, e) =>
        {
            if (presenter.IsSet(TextStyleProperty))
                Apply(presenter, e.OldValue as Control, e.NewValue as Control);
        });
    }

    public static TextStyle? GetTextStyle(ContentPresenter presenter) => presenter.GetValue(TextStyleProperty);
    public static void SetTextStyle(ContentPresenter presenter, TextStyle? value) => presenter.SetValue(TextStyleProperty, value);

    private static void Apply(ContentPresenter presenter, Control? oldChild, Control? newChild)
    {
        if (oldChild is not null)
        {
            oldChild.GetValue(AppliedProperty)?.Dispose();
            oldChild.ClearValue(AppliedProperty);
        }
        if (newChild is not null && GetTextStyle(presenter) is { } style)
            newChild.SetValue(AppliedProperty, newChild.SetValue(MaterialText.DefaultStyleProperty, style, BindingPriority.Template));
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not null && value is not "";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
