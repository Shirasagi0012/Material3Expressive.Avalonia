using System;
using Avalonia;
using Avalonia.Media;
using MaterialColorUtilities.Avalonia;
using PropertyGenerator.Avalonia;

namespace Material3Expressive.Foundation;

/// <summary>
/// A whole Material theme: colour, typography, shape, motion and component token overrides.
/// </summary>
/// <remarks>
/// This is the authoring surface — mutable and bindable, so a scheme's seed colour can be a
/// <c>DynamicResource</c> and a live theme can be edited in place. Consumers never see it: every
/// change rebuilds an immutable <see cref="ResolvedTheme" />, and that is what
/// <see cref="MaterialThemeScope" /> publishes into the control tree.
/// </remarks>
public partial class MaterialTheme : AvaloniaObject
{
    /// <summary>The Material 3 baseline seed, so an undeclared theme still renders as Material.</summary>
    private static readonly Color BaselineSeedColor = Color.FromRgb(0x67, 0x50, 0xA4);

    private ResolvedTheme? _resolved;

    public MaterialTheme()
    {
        // Subscribe before assigning the defaults so that the initial values go through exactly
        // the same attach path as any later assignment.
        PropertyChanged += OnSelfPropertyChanged;

        ColorScheme = new TonalSpotScheme(BaselineSeedColor);
        TypographyScheme = new TypographyScheme();
        ShapeScheme = new ShapeScheme();
        MotionScheme = new MotionScheme();
        ButtonTokens = new ButtonTokens();
    }

    /// <summary>Raised whenever this theme or any of its schemes changes.</summary>
    public event EventHandler? Changed;

    [GeneratedStyledProperty] public partial ColorScheme? ColorScheme { get; set; }
    [GeneratedStyledProperty] public partial TypographyScheme TypographyScheme { get; set; }
    [GeneratedStyledProperty] public partial ShapeScheme ShapeScheme { get; set; }
    [GeneratedStyledProperty] public partial MotionScheme MotionScheme { get; set; }
    [GeneratedStyledProperty] public partial ButtonTokens ButtonTokens { get; set; }

    /// <summary>The current snapshot, rebuilt on demand after any change.</summary>
    public ResolvedTheme Resolved => _resolved ??= new ResolvedTheme(this);

    public object ProvideValue()
    {
        return this;
    }

    private void OnSelfPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != ColorSchemeProperty
            && e.Property != TypographySchemeProperty
            && e.Property != ShapeSchemeProperty
            && e.Property != MotionSchemeProperty
            && e.Property != ButtonTokensProperty)
            return;

        Detach(e.OldValue);
        Attach(e.NewValue);
        Invalidate();
    }

    private void Attach(object? scheme)
    {
        switch (scheme)
        {
            // A colour scheme changes for reasons a property change does not cover — a custom
            // colour added to its collection, for instance — so it has its own event.
            case ColorScheme colorScheme:
                colorScheme.SchemeChanged += OnSchemeChanged;
                break;
            case AvaloniaObject avaloniaObject:
                avaloniaObject.PropertyChanged += OnSchemePropertyChanged;
                break;
        }
    }

    private void Detach(object? scheme)
    {
        switch (scheme)
        {
            case ColorScheme colorScheme:
                colorScheme.SchemeChanged -= OnSchemeChanged;
                break;
            case AvaloniaObject avaloniaObject:
                avaloniaObject.PropertyChanged -= OnSchemePropertyChanged;
                break;
        }
    }

    private void OnSchemeChanged(object? sender, EventArgs e)
    {
        Invalidate();
    }

    private void OnSchemePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        Invalidate();
    }

    private void Invalidate()
    {
        _resolved = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
