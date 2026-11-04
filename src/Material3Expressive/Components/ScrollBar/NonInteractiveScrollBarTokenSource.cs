using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class NonInteractiveScrollBarTokenSource : ITokenSource
{
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value)
    {
        if (ReferenceEquals(key, Comp.NonInteractiveScrollBar.Thickness)) { value = 4d; return true; }
        if (ReferenceEquals(key, Comp.NonInteractiveScrollBar.ThumbMinLength)) { value = 24d; return true; }
        if (ReferenceEquals(key, Comp.NonInteractiveScrollBar.ThumbMaxLengthFraction)) { value = 0.9d; return true; }
        if (ReferenceEquals(key, Comp.NonInteractiveScrollBar.MainAxisTrackInset)) { value = 2d; return true; }
        if (ReferenceEquals(key, Comp.NonInteractiveScrollBar.CrossAxisTrackInset)) { value = 0d; return true; }
        if (ReferenceEquals(key, Comp.NonInteractiveScrollBar.ThumbColor)) { value = new TokenRef(Sys.Color.Outline); return true; }
        if (ReferenceEquals(key, Comp.NonInteractiveScrollBar.ThumbOpacity)) { value = 0.7d; return true; }
        if (ReferenceEquals(key, Comp.NonInteractiveScrollBar.TrackColor)) { value = Colors.Transparent; return true; }
        if (ReferenceEquals(key, Comp.NonInteractiveScrollBar.FadeDuration)) { value = TimeSpan.FromMilliseconds(250); return true; }
        if (ReferenceEquals(key, Comp.NonInteractiveScrollBar.FadeDelay)) { value = TimeSpan.FromMilliseconds(400); return true; }
        value = null!; return false;
    }
    public event EventHandler? Changed { add { } remove { } }
}
