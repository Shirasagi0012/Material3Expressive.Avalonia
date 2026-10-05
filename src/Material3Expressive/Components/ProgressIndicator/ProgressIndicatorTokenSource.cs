using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class ProgressIndicatorTokenSource : ITokenSource
{
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value)
    {
        if (ReferenceEquals(key, Comp.LinearProgressIndicator.Width)) { value = 240d; return true; }
        if (ReferenceEquals(key, Comp.CircularProgressIndicator.IndeterminateTrackColor)) { value = Avalonia.Media.Colors.Transparent; return true; }
        if (ReferenceEquals(key, Comp.ProgressIndicator.StrokeCap)) { value = Avalonia.Media.PenLineCap.Round; return true; }
        if (ReferenceEquals(key, Comp.ProgressIndicator.ActiveIndicatorColor)) { value = new TokenRef(Sys.Color.Primary); return true; }
        if (ReferenceEquals(key, Comp.ProgressIndicator.TrackColor)) { value = new TokenRef(Sys.Color.SecondaryContainer); return true; }
        if (ReferenceEquals(key, Comp.LinearProgressIndicator.Height) || ReferenceEquals(key, Comp.LinearProgressIndicator.ActiveThickness) ||
            ReferenceEquals(key, Comp.LinearProgressIndicator.TrackThickness) || ReferenceEquals(key, Comp.LinearProgressIndicator.TrackActiveSpace) ||
            ReferenceEquals(key, Comp.LinearProgressIndicator.StopSize) || ReferenceEquals(key, Comp.CircularProgressIndicator.ActiveThickness) ||
            ReferenceEquals(key, Comp.CircularProgressIndicator.TrackThickness) || ReferenceEquals(key, Comp.CircularProgressIndicator.TrackActiveSpace))
        { value = 4d; return true; }
        if (ReferenceEquals(key, Comp.LinearProgressIndicator.WaveHeight)) { value = 10d; return true; }
        if (ReferenceEquals(key, Comp.LinearProgressIndicator.ActiveWaveWavelength) || ReferenceEquals(key, Comp.CircularProgressIndicator.Size))
        { value = 40d; return true; }
        if (ReferenceEquals(key, Comp.LinearProgressIndicator.IndeterminateActiveWaveWavelength)) { value = 20d; return true; }
        if (ReferenceEquals(key, Comp.CircularProgressIndicator.WaveSize)) { value = 48d; return true; }
        if (ReferenceEquals(key, Comp.CircularProgressIndicator.ActiveWaveWavelength)) { value = 15d; return true; }
        value = null!; return false;
    }
    public event EventHandler? Changed { add { } remove { } }
}
