using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Data;

namespace Material3Expressive.Foundation.Typography;

/// <summary>
/// Applies an aggregate <see cref="TextStyle" /> to Avalonia's native text properties.
/// </summary>
/// <remarks>
/// Every specified field is written to the matching inherited native property (TextElement.FontFamily,
/// FontSize, FontWeight, FontStyle, FontStretch, LetterSpacing, FontFeatures, Foreground and
/// TextBlock.LineHeight, TextAlignment) on the element itself. Avalonia's property inheritance then
/// carries the values to everything below it, including content a ContentPresenter hosts for its owner
/// (a presenter is its child's inheritance parent). Unspecified fields write nothing, so the inherited
/// value shows through: a per-field merge with the ancestors, as Compose's ProvideTextStyle does.
/// The values are Style-priority bindings on the element itself, so nothing is restyled: local values,
/// template values and style triggers win over them, and they win over control themes. Among plain
/// styles on the same element, styles applied after the projection was created win (see INTERNALS).
/// </remarks>
public sealed class MaterialText : AvaloniaObject
{
    private MaterialText() { }

    public static readonly AttachedProperty<TextStyle?> StyleProperty =
        AvaloniaProperty.RegisterAttached<MaterialText, StyledElement, TextStyle?>("Style");

    /// <summary>Former provider property; now merged beneath <see cref="StyleProperty" /> with the same behaviour.</summary>
    [Obsolete("Use MaterialText.Style. Both now merge with inherited text properties.")]
    public static readonly AttachedProperty<TextStyle?> DefaultStyleProperty =
        AvaloniaProperty.RegisterAttached<MaterialText, StyledElement, TextStyle?>("DefaultStyle");

    private static readonly AttachedProperty<Projection?> ProjectionProperty =
        AvaloniaProperty.RegisterAttached<MaterialText, StyledElement, Projection?>("Projection");

#pragma warning disable CS0618
    static MaterialText()
    {
        StyleProperty.Changed.AddClassHandler<StyledElement>((owner, _) => Project(owner));
        DefaultStyleProperty.Changed.AddClassHandler<StyledElement>((owner, _) => Project(owner));
    }

    public static TextStyle? GetStyle(StyledElement element) => element.GetValue(StyleProperty);
    public static void SetStyle(StyledElement element, TextStyle? value) => element.SetValue(StyleProperty, value);
    [Obsolete("Use MaterialText.GetStyle.")]
    public static TextStyle? GetDefaultStyle(StyledElement element) => element.GetValue(DefaultStyleProperty);
    [Obsolete("Use MaterialText.SetStyle.")]
    public static void SetDefaultStyle(StyledElement element, TextStyle? value) => element.SetValue(DefaultStyleProperty, value);

    private static void Project(StyledElement owner)
    {
        var provided = owner.GetValue(DefaultStyleProperty);
        var selected = owner.GetValue(StyleProperty);
        var style = provided is null ? selected : provided.Merge(selected);
#pragma warning restore CS0618

        var projection = owner.GetValue(ProjectionProperty);
        if (projection is null)
        {
            if (style is null)
                return;
            projection = new Projection(owner);
            owner.SetValue(ProjectionProperty, projection);
        }
        projection.Publish(style);
    }

    // One Style-priority binding per specified native property. A value update flows through the
    // existing binding, so it keeps its place among the element's style values: a style applied to
    // the element after the binding (an application or class style) keeps winning across updates.
    // An unspecified field has no binding at all (a binding that yields no value would reset the
    // property to its default instead of exposing the inherited value).
    private sealed class Projection(StyledElement owner)
    {
        private readonly Value[] _values =
        [
            new(owner, TextElement.FontFamilyProperty, s => s.FontFamily),
            new(owner, TextElement.FontSizeProperty, s => s.FontSize),
            new(owner, TextElement.FontWeightProperty, s => s.FontWeight),
            new(owner, TextElement.FontStyleProperty, s => s.FontStyle),
            new(owner, TextElement.FontStretchProperty, s => s.FontStretch),
            new(owner, TextElement.LetterSpacingProperty, s => s.LetterSpacing),
            new(owner, TextElement.FontFeaturesProperty, s => s.FontFeatures),
            new(owner, TextBlock.LineHeightProperty, s => s.LineHeight),
            new(owner, TextBlock.TextAlignmentProperty, s => s.TextAlignment),
            new(owner, TextElement.ForegroundProperty, s => s.Foreground),
        ];

        public void Publish(TextStyle? style)
        {
            foreach (var value in _values)
                value.Publish(style is null ? null : value.Read(style));
        }
    }

    private sealed class Value(StyledElement owner, AvaloniaProperty property, Func<TextStyle, object?> read) : IObservable<object?>
    {
        private IObserver<object?>? _observer;
        private IDisposable? _binding;
        private object? _current;
        public object? Read(TextStyle style) => read(style);

        public void Publish(object? value)
        {
            if (value is null)
            {
                _binding?.Dispose();
                _binding = null;
                _current = null;
                return;
            }
            if (Equals(value, _current) && _binding is not null)
                return;
            _current = value;
            if (_binding is null)
                _binding = owner.Bind(property, this, BindingPriority.Style);
            else
                _observer?.OnNext(value);
        }

        public IDisposable Subscribe(IObserver<object?> observer)
        {
            _observer = observer;
            observer.OnNext(_current);
            return new Unsubscriber(this, observer);
        }

        private sealed class Unsubscriber(Value owner, IObserver<object?> observer) : IDisposable
        {
            public void Dispose()
            {
                if (ReferenceEquals(owner._observer, observer))
                    owner._observer = null;
            }
        }
    }
}
