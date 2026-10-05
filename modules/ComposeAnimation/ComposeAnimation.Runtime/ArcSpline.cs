namespace ComposeAnimation.Runtime;

/// <summary>
/// A curve fit system that stitches an x,y path together with quarter ellipses.
/// </summary>
/// <param name="arcModes">Arc mode per segment, of size <paramref name="timePoints"/>.Length.</param>
/// <param name="timePoints">Timestamps in seconds.</param>
/// <param name="y">
/// Values per timestamp, one array per dimension pair. Arrays are of even size since two values
/// are needed to interpolate each arc.
/// </param>
internal sealed class ArcSpline(int[] arcModes, double[] timePoints, double[][] y)
{
    private const int LutSize = 101;
    private const double Epsilon = 0.001;
    private const double HalfPi = Math.PI * 0.5;
    private const bool IsExtrapolate = true;

    // Internal segment modes.
    private const int StartVertical = 1;
    private const int StartHorizontal = 2;
    private const int StartLinear = 3;
    private const int DownArc = 4;
    private const int UpArc = 5;

    private readonly Arc[][] _arcs = CreateArcs(arcModes, timePoints, y);
    private readonly double[] _position = new double[y[0].Length];
    private readonly double[] _slope = new double[y[0].Length];

    private static Arc[][] CreateArcs(int[] arcModes, double[] timePoints, double[][] y)
    {
        var arcs = new Arc[timePoints.Length - 1][];
        var mode = StartVertical;
        var last = StartVertical;

        for (var i = 0; i < arcs.Length; i++)
        {
            mode = arcModes[i] switch
            {
                ArcSplineArcStartVertical => last = StartVertical,
                ArcSplineArcStartHorizontal => last = StartHorizontal,
                ArcSplineArcStartFlip => last = last == StartVertical ? StartHorizontal : StartVertical,
                ArcSplineArcStartLinear => StartLinear,
                ArcSplineArcAbove => UpArc,
                ArcSplineArcBelow => DownArc,
                _ => mode,
            };

            var from = y[i];
            var to = y[i + 1];
            var pairCount = from.Length / 2 + from.Length % 2;
            arcs[i] = new Arc[pairCount];
            for (var j = 0; j < pairCount; j++)
            {
                var k = j * 2;
                arcs[i][j] = new Arc(mode, timePoints[i], timePoints[i + 1], from[k], from[k + 1], to[k], to[k + 1]);
            }
        }

        return arcs;
    }

    /// <summary>Fills <paramref name="destination"/> with the values at time <paramref name="time"/>.</summary>
    public void GetPosition(double time, Span<double> destination)
    {
        var lastIndex = _arcs.Length - 1;
        var start = _arcs[0][0].Time1;
        var end = _arcs[lastIndex][0].Time2;
        var size = _position.Length;

        if (IsExtrapolate && (time < start || time > end))
        {
            // Extrapolate linearly from the slope at the closest end point.
            var segment = time > end ? lastIndex : 0;
            var anchor = time > end ? end : start;
            var dt = time - anchor;

            for (var j = 0; j < size / 2; j++)
            {
                var arc = _arcs[segment][j];
                if (arc.IsLinear)
                {
                    _position[j * 2] = arc.GetLinearX(anchor) + dt * arc.LinearDx;
                    _position[j * 2 + 1] = arc.GetLinearY(anchor) + dt * arc.LinearDy;
                }
                else
                {
                    arc.SetPoint(anchor);
                    _position[j * 2] = arc.CalcX() + dt * arc.CalcDx();
                    _position[j * 2 + 1] = arc.CalcY() + dt * arc.CalcDy();
                }
            }
        }
        else
        {
            var populated = false;
            for (var i = 0; i < _arcs.Length && !populated; i++)
            {
                for (var j = 0; j < size / 2; j++)
                {
                    var arc = _arcs[i][j];
                    if (time <= arc.Time2)
                    {
                        if (arc.IsLinear)
                        {
                            _position[j * 2] = arc.GetLinearX(time);
                            _position[j * 2 + 1] = arc.GetLinearY(time);
                        }
                        else
                        {
                            arc.SetPoint(time);
                            _position[j * 2] = arc.CalcX();
                            _position[j * 2 + 1] = arc.CalcY();
                        }
                        populated = true;
                    }
                }
            }
        }

        _position.AsSpan(0, destination.Length).CopyTo(destination);
    }

    /// <summary>Fills <paramref name="destination"/> with the slopes at time <paramref name="time"/>.</summary>
    public void GetSlope(double time, Span<double> destination)
    {
        var t = Math.Clamp(time, _arcs[0][0].Time1, _arcs[^1][0].Time2);
        var size = _slope.Length;

        var populated = false;
        for (var i = 0; i < _arcs.Length && !populated; i++)
        {
            for (var j = 0; j < size / 2; j++)
            {
                var arc = _arcs[i][j];
                if (t <= arc.Time2)
                {
                    if (arc.IsLinear)
                    {
                        _slope[j * 2] = arc.LinearDx;
                        _slope[j * 2 + 1] = arc.LinearDy;
                    }
                    else
                    {
                        arc.SetPoint(t);
                        _slope[j * 2] = arc.CalcDx();
                        _slope[j * 2 + 1] = arc.CalcDy();
                    }
                    populated = true;
                }
            }
        }

        _slope.AsSpan(0, destination.Length).CopyTo(destination);
    }

    internal const int ArcSplineArcStartLinear = 0;
    internal const int ArcSplineArcStartVertical = 1;
    internal const int ArcSplineArcStartHorizontal = 2;
    internal const int ArcSplineArcStartFlip = 3;
    internal const int ArcSplineArcBelow = 4;
    internal const int ArcSplineArcAbove = 5;

