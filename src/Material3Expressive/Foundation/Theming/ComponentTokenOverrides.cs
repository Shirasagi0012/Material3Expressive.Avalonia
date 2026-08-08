using Avalonia;

namespace Material3Expressive.Foundation;

/// <summary>
/// Base class for legacy generated per-component override bags a theme can carry.
/// </summary>
/// <remarks>
/// One subclass is generated for each remaining <c>componentDefaults</c> table, with a styled
/// property per token and — for tables that have variants — a <c>Variant</c> selecting which set
/// it retunes. The bags are sparse: <see cref="AvaloniaObject.IsSet" /> decides what actually
/// replaces the generated default, so an app can change one value without restating the set around
/// it.
/// <para>
/// This is a migration compatibility surface. Components on the component-owned Defaults path,
/// such as the Button family, deliberately do not generate an override bag; customise them
/// through their public Avalonia properties, styles or control themes instead.
/// </para>
/// </remarks>
public abstract class ComponentTokenOverrides : AvaloniaObject
{
}
