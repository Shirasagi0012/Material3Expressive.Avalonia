using Avalonia;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

/// <summary>
/// Shares animated bounds while both sides keep their own content. Source fades out and target
/// fades in inside the same Runtime rectangle. Fade and bounds may use different specs; the
/// session waits for both.
/// </summary>
public partial class SharedBounds : SharedTransitionContent
{
    private LookaheadEntry? _lookaheadEntry;

    public static readonly StyledProperty<IFiniteAnimationSpec<double>> FadeSpecProperty =
        AvaloniaProperty.Register<SharedBounds, IFiniteAnimationSpec<double>>(nameof(FadeSpec),
            new SpringSpec<double>(), validate: value => value is not null);

    public IFiniteAnimationSpec<double> FadeSpec
    {
        get => GetValue(FadeSpecProperty);
        set => SetValue(FadeSpecProperty, value);
    }

    internal override SharedContentKind Kind => SharedContentKind.Bounds;
}
