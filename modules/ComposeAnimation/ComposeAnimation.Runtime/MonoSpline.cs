namespace ComposeAnimation.Runtime;

/// <summary>
/// A monotone cubic Hermite spline interpolation in multiple dimensions.
/// </summary>
/// <param name="timePoints">Timestamps of all data points, in seconds.</param>
/// <param name="values">Values at each point, one array per point.</param>
/// <param name="periodicBias">
/// When in the [0, 1] range, adjusts the initial and final velocity to be the same, creating a
/// periodic spline. <see cref="double.NaN"/> for a non-periodic spline.
/// </param>
internal sealed class MonoSpline(double[] timePoints, double[][] values, double periodicBias)
{
    private const bool IsExtrapolate = true;

    private readonly double[] _slopeTemp = new double[values[0].Length];
    private readonly double[][] _tangents = CreateTangents(timePoints, values, periodicBias);

    private static double[][] CreateTangents(double[] time, double[][] values, double periodicBias)
    {
        var n = time.Length;
        var dim = values[0].Length;
        var slope = new double[n - 1][];
        var tangent = new double[n][];
        for (var i = 0; i < n - 1; i++)
            slope[i] = new double[dim];
        for (var i = 0; i < n; i++)
            tangent[i] = new double[dim];

        for (var j = 0; j < dim; j++)
        {
            for (var i = 0; i < n - 1; i++)
            {
                var dt = time[i + 1] - time[i];
                slope[i][j] = (values[i + 1][j] - values[i][j]) / dt;
                tangent[i][j] = i == 0 ? slope[i][j] : (slope[i - 1][j] + slope[i][j]) * 0.5;
            }
            tangent[n - 1][j] = slope[n - 2][j];
        }

        if (!Double.IsNaN(periodicBias))
        {
            for (var j = 0; j < dim; j++)
            {
                // Slope indicated by the bias, where 0 is the last slope and 1 is the initial slope.
                var adjustedSlope = slope[n - 2][j] * (1 - periodicBias) + slope[0][j] * periodicBias;
                slope[0][j] = adjustedSlope;
                slope[n - 2][j] = adjustedSlope;
                tangent[0][j] = adjustedSlope;
                tangent[n - 1][j] = adjustedSlope;
            }
        }

        // Enforce monotonicity by limiting the tangent ratio when slopes have the same sign.
        for (var i = 0; i < n - 1; i++)
        {
            for (var j = 0; j < dim; j++)
            {
                if (slope[i][j] == 0.0)
                {
                    tangent[i][j] = 0.0;
                    tangent[i + 1][j] = 0.0;
                }
                else
                {
                    var a = tangent[i][j] / slope[i][j];
                    var b = tangent[i + 1][j] / slope[i][j];
                    var h = Double.Hypot(a, b);
                    if (h > 9.0)
                    {
                        var t = 3.0 / h;
                        tangent[i][j] = t * a * slope[i][j];
                        tangent[i + 1][j] = t * b * slope[i][j];
                    }
                }
            }
        }

        return tangent;
    }

    /// <summary>
    /// Fills <paramref name="destination"/> with the values at time <paramref name="time"/>.
    /// <paramref name="index"/> is a hint for the segment search; it must be the index returned by
    /// <see cref="KeyframeMath.FindEntryIndex"/> for the same time.
    /// </summary>
    public void GetPosition(double time, Span<double> destination, int index = 0)
    {
        var n = timePoints.Length;
        var dim = values[0].Length;

        var k = time <= timePoints[0] ? 0 : time >= timePoints[n - 1] ? n - 1 : -1;
        if (IsExtrapolate && k != -1)
        {
            GetSlope(timePoints[k], _slopeTemp);
            for (var j = 0; j < dim; j++)
                destination[j] = values[k][j] + (time - timePoints[k]) * _slopeTemp[j];
            return;
        }

        for (var i = index; i < n - 1; i++)
        {
            if (time == timePoints[i])
            {
                for (var j = 0; j < dim; j++)
                    destination[j] = values[i][j];
                return;
            }

            if (time < timePoints[i + 1])
            {
                var h = timePoints[i + 1] - timePoints[i];
                var x = (time - timePoints[i]) / h;
                for (var j = 0; j < dim; j++)
                {
                    destination[j] = HermiteInterpolate(
                        h,
                        x,
                        values[i][j],
                        values[i + 1][j],
                        _tangents[i][j],
                        _tangents[i + 1][j]);
                }
                return;
            }
        }
    }

    /// <summary>Fills <paramref name="destination"/> with the slopes at time <paramref name="time"/>.</summary>
    public void GetSlope(double time, Span<double> destination, int index = 0)
    {
        var n = timePoints.Length;
        var dim = values[0].Length;

        // Out of range (or exactly at the ends), return the corresponding tangent directly.
        var tangentIndex = time <= timePoints[0] ? 0 : time >= timePoints[n - 1] ? n - 1 : -1;
        if (tangentIndex != -1)
        {
            for (var j = 0; j < dim; j++)
                destination[j] = _tangents[tangentIndex][j];
            return;
        }

        for (var i = index; i < n - 1; i++)
        {
            if (time <= timePoints[i + 1])
            {
                var h = timePoints[i + 1] - timePoints[i];
                var x = (time - timePoints[i]) / h;
                for (var j = 0; j < dim; j++)
                {
                    destination[j] = HermiteDifferential(
                        h,
                        x,
                        values[i][j],
                        values[i + 1][j],
                        _tangents[i][j],
                        _tangents[i + 1][j]) / h;
                }
                return;
            }
        }
    }

    private static double HermiteInterpolate(double h, double x, double y1, double y2, double t1, double t2)
    {
        var x2 = x * x;
        var x3 = x2 * x;
        return h * t1 * (x - 2 * x2 + x3) + h * t2 * (x3 - x2) + y1 - (3 * x2 - 2 * x3) * (y1 - y2);
    }

    private static double HermiteDifferential(double h, double x, double y1, double y2, double t1, double t2)
    {
        var x2 = x * x;
        return h * (t1 - 2 * x * (2 * t1 + t2) + 3 * (t1 + t2) * x2) - 6 * (x - x2) * (y1 - y2);
    }
}
