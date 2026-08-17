using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;

namespace Material3Expressive.Controls;

/// <summary>
/// Private, owner-local composition root for Material runtime controllers.
/// </summary>
/// <remarks>
/// This attached value is intentionally non-inherited: every actual control owns at most one
/// runtime graph. It has no lifecycle or dispatch logic of its own; that logic belongs to the
/// controller whose domain it serves.
/// </remarks>
internal sealed class MaterialElementContext
{
    private static readonly AttachedProperty<MaterialElementContext?> ContextProperty =
        AvaloniaProperty.RegisterAttached<MaterialElementContext, Control, MaterialElementContext?>(
            "__MaterialElementContext",
            defaultValue: null,
            inherits: false);

    private readonly Control _owner;
    private MaterialThemeController? _theme;
    private MaterialMotionController? _motion;
    private MaterialInteractionController? _interaction;

    private MaterialElementContext(Control owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    internal Control Owner => _owner;

    [Obsolete(
        "Legacy whole-theme snapshot controller. Consume typed MaterialValueKey<T> resources.",
        DiagnosticId = "M3E1004")]
    internal MaterialThemeController Theme => _theme ?? CreateTheme();

    internal MaterialMotionController Motion => _motion ?? CreateMotion();

    internal MaterialInteractionController Interaction => _interaction ?? CreateInteraction();

    [Obsolete(
        "Legacy whole-theme snapshot controller. Consume typed MaterialValueKey<T> resources.",
        DiagnosticId = "M3E1004")]
    internal MaterialThemeController? ThemeIfCreated => _theme;
    internal MaterialMotionController? MotionIfCreated => _motion;
    internal MaterialInteractionController? InteractionIfCreated => _interaction;

    /// <summary>Ensures the owner-local composition root exists.</summary>
    internal static MaterialElementContext Ensure(Control owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        owner.VerifyAccess();

        if (owner.GetValue(ContextProperty) is { } existing)
            return existing;

        // Publish before activation. Property notifications can re-enter Ensure while a
        // controller is activating, and that reentrant lookup must see this exact instance.
        var created = new MaterialElementContext(owner);
        try
        {
            owner.SetValue(ContextProperty, created);
        }
        catch
        {
            if (ReferenceEquals(owner.GetValue(ContextProperty), created))
                owner.ClearValue(ContextProperty);
            throw;
        }

        return created;
    }

    /// <summary>Gets a previously initialized runtime without creating one.</summary>
    internal static MaterialElementContext GetRequired(Control owner)
    {
        if (TryGet(owner, out var context))
            return context;

        throw new InvalidOperationException("The control has not initialized its Material runtime.");
    }

    internal static bool TryGet(
        Control owner,
        [NotNullWhen(true)] out MaterialElementContext? context)
    {
        ArgumentNullException.ThrowIfNull(owner);
        owner.VerifyAccess();
        context = owner.GetValue(ContextProperty);
        return context is not null;
    }

    private MaterialThemeController CreateTheme()
    {
        var controller = new MaterialThemeController(_owner);
        _theme = controller;
        controller.Activate();
        return controller;
    }

    private MaterialMotionController CreateMotion()
    {
        var controller = new MaterialMotionController(_owner);
        _motion = controller;
        controller.Activate();
        return controller;
    }

    private MaterialInteractionController CreateInteraction()
    {
        var controller = new MaterialInteractionController(_owner, Theme, Motion);
        _interaction = controller;
        controller.Activate();
        return controller;
    }
}
