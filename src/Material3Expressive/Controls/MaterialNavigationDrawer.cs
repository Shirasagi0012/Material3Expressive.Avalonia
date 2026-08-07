using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Controls;

/// <summary>
/// A navigation drawer: a panel of destinations that slides in from the side.
/// </summary>
/// <remarks>
/// The drawer is drawn over whatever it is placed on top of, so it belongs in the same container
/// as the content it covers rather than in a popup. That keeps it inside the theme scope it was
/// declared in, which a popup would also do, but it also keeps it inside the same layout — a
/// standard drawer has to share space with the content, and a popup cannot.
/// <para>
/// Opening slides the panel and fades the scrim from one spring, so a drawer dismissed mid-open
/// reverses from where it is. The scrim only exists for the modal variant, where it is also the
/// dismiss target.
/// </para>
/// </remarks>
public class MaterialNavigationDrawer : ContentControl, IMaterialThemeConsumer, IMaterialMotionConsumer
{
    public static readonly StyledProperty<DrawerVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialNavigationDrawer, DrawerVariant>(nameof(Variant));

    /// <inheritdoc cref="IsOpen" />
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<MaterialNavigationDrawer, bool>(
            nameof(IsOpen), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    private readonly MaterialElementContext _theme;
    private readonly MaterialMotionScope _motion;

    private SpringAnimation? _open;
    private DrawerTokens _tokens = DrawerTokens.Modal;
    private DrawerColors _colors = DrawerTokens.Modal.Resolve(null, false);

    public MaterialNavigationDrawer()
    {
        _theme = MaterialElementContext.GetOrCreate(this);
        _motion = MaterialElementContext.GetOrCreate(this).Motion;
        ApplyTokens();
    }

    void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state) => ApplyTokens();

    void IMaterialMotionConsumer.OnMaterialMotionFrame()
    {
        InvalidateArrange();
        InvalidateVisual();
    }

    static MaterialNavigationDrawer()
    {
        AffectsRender<MaterialNavigationDrawer>(VariantProperty, IsOpenProperty);
    }

    public DrawerVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>Whether the drawer is showing.</summary>
    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>How far open the drawer is, 0 closed and 1 open.</summary>
    protected double OpenFraction => _open?.Value ?? (IsOpen ? 1 : 0);

    private SpringAnimation Open =>
        _open ??= _motion.AddSpring(IsOpen ? 1 : 0, _theme.Spring(MotionRole.DefaultSpatial));

    public override void Render(DrawingContext context)
    {
        var fraction = OpenFraction;
        if (fraction <= 0.001)
            return;

        var width = _tokens.ContainerWidth;
        var offset = -width * (1 - fraction);

        if (_tokens.IsModal && _tokens.ScrimOpacity > 0)
        {
            var scrim = _theme.Theme.ColorScheme?.GetColor(SysColorToken.Scrim, _theme.IsDark) ?? Colors.Black;
            using (context.PushOpacity(_tokens.ScrimOpacity * fraction))
                context.FillRectangle(new SolidColorBrush(scrim), new Rect(Bounds.Size));
        }

        var panel = new Rect(offset, 0, width, Bounds.Height);
        var corners = _theme.Theme.Shapes.GetCornerRadius(_tokens.ContainerShape, panel.Size);

        using (context.PushTransform(Matrix.CreateTranslation(panel.X, panel.Y)))
        {
            MaterialContainer.Draw(
                context,
                panel.Size,
                corners,
                _colors.ContainerColor);
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // The content is the drawer's own panel, so it rides the slide.
        var width = _tokens.ContainerWidth;
        var offset = -width * (1 - OpenFraction);

        if (Presenter is { } presenter)
            presenter.Arrange(new Rect(offset, 0, width, finalSize.Height));

        return finalSize;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        // Tapping the scrim dismisses a modal drawer; a standard one has no scrim to tap.
        if (!_tokens.IsModal || !IsOpen)
            return;

        if (e.GetPosition(this).X > _tokens.ContainerWidth)
        {
            IsOpen = false;
            e.Handled = true;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == VariantProperty)
        {
            ApplyTokens();
        }
        else if (change.Property == IsOpenProperty)
        {
            Open.AnimateTo(IsOpen ? 1 : 0, _theme.Spring(MotionRole.DefaultSpatial));
            _motion.Start();
            IsHitTestVisible = IsOpen;
        }
    }

    private void ApplyTokens()
    {
        _tokens = _theme.Theme.GetDrawerTokens(Variant);
        _colors = _theme.Theme.GetDrawerColors(Variant, _theme.IsDark);

        IsHitTestVisible = IsOpen;
        InvalidateArrange();
        InvalidateVisual();
    }
}
