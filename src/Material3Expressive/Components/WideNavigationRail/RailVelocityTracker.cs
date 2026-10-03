namespace Material3Expressive.Components;

// Fixed Compose Desktop Lsq2VelocityTracker / VelocityTracker1D (non-differential).
// Local to the modal draggable; no second public interaction or animation system.
internal sealed class RailVelocityTracker
{
    private readonly (ulong Time, float Position)?[] _samples = new (ulong, float)?[20];
    private int _index;
    public void Reset() { Array.Clear(_samples); _index = 0; }
    public void Add(ulong time, float position) => _samples[_index = (_index + 1) % 20] = (time, position);
    public float Calculate(ulong releaseTime)
    {
        if (_samples[_index] is not { } newest || releaseTime - newest.Time > 40) return 0;
        var x = new float[20]; var y = new float[20]; var count = 0; var previous = newest.Time;
        for (var i = 0; i < 20; i++)
        {
            if (_samples[(_index - i + 20) % 20] is not { } sample) break;
            var age = (float)(newest.Time - sample.Time);
            if (age > 100 || previous - sample.Time > 40) break;
            x[count] = -age; y[count] = sample.Position; count++; previous = sample.Time;
        }
        if (count < 2) return 0; // Fixed ComposeUiFlags.isVelocityTrackerMinSampleSizeFixEnabled.
        var n = Math.Min(3, count);
        var a = Enumerable.Range(0, n).Select(_ => new float[count]).ToArray();
        var q = Enumerable.Range(0, n).Select(_ => new float[count]).ToArray();
        var r = Enumerable.Range(0, n).Select(_ => new float[n]).ToArray();
        for (var h = 0; h < count; h++) { a[0][h] = 1; for (var i = 1; i < n; i++) a[i][h] = a[i - 1][h] * x[h]; }
        float Dot(float[] left, float[] right) { var sum = 0f; for (var h = 0; h < count; h++) sum += left[h] * right[h]; return sum; }
        for (var j = 0; j < n; j++)
        {
            Array.Copy(a[j], q[j], count);
            for (var i = 0; i < j; i++) { var dot = Dot(q[j], q[i]); for (var h = 0; h < count; h++) q[j][h] -= dot * q[i][h]; }
            var inverse = 1f / Math.Max(MathF.Sqrt(Dot(q[j], q[j])), 1e-6f);
            for (var h = 0; h < count; h++) q[j][h] *= inverse;
            for (var i = j; i < n; i++) r[j][i] = Dot(q[j], a[i]);
        }
        var coefficients = new float[n];
        for (var i = n - 1; i >= 0; i--)
        {
            var value = Dot(q[i], y);
            for (var j = n - 1; j > i; j--) value -= r[i][j] * coefficients[j];
            coefficients[i] = value / r[i][i];
        }
        var velocity = coefficients[1] * 1000;
        return float.IsNaN(velocity) ? 0 : velocity;
    }
}
