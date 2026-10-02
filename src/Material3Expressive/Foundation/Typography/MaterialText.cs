using Avalonia;
using static Material3Expressive.Foundation.Typography.MaterialText;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Material3Expressive.Foundation.Typography;

/// <summary>Aggregate text styles for ordinary Avalonia text controls and content scopes.</summary>
public sealed class MaterialText : AvaloniaObject
{
    private MaterialText() { }

    public static readonly AttachedProperty<TextStyle?> StyleProperty =
        AvaloniaProperty.RegisterAttached<MaterialText, StyledElement, TextStyle?>("Style");
    public static readonly AttachedProperty<TextStyle?> DefaultStyleProperty =
        AvaloniaProperty.RegisterAttached<MaterialText, StyledElement, TextStyle?>("DefaultStyle");

    static MaterialText()
    {
        StyleProperty.Changed.AddClassHandler<StyledElement>((owner, _) => TextStyleHost.EnsureScope(owner));
        DefaultStyleProperty.Changed.AddClassHandler<StyledElement>((owner, _) => TextStyleHost.EnsureScope(owner));
    }

    public static TextStyle? GetStyle(StyledElement element) => element.GetValue(StyleProperty);
    public static void SetStyle(StyledElement element, TextStyle? value) => element.SetValue(StyleProperty, value);
    public static TextStyle? GetDefaultStyle(StyledElement element) => element.GetValue(DefaultStyleProperty);
    public static void SetDefaultStyle(StyledElement element, TextStyle? value) => element.SetValue(DefaultStyleProperty, value);
}

internal sealed class TextStyleHost : AvaloniaObject
{
    // Separate input/output properties prevent an inherited aggregate from hiding the parent's
    // value at a provider. Only actual providers publish ContextStyle; ordinary containers inherit it.
    public static readonly AttachedProperty<TextStyle?> ContextStyleProperty =
        AvaloniaProperty.RegisterAttached<TextStyleHost, StyledElement, TextStyle?>("ContextStyle", inherits: true);
    private static readonly AttachedProperty<TextStyle?> BaselineStyleProperty =
        AvaloniaProperty.RegisterAttached<TextStyleHost, StyledElement, TextStyle?>("BaselineStyle");
    private static readonly AttachedProperty<Scope?> ScopeProperty =
        AvaloniaProperty.RegisterAttached<TextStyleHost, StyledElement, Scope?>("Scope");
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<TextStyleHost, StyledElement, bool>("IsEnabled");

    static TextStyleHost() => IsEnabledProperty.Changed.AddClassHandler<StyledElement>((owner, e) =>
    {
        if (e.NewValue is true)
            EnsureScope(owner);
    });
    public static bool GetIsEnabled(StyledElement element) => element.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(StyledElement element, bool value) => element.SetValue(IsEnabledProperty, value);

    internal static void EnsureScope(StyledElement owner)
    {
        if (owner.GetValue(ScopeProperty) is not null)
            return;
        var scope = new Scope(owner);
        owner.SetValue(ScopeProperty, scope);
        scope.Initialize();
    }

    private sealed class Scope(StyledElement owner)
    {
        private StyledElement? _parent;
        private bool _updating;
        private bool _detached;
        private Style? _projection;
        private TextStyle? _projected;

        public void Initialize()
        {
            owner.PropertyChanged += OwnerChanged;
            owner.AttachedToLogicalTree += Attached;
            owner.DetachedFromLogicalTree += Detached;
            if (owner is Visual visual)
            {
                visual.AttachedToVisualTree += VisualAttached;
                visual.DetachedFromVisualTree += VisualDetached;
            }
            owner.Bind(BaselineStyleProperty, Typography.CreateBinding(TypographyRole.BodyLarge));
            Reparent();
        }

        private void Attached(object? sender, LogicalTreeAttachmentEventArgs e) { _detached = false; Reparent(); }
        private void Detached(object? sender, LogicalTreeAttachmentEventArgs e) { _detached = true; Reparent(); }
        private void VisualAttached(object? sender, VisualTreeAttachmentEventArgs e) { _detached = false; Reparent(); }
        private void VisualDetached(object? sender, VisualTreeAttachmentEventArgs e) { _detached = true; Reparent(); }

