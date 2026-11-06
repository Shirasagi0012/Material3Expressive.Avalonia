namespace Material3Expressive.Foundation.Input;

// Fixed Core VelocityTracker1D(isDataDifferential=true), Impulse strategy. Delta units
// are physical pixels, timestamps milliseconds; unlike touch tracking, no release sample.
internal sealed class DifferentialVelocityTracker
{
    private readonly (ulong Time, float Delta)?[] _samples = new (ulong, float)?[20];
    private int _index;
    internal void Add(ulong time, float delta) => _samples[_index = (_index + 1) % 20] = (time, delta);
    internal float Calculate()
    {
        if (_samples[_index] is not { } newest) return 0;
        var data = new float[20]; var times = new float[20]; var count = 0; var previous = newest.Time;
        for (var i = 0; i < 20; i++)
        {
            if (_samples[(_index - i + 20) % 20] is not { } sample) break;
            var age = (float)(newest.Time - sample.Time);
            if (age > 100 || previous - sample.Time > 40) break;
            data[count] = sample.Delta; times[count] = -age; count++; previous = sample.Time;
        }
        var work = 0f;
        float Velocity() => Math.Sign(work) * MathF.Sqrt(2 * MathF.Abs(work));
        for (var i = count - 1; i > 0; i--)
        {
            var elapsed = times[i] - times[i - 1];
            if (elapsed == 0) continue;
            var velocity = -data[i - 1] / elapsed;
            work += (velocity - Velocity()) * MathF.Abs(velocity);
            if (i == count - 1) work *= .5f;
        }
        return Velocity() * 1000;
    }
}
