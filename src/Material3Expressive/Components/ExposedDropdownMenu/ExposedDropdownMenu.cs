/* ExposedDropdownMenu.kt at docs/upstream-baseline.json.
 * Copyright 2023-2026 The Android Open Source Project; Apache-2.0. */
using Avalonia;
using Avalonia.Controls.Primitives;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

/// <summary>A baseline dropdown constrained to the exposed field's width and available height.</summary>
public class ExposedDropdownMenu : DropdownMenu
{
    public static readonly StyledProperty<bool> MatchAnchorWidthProperty =
        AvaloniaProperty.Register<ExposedDropdownMenu, bool>(nameof(MatchAnchorWidth), true);
    public bool MatchAnchorWidth { get => GetValue(MatchAnchorWidthProperty); set => SetValue(MatchAnchorWidthProperty, value); }
    protected override Type StyleKeyOverride => typeof(DropdownMenu);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MatchAnchorWidthProperty || change.Property == WidthProperty || change.Property == MinWidthProperty
            || change.Property == MaxWidthProperty || change.Property == HeightProperty || change.Property == MinHeightProperty
            || change.Property == MaxHeightProperty) UpdatePosition(true);
    }

    internal override void ConfigurePopupConstraints(Popup popup, Rect anchor, Size window, double density)
    {
        var margin = MathUtils.DipToPx(48, density);
        var windowHeight = MathUtils.DipToPx(window.Height, density);
        var top = anchor.Y * density; var bottom = anchor.Bottom * density;
        var available = top > windowHeight || bottom < 0 ? windowHeight - 2 * margin
            : MathUtils.RoundToInt(Math.Max(top - margin, windowHeight - margin - bottom));
        // exposedDropdownSize changes maxHeight within the constraints already supplied by
        // the caller. An explicit height/minHeight can therefore exceed the available gap.
        var maxHeight = Math.Min(MaxHeight, window.Height);
        var minHeight = Math.Min(MinHeight, maxHeight);
        popup.MaxHeight = Math.Clamp(double.IsNaN(Height) ? Math.Max(available, 0) / density : Height, minHeight, maxHeight);
        var maxWidth = Math.Min(MaxWidth, window.Width);
        var minWidth = Math.Min(MinWidth, maxWidth);
        popup.Width = MatchAnchorWidth ? Math.Clamp(double.IsNaN(Width) ? anchor.Width : Width, minWidth, maxWidth) : double.NaN;
    }
    internal override IDropdownMenuPositionProvider CreatePositionProvider(double density) => new ExposedDropdownMenuPositionProvider(density);
}

/// <summary>Exposed menu candidate order and transform origin, in physical window pixels.</summary>
public sealed class ExposedDropdownMenuPositionProvider(double density = 1) : IDropdownMenuPositionProvider
{
    public RelativePoint TransformOrigin { get; private set; } = RelativePoint.Center;

    public PixelPoint CalculatePosition(PixelRect anchorBounds, PixelSize windowSize, bool rightToLeft, PixelSize menuSize)
    {
        var xCandidates = new[] { rightToLeft ? anchorBounds.Right - menuSize.Width : anchorBounds.X,
            rightToLeft ? anchorBounds.X : anchorBounds.Right - menuSize.Width,
            anchorBounds.X + anchorBounds.Width / 2 < windowSize.Width / 2 ? 0 : windowSize.Width - menuSize.Width };
        var x = xCandidates.FirstOrDefault(x => x >= 0 && x + menuSize.Width <= windowSize.Width, xCandidates[^1]);
        var yCandidates = new[] { anchorBounds.Bottom, anchorBounds.Y - menuSize.Height,
            anchorBounds.Y + anchorBounds.Height / 2 < windowSize.Height / 2 ? 0 : windowSize.Height - menuSize.Height };
        var y = 0;
        for (var i = 0; i < yCandidates.Length; i++)
        {
            y = yCandidates[i];
            if (i == yCandidates.Length - 1)
            {
                var margin = MathUtils.DipToPx(48, density);
                y = menuSize.Height >= windowSize.Height - 2 * margin
                    ? MathUtils.RoundToInt((windowSize.Height - menuSize.Height) / 2f)
                    : Math.Clamp(y, margin, windowSize.Height - margin - menuSize.Height);
            }
            if (i == yCandidates.Length - 1 || y >= 0 && y + menuSize.Height <= windowSize.Height) break;
        }
        TransformOrigin = DropdownMenuPositionProvider.CalculateTransformOrigin(anchorBounds, new PixelRect(x, y, menuSize.Width, menuSize.Height));
        return new PixelPoint(x, y);
    }
}