        private void Reparent()
        {
            var parent = _detached ? null : owner.Parent
                ?? (owner as Visual)?.GetVisualParent() as StyledElement
                ?? owner.TemplatedParent as StyledElement;
            if (!ReferenceEquals(parent, _parent))
            {
                if (_parent is not null)
                    _parent.PropertyChanged -= ParentChanged;
                _parent = parent;
                if (_parent is not null)
                    _parent.PropertyChanged += ParentChanged;
            }
            Update();
        }

        private void ParentChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ContextStyleProperty)
                Update();
        }

        private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == StyledElement.ParentProperty || e.Property == StyledElement.TemplatedParentProperty)
                Reparent();
            else if (e.Property == StyleProperty || e.Property == DefaultStyleProperty
                || e.Property == BaselineStyleProperty || IsNativeTypography(e.Property))
                Update();
        }

        private static bool IsNativeTypography(AvaloniaProperty property) =>
            property == TextElement.FontFamilyProperty || property == TextElement.FontSizeProperty
            || property == TextElement.FontWeightProperty || property == TextElement.FontStyleProperty
            || property == TextElement.FontStretchProperty || property == TextElement.LetterSpacingProperty
            || property == TextBlock.LineHeightProperty || property == TextBlock.TextAlignmentProperty;

        private void Update()
        {
            if (_updating)
                return;
            _updating = true;
            try
            {
                var inherited = _parent?.GetValue(ContextStyleProperty);
                var baseline = owner.GetValue(BaselineStyleProperty);
                var provided = GetDefaultStyle(owner);
                var environment = (inherited ?? baseline ?? TextStyle.Empty).Merge(provided);
                var selected = GetStyle(owner) ?? environment;

                if (owner is TextBlock or ContentPresenter or TemplatedControl && selected != _projected)
                {
                    _projected = selected;
                    var projection = new Style();
                    void Add(AvaloniaProperty property, object? value)
                    {
                        if (value is not null)
                            projection.Setters.Add(new Setter(property, value));
                    }
                    Add(TextElement.FontFamilyProperty, selected.FontFamily);
                    Add(TextElement.FontSizeProperty, selected.FontSize);
                    Add(TextElement.FontWeightProperty, selected.FontWeight);
                    Add(TextElement.FontStyleProperty, selected.FontStyle);
                    Add(TextElement.FontStretchProperty, selected.FontStretch);
                    Add(TextElement.LetterSpacingProperty, selected.LetterSpacing);
                    Add(TextBlock.LineHeightProperty, selected.LineHeight);
                    Add(TextBlock.TextAlignmentProperty, selected.TextAlignment);
                    Add(TextElement.ForegroundProperty, selected.Foreground);
                    // A selector-less style belongs only to its host. Its stable position before
                    // user styles avoids taking local values or depending on binding fallbacks.
                    if (_projection is not null)
                        owner.Styles.Remove(_projection);
                    _projection = projection;
                    owner.Styles.Insert(0, projection);
                    owner.ApplyStyling();
                }

                if (provided is null)
                {
                    owner.ClearValue(ContextStyleProperty);
                    return;
                }

                // Component providers project the chosen style onto their native properties.
                // Forward the *effective* font values so Button.FontSize, etc. still apply to
                // explicit logical content as well as template-generated labels. Foreground
                // stays separate: Material contentColor is not a replacement for TextStyle.color.
                var context = environment;
                if (owner is TemplatedControl or ContentPresenter)
                {
                    context = context with
                    {
                        FontFamily = owner.GetValue(TextElement.FontFamilyProperty),
                        FontSize = owner.GetValue(TextElement.FontSizeProperty),
                        FontWeight = owner.GetValue(TextElement.FontWeightProperty),
                        FontStyle = owner.GetValue(TextElement.FontStyleProperty),
                        FontStretch = owner.GetValue(TextElement.FontStretchProperty),
                        LetterSpacing = owner.GetValue(TextElement.LetterSpacingProperty),
                        LineHeight = owner.GetValue(TextBlock.LineHeightProperty),
                        TextAlignment = owner.GetValue(TextBlock.TextAlignmentProperty),
                    };
                }
                owner.SetValue(ContextStyleProperty, context);
            }
            finally { _updating = false; }
        }
    }
}
