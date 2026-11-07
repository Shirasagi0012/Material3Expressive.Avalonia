// MenuDefaults.kt: DropdownMenuGroupLabel / DropdownMenuItemTrailingLabel.
// AndroidX f3ed195f0c9e; Copyright 2025 The Android Open Source Project; Apache-2.0.
using Avalonia;
using Avalonia.Controls;

namespace Material3Expressive.Components;

/// <summary>A non-interactive heading for a dropdown menu group.</summary>
public class DropdownMenuGroupLabel : ContentControl
{
    internal static readonly StyledProperty<double> DefaultMinWidthProperty =
        AvaloniaProperty.Register<DropdownMenuGroupLabel, double>(nameof(DefaultMinWidth));
    internal static readonly StyledProperty<double> DefaultMaxWidthProperty =
        AvaloniaProperty.Register<DropdownMenuGroupLabel, double>(nameof(DefaultMaxWidth));
    internal static readonly StyledProperty<double> DefaultMinHeightProperty =
        AvaloniaProperty.Register<DropdownMenuGroupLabel, double>(nameof(DefaultMinHeight));
    internal double DefaultMinWidth { get => GetValue(DefaultMinWidthProperty); set => SetValue(DefaultMinWidthProperty, value); }
    internal double DefaultMaxWidth { get => GetValue(DefaultMaxWidthProperty); set => SetValue(DefaultMaxWidthProperty, value); }
    internal double DefaultMinHeight { get => GetValue(DefaultMinHeightProperty); set => SetValue(DefaultMinHeightProperty, value); }

    static DropdownMenuGroupLabel() => AffectsMeasure<DropdownMenuGroupLabel>(
        DefaultMinWidthProperty, DefaultMaxWidthProperty, DefaultMinHeightProperty);

    protected override Size MeasureOverride(Size availableSize)
    {
        // sizeIn is inside the caller's modifier: explicit width/height and minima win.
        // Its minima are constrained by the parent and are not propagated to Box content.
        var width = double.IsNaN(Width) ? Math.Min(availableSize.Width, Math.Max(MinWidth, DefaultMaxWidth)) : availableSize.Width;
        var size = base.MeasureOverride(new Size(width, availableSize.Height));
        return new Size(Math.Max(size.Width, Math.Min(width, DefaultMinWidth)),
            Math.Max(size.Height, Math.Min(availableSize.Height, DefaultMinHeight)));
    }
}

/// <summary>A menu trailing label, with label typography and direction-aware precision padding.</summary>
public class DropdownMenuItemTrailingLabel : ContentControl;
