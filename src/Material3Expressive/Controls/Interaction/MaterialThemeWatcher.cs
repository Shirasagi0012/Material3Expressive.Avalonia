using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>
/// Keeps a control's view of the theme in scope up to date.
/// </summary>
/// <remarks>
/// A helper rather than a base class because most Material components map onto a stock Avalonia
/// control — a button is a <see cref="Button" />, a chip is a <c>ToggleButton</c> — and inheriting
/// from a Material base would mean reimplementing content, commands and click handling that
/// Avalonia already gets right. Hosting this instead costs one field.
/// </remarks>
public sealed class MaterialThemeWatcher
{
    private readonly Control _owner;

    private bool _isDark;

    // Null until the first resolution, so a control whose theme happens to equal the default still
    // gets its initial notification.
    private ResolvedTheme? _theme;

    public MaterialThemeWatcher(Control owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));

        _owner.AttachedToLogicalTree += OnAttached;
        _owner.PropertyChanged += OnPropertyChanged;

        // Deliberately not resolving here: a control has no ActualThemeVariant until it is in a
        // tree, and an owner's constructor has not finished running either. Consumers read
        // ResolvedTheme.Default until the first attach, which is the same contract a control
        // reading the theme itself has.
    }

    /// <summary>Raised whenever the theme or the element's theme variant changes.</summary>
    public event EventHandler? Changed;

    /// <summary>The theme in scope at the owner.</summary>
    public ResolvedTheme Theme => _theme ?? ResolvedTheme.Default;

    /// <summary>Whether the owner's theme variant resolves to dark.</summary>
    public bool IsDark => _isDark;

    /// <summary>The spring named by a motion role, from the theme in scope.</summary>
    public SpringSpec Spring(MotionRole role)
    {
        return Theme.Motion.Get(role);
    }

    private void OnAttached(object? sender, LogicalTreeAttachmentEventArgs e)
    {
        Update();
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MaterialThemeScope.ResolvedThemeProperty
            || e.Property == ThemeVariantScope.ActualThemeVariantProperty)
            Update();
    }

    private void Update()
    {
        var theme = MaterialThemeScope.GetResolvedTheme(_owner);
        var isDark = ColorScheme.IsDark(_owner.ActualThemeVariant ?? ThemeVariant.Default);

        // Snapshots are immutable and replaced wholesale, so reference equality is enough to know
        // that nothing can have changed.
        if (_theme is { } current && ReferenceEquals(theme, current) && isDark == _isDark)
            return;

        _theme = theme;
        _isDark = isDark;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
