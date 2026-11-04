namespace Material3Expressive.Components;

/// <summary>A full-window search presentation with a contained input and independently fading results.</summary>
/// <remarks>Pair with SearchBarState.CreateContained() for the upstream contained motion defaults.</remarks>
public class ExpandedFullScreenContainedSearchBar : ExpandedFullScreenSearchBar
{
    protected override Type StyleKeyOverride => typeof(ExpandedFullScreenContainedSearchBar);
}
