using Avalonia;

namespace Material3Expressive.Foundation;

/// <summary>
/// Base class for the generated per-component override bags a theme can carry.
/// </summary>
/// <remarks>
/// One subclass is generated per component token table, with a styled property per token and — for
/// tables that have variants — a <c>Variant</c> selecting which set it retunes. The bags are
/// sparse: <see cref="AvaloniaObject.IsSet" /> decides what actually replaces the generated
/// default, so an app can change one colour without restating the set around it.
/// <para>
/// Declared inline on a theme:
/// </para>
/// <code>
/// &lt;foundation:MaterialTheme&gt;
///     &lt;foundation:ButtonColorOverrides Variant="Filled" ContainerColor="Custom:Brand" /&gt;
/// &lt;/foundation:MaterialTheme&gt;
/// </code>
/// </remarks>
public abstract class ComponentTokenOverrides : AvaloniaObject
{
}
