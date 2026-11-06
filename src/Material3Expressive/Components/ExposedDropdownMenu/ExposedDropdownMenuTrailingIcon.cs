/* ExposedDropdownMenuDefaults.TrailingIcon / internal Icons.Filled.ArrowDropDown.
 * Fixed baseline: docs/upstream-baseline.json; Copyright The Android Open Source Project, Apache-2.0. */
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Material3Expressive.Components;

/// <summary>The default 24 DIP exposed-menu arrow. Expansion immediately rotates it by 180 degrees.</summary>
public class ExposedDropdownMenuTrailingIcon : Control
{
    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<ExposedDropdownMenuTrailingIcon, bool>(nameof(IsExpanded));
    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<ExposedDropdownMenuTrailingIcon>();
    public bool IsExpanded { get => GetValue(IsExpandedProperty); set => SetValue(IsExpandedProperty, value); }
    public IBrush? Foreground { get => GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }
    private static readonly Geometry Arrow = Geometry.Parse("M 7,10 l 5,5 5,-5 Z");

    static ExposedDropdownMenuTrailingIcon() => AffectsRender<ExposedDropdownMenuTrailingIcon>(IsExpandedProperty, ForegroundProperty);
    protected override Size MeasureOverride(Size availableSize) => new Size(24, 24).Constrain(availableSize);
    public override void Render(DrawingContext context)
    {
        var scale = Math.Min(Bounds.Width, Bounds.Height) / 24;
        var rotation = IsExpanded ? Matrix.CreateTranslation(-12, -12) * Matrix.CreateRotation(Math.PI) * Matrix.CreateTranslation(12, 12) : Matrix.Identity;
        using var transform = context.PushTransform(rotation * Matrix.CreateScale(scale, scale)
            * Matrix.CreateTranslation((Bounds.Width - 24 * scale) / 2, (Bounds.Height - 24 * scale) / 2));
        context.DrawGeometry(Foreground, null, Arrow);
    }
}
