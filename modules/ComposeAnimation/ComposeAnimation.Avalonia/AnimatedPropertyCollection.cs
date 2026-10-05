using Avalonia;
using Avalonia.Collections;
using Avalonia.Threading;

namespace ComposeAnimation;

/// <summary>
/// Per-control list of <see cref="AnimatedProperty"/> declarations. The collection is the
/// declaration; motion state lives on the host so the same instance can be shared via styles.
/// </summary>
public sealed class AnimatedPropertyCollection : AvaloniaList<AnimatedProperty>
{
    public AnimatedPropertyCollection()
    {
        ResetBehavior = ResetBehavior.Remove;
        Validate = static item =>
        {
            ArgumentNullException.ThrowIfNull(item);
            Dispatcher.UIThread.VerifyAccess();
            if (item.Property is { IsDirect: true } property)
                throw new InvalidOperationException($"Cannot animate direct property {property}.");
        };
    }
}