    private sealed class Arc
    {
        private const double LutSizeMinusOne = LutSize - 1;

        private double _arcDistance;
        private readonly double _oneOverDeltaTime;
        private readonly double _arcVelocity;
        private readonly double _vertical;
        private readonly double[] _lut = new double[LutSize];
        private readonly double _ellipseA;
        private readonly double _ellipseB;
        private readonly double _ellipseCenterX;
        private readonly double _ellipseCenterY;
        private readonly double _x1;
        private readonly double _y1;
        private readonly double _x2;
        private readonly double _y2;
        private double _tmpSinAngle;
        private double _tmpCosAngle;

        public Arc(int mode, double time1, double time2, double x1, double y1, double x2, double y2)
        {
            Time1 = time1;
            Time2 = time2;
            _x1 = x1;
            _y1 = y1;
            _x2 = x2;
            _y2 = y2;

            var dx = x2 - x1;
            var dy = y2 - y1;
            var isVertical = mode switch
            {
                StartVertical => true,
                UpArc => dy < 0,
                DownArc => dy > 0,
                _ => false,
            };
            _vertical = isVertical ? -1.0 : 1.0;
            _oneOverDeltaTime = 1 / (time2 - time1);

            IsLinear = mode == StartLinear || Math.Abs(dx) < Epsilon || Math.Abs(dy) < Epsilon;
            if (IsLinear)
            {
                _arcDistance = Double.Hypot(dy, dx);
                _arcVelocity = _arcDistance * _oneOverDeltaTime;
                // Cache the slope in the unused ellipse center.
                _ellipseCenterX = dx * _oneOverDeltaTime;
                _ellipseCenterY = dy * _oneOverDeltaTime;
                _ellipseA = Double.NaN;
                _ellipseB = Double.NaN;
            }
            else
            {
                _ellipseA = dx * _vertical;
                _ellipseB = dy * -_vertical;
                _ellipseCenterX = isVertical ? x2 : x1;
                _ellipseCenterY = isVertical ? y1 : y2;
                BuildTable(x1, y1, x2, y2);
                _arcVelocity = _arcDistance * _oneOverDeltaTime;
            }
        }

        public double Time1 { get; }
        public double Time2 { get; }
        public bool IsLinear { get; }
        public double LinearDx => _ellipseCenterX;
        public double LinearDy => _ellipseCenterY;

        public void SetPoint(double time)
        {
            var angle = CalcAngle(time);
            _tmpSinAngle = Math.Sin(angle);
            _tmpCosAngle = Math.Cos(angle);
        }

        private double CalcAngle(double time)
        {
            var percent = (_vertical == -1.0 ? Time2 - time : time - Time1) * _oneOverDeltaTime;
            return HalfPi * Lookup(percent);
        }

        public double CalcX() => _ellipseCenterX + _ellipseA * _tmpSinAngle;
        public double CalcY() => _ellipseCenterY + _ellipseB * _tmpCosAngle;

        public double CalcDx() => _ellipseA * _tmpCosAngle * TangentNorm() * _vertical;
        public double CalcDy() => -_ellipseB * _tmpSinAngle * TangentNorm() * _vertical;

        private double TangentNorm()
        {
            var vx = _ellipseA * _tmpCosAngle;
            var vy = -_ellipseB * _tmpSinAngle;
            return _arcVelocity / Double.Hypot(vx, vy);
        }

        public double GetLinearX(double time)
        {
            var t = (time - Time1) * _oneOverDeltaTime;
            return _x1 + t * (_x2 - _x1);
        }

        public double GetLinearY(double time)
        {
            var t = (time - Time1) * _oneOverDeltaTime;
            return _y1 + t * (_y2 - _y1);
        }

        private double Lookup(double v)
        {
            if (v <= 0)
                return 0.0;
            if (v >= 1)
                return 1.0;

            var pos = v * LutSizeMinusOne;
            var index = (int)pos;
            var offset = pos - index;
            return _lut[index] + offset * (_lut[index + 1] - _lut[index]);
        }

        private void BuildTable(double x1, double y1, double x2, double y2)
        {
            var a = x2 - x1;
            var b = y1 - y2;
            var percents = new double[91];

            var lx = 0.0;
            var ly = b; // == b * cos(0), since index 0 is skipped in the loop below.
            var dist = 0.0;

            for (var i = 1; i < percents.Length; i++)
            {
                var angle = (90.0 * i / (percents.Length - 1)) * (Math.PI / 180.0);
                var s = Math.Sin(angle);
                var c = Math.Cos(angle);
                var px = a * s;
                var py = b * c;
                dist += Double.Hypot(px - lx, py - ly);
                percents[i] = dist;
                lx = px;
                ly = py;
            }

            _arcDistance = dist;
            for (var i = 1; i < percents.Length; i++)
                percents[i] /= dist;

            var lutLastIndex = LutSizeMinusOne;
            for (var i = 0; i < LutSize; i++)
            {
                var pos = i / lutLastIndex;
                var index = Array.BinarySearch(percents, pos);
                if (index >= 0)
                {
                    _lut[i] = index / (percents.Length - 1.0);
                }
                else
                {
                    var insertion = ~index;
                    if (insertion == 0)
                    {
                        _lut[i] = 0.0;
                    }
                    else
                    {
                        var p1 = insertion - 1;
                        var fraction = (p1 + (pos - percents[p1]) / (percents[insertion] - percents[p1]));
                        _lut[i] = fraction / (percents.Length - 1.0);
                    }
                }
            }
        }
    }
}
