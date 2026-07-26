using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>
/// Base class for controls that read their tokens in code rather than through bindings.
/// </summary>
/// <remarks>
/// This is the path the library is built on: a control subscribes to the theme in scope at itself
/// and re-resolves when it changes. Because both inputs — the inherited theme snapshot and the
/// element's own theme variant — are read from the control, scoping works without any of the
/// machinery a shared, parse-time-bound setter needs. Markup extensions remain available to app
/// authors, but no control here depends on them.
/// </remarks>
public abstract class MaterialControl : TemplatedControl
{
    private MotionGroup? _motion;
    private bool _isDark;

    // Null until the first resolution, so that a control whose theme happens to equal the default
    // still gets its initial OnThemeChanged call.
    private ResolvedTheme? _theme;

    /// <summary>
    /// The theme in scope at this control. Named for its type rather than "Theme", which on
    /// <see cref="Avalonia.StyledElement" /> already means the control's <c>ControlTheme</c>.
    /// </summary>
    protected ResolvedTheme ResolvedTheme => _theme ?? Foundation.ResolvedTheme.Default;

    /// <summary>Whether the control's theme variant resolves to dark.</summary>
    protected bool IsDarkVariant => _isDark;

    /// <summary>
    /// This control's animations and the frame loop that drives them. Created on first use, so a
    /// control that never animates never allocates one.
    /// </summary>
    protected MotionGroup Motion
    {
        get
        {
            if (_motion is { } existing)
                return existing;

            _motion = new MotionGroup(this);
            _motion.Ticked += (_, _) => OnMotionTick();
            return _motion;
        }
    }

    /// <summary>The spring named by a motion role, from the theme in scope.</summary>
    protected SpringSpec Spring(MotionRole role)
    {
        return ResolvedTheme.Motion.Get(role);
    }

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        UpdateTheme();
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);

        // Off the tree there is no render clock, so anything mid-flight would freeze.
        _motion?.SettleAll();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == MaterialThemeScope.ResolvedThemeProperty
            || change.Property == ThemeVariantScope.ActualThemeVariantProperty)
            UpdateTheme();
    }

    /// <summary>
    /// Called whenever the theme or the variant changes, and once on attach. Resolve token values
    /// into control state here.
    /// </summary>
    protected virtual void OnThemeChanged(ResolvedTheme theme, bool isDark)
    {
    }

    /// <summary>
    /// Called once per frame while anything in <see cref="Motion" /> is running. Invalidates the
    /// visual by default, which is what an animated token value almost always needs.
    /// </summary>
    protected virtual void OnMotionTick()
    {
        InvalidateVisual();
    }

    private void UpdateTheme()
    {
        var theme = MaterialThemeScope.GetResolvedTheme(this);
        var isDark = ColorScheme.IsDark(ActualThemeVariant);

        // Snapshots are immutable and replaced wholesale, so reference equality is enough to know
        // that nothing can have changed.
        if (_theme is { } current && ReferenceEquals(theme, current) && isDark == _isDark)
            return;

        _theme = theme;
        _isDark = isDark;
        OnThemeChanged(theme, isDark);
    }
}
