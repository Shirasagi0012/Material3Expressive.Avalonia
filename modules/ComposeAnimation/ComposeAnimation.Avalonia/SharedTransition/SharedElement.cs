namespace ComposeAnimation;

/// <summary>
/// Shares one visual identity across a matched pair. During the transition only the incoming
/// content is drawn in the host overlay; the source supplies the start bounds and is kept from
/// painting in place.
/// </summary>
public class SharedElement : SharedTransitionContent
{
    internal override SharedContentKind Kind => SharedContentKind.Element;
}
