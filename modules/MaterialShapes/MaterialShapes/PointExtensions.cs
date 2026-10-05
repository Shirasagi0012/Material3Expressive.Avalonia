using Avalonia;

namespace MaterialShapes;

internal static class PointExtensions
{
    extension(Point p)
    {
        /// <summary>
        /// Compute the Z coordinate of the cross product of two vectors (p x other).
        /// Returns true if the second vector is going clockwise (&gt; 0) compared with the first one,
        /// false if it is counter-clockwise (&lt;= 0). It will be 0 if the vectors are co-linear.
        /// </summary>
        public bool Clockwise(Point other)
        {
            return p.X * other.Y - p.Y * other.X > 0;
        }

        public double GetDistanceSquared()
        {
            return p.X * p.X + p.Y * p.Y;
        }

        internal double GetDistance()
        {
            return Math.Sqrt(p.X * p.X + p.Y * p.Y);
        }

        internal Point GetDirection()
        {
            var d = p.GetDistance();
            return d <= Utils.DistanceEpsilon ? default : p / d;
        }

        internal double DotProduct(Point other)
        {
            return p.X * other.X + p.Y * other.Y;
        }

        internal Point Rotate90()
        {
            return new Point(-p.Y, p.X);
        }
    }
}