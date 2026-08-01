using Avalonia;
using Avalonia.Controls;

namespace Material3Expressive.Controls;

/// <summary>Shares one interaction host between all presenters in a control template.</summary>
internal static class MaterialInteraction
{
    private static readonly AttachedProperty<MaterialInteractionHost?> HostProperty =
        AvaloniaProperty.RegisterAttached<Control, MaterialInteractionHost?>(
            "MaterialInteractionHost", typeof(MaterialInteraction));

    public static MaterialInteractionHost GetOrCreate(Control owner)
    {
        if (owner.GetValue(HostProperty) is { } existing)
            return existing;

        var created = new MaterialInteractionHost(owner);
        owner.SetValue(HostProperty, created);
        return created;
    }
}
