using System;

namespace weatherd.math
{
    /// <summary>
    ///     Represents an Angle in degrees, radians or gradians.
    /// </summary>
    public readonly struct Angle : IComparable<Angle>, IEquatable<Angle>
    {
        #region Constants

        public static readonly Angle Zero = FromDegrees(0);
        public static readonly Angle Pos90 = FromDegrees(90);
        public static readonly Angle Neg90 = FromDegrees(-90);
        public static readonly Angle Pos180 = FromDegrees(180);
        public static readonly Angle Neg180 = FromDegrees(-180);
        public static readonly Angle Pos360 = FromDegrees(360);
        public static readonly Angle Neg360 = FromDegrees(-360);
        public static readonly Angle Minute = FromDegrees(1.0 / 60.0);
        public static readonly Angle Second = FromDegrees(1.0 / 3600.0);

        private const double DEGREES_TO_RADIANS = Math.PI / 180d;
        private const double RADIANS_TO_DEGREES = 180d / Math.PI;
        private const double RADIANS_TO_GRADIANS = 63.662;
        private const double GRADIANS_TO_RADIANS = 1 / 63.662;

        #endregion

        #region Properties

        public double Degrees { get; }
        public double Radians { get; }
        public double Gradians { get; }

        #endregion

        #region Constructors

        public static Angle FromDegrees(double degrees) => new(degrees * DEGREES_TO_RADIANS);

        public static Angle FromRadians(double radians) => new(radians);

        public static Angle FromGradians(double gradians) => FromRadians(GRADIANS_TO_RADIANS * gradians);

        public Angle(Angle angle)
        {
            if (angle == null)
                throw new ArgumentNullException(nameof(angle));

            Degrees = angle.Degrees;
            Radians = angle.Radians;
            Gradians = angle.Gradians;
        }

        public Angle(double radians)
        {
            Degrees = radians * RADIANS_TO_DEGREES;
            Radians = radians;
            Gradians = radians * RADIANS_TO_GRADIANS;
        }

        #endregion

        #region Calculations

        /// <summary>
        ///     Adds an angle to this angle.
        /// </summary>
        /// <param name="angle">The angle to add to this angle.</param>
        /// <returns>The result of adding this angle and <paramref name="angle" /></returns>
        public Angle Add(Angle angle)
        {
            if (angle == null)
                throw new ArgumentNullException(nameof(angle));

            return new Angle(Radians + angle.Radians);
        }

        /// <summary>
        ///     Subtracts an angle from this angle.
        /// </summary>
        /// <param name="angle">The angle to subtract from this angle.</param>
        /// <returns>The result of subtracting <paramref name="angle" /> from this angle.</returns>
        public Angle Subtract(Angle angle)
        {
            if (angle == null)
                throw new ArgumentNullException(nameof(angle));

            return new Angle(Radians - angle.Radians);
        }

        /// <summary>
        ///     Multiplies this angle by another angle.
        /// </summary>
        /// <param name="angle">The angle to multiply this angle with.</param>
        /// <returns>The result of this angle multiplied by <paramref name="angle" /></returns>
        public Angle Multiply(Angle angle)
        {
            if (angle == null)
                throw new ArgumentNullException(nameof(angle));

            return new Angle(Radians * angle.Radians);
        }

        /// <summary>
        ///     Multiplies this angle by a scalar.
        /// </summary>
        /// <param name="scalar">The number to multiply the angle with.</param>
        /// <returns>The result of this angle multiplied by <paramref name="scalar" /></returns>
        public Angle Multiply(double scalar)
            => new(Radians * scalar);

        /// <summary>
        ///     Divides this angle by another angle.
        /// </summary>
        /// <param name="angle">The angle to divide this angle by.</param>
        /// <returns>The result of this angle divided by <paramref name="angle" /></returns>
        public Angle Divide(Angle angle)
        {
            if (angle == null)
                throw new ArgumentNullException(nameof(angle));
            if (Math.Abs(angle.Degrees) < double.Epsilon)
                throw new ArgumentOutOfRangeException(nameof(angle), @"Degrees must not be zero.");

            return new Angle(Radians / angle.Radians);
        }

        /// <summary>
        ///     Divides this angle by a divisor.
        /// </summary>
        /// <param name="divisor">The divisor to divide the angle by.</param>
        /// <returns>The result of this angle divided by <paramref name="divisor" /></returns>
        public Angle Divide(int divisor)
            => new(Radians / divisor);

        /// <summary>
        ///     Divides this angle by a divisor.
        /// </summary>
        /// <param name="divisor">The divisor to divide the angle by.</param>
        /// <returns>The result of this angle divided by <paramref name="divisor" /></returns>
        public Angle Divide(double divisor)
            => new(Radians / divisor);

        /// <summary>
        ///     Returns the angular distance from this angle to another angle.
        /// </summary>
        /// <param name="angle">The angle to measure arcdistance to.</param>
        /// <returns>The angular distance between this angle and <paramref name="angle" /></returns>
        public Angle AngularDistanceTo(Angle angle)
        {
            if (angle == null)
                throw new ArgumentNullException(nameof(angle));

            double diffDegrees = (angle - this).Degrees;
            if (diffDegrees < -180)
                diffDegrees += 360;
            else if (diffDegrees > 180)
                diffDegrees -= 360;

            double absAngle = Math.Abs(diffDegrees);

            return FromDegrees(absAngle);
        }

        /// <summary>
        ///     Returns the sine of this angle.
        /// </summary>
        /// <returns>
        ///     The sine of this angle. If this angle is equal to NaN, NegativeInfinity,
        ///     or PositiveInfinity, this method returns NaN.
        /// </returns>
        public double Sin() => Math.Sin(Radians);

        /// <summary>
        ///     Returns the sine of half this angle.
        /// </summary>
        /// <returns>
        ///     The sine of half this angle. If this angle is equal to NaN, NegativeInfinity,
        ///     or PositiveInfinity, this method returns NaN.
        /// </returns>
        public double SinHalfAngle() => Math.Sin(Radians * 0.5);

        /// <summary>
        ///     Returns the angle whose sine is the specified number.
        /// </summary>
        /// <param name="sine">
        ///     A number representing a sine, where <paramref name="sine" /> must be greater than or equal to -1,
        ///     but less than or equal to 1.
        /// </param>
        /// <returns>
        ///     An angle, θ, measured in radians, such that -π/2≤θ≤π/2
        ///     -or-
        ///     NaN if <paramref name="sine" />&lt;-1 or <paramref name="sine" />&gt;1 or <paramref name="sine" /> equals NaN.
        /// </returns>
        public static Angle Asin(double sine) => new(Math.Asin(sine));

        /// <summary>
        ///     Returns the cosine of this angle.
        /// </summary>
        /// <returns>
        ///     The cosine of this angle. If this angle is equal to NaN, NegativeInfinity,
        ///     or PositiveInfinity, this method returns NaN.
        /// </returns>
        public double Cos() => Math.Cos(Radians);

        /// <summary>
        ///     Returns the cosine of half this angle.
        /// </summary>
        /// <returns>
        ///     The cosine of half this angle. If this angle is equal to NaN, NegativeInfinity,
        ///     or PositiveInfinity, this method returns NaN.
        /// </returns>
        public double CosHalfAngle() => Math.Cos(Radians * 0.5);

        /// <summary>
        ///     Returns the angle whose cosine is the specified number.
        /// </summary>
        /// <param name="cosine">
        ///     A number representing a cosine, where <paramref name="cosine">cosine</paramref> must be greater
        ///     than or equal to -1, but less than or equal to 1.
        /// </param>
        /// <returns>
        ///     An angle, θ, measured in radians, such that 0 ≤θ≤π
        ///     -or-
        ///     NaN if <paramref name="cosine">cosine</paramref>&lt;-1 or <paramref name="cosine">cosine</paramref>&gt;1 or
        ///     <paramref name="cosine">cosine</paramref> equals NaN.
        /// </returns>
        public static Angle Acos(double cosine) => new(Math.Acos(cosine));

        /// <summary>
        ///     Returns the tangent of this angle.
        /// </summary>
        /// <returns>
        ///     The tangent of this angle. If this angle is equal to NaN, NegativeInfinity,
        ///     or PositiveInfinity, this method returns NaN.
        /// </returns>
        public double Tan() => Math.Tan(Radians);

        /// <summary>
        ///     Returns the angle whose tangent is half the current angle.
        /// </summary>
        /// <returns>
        ///     An angle, θ, measured in radians, such that -π/2 ≤θ≤π/2.
        ///     -or-
        ///     NaN if d equals NaN, -π/2 rounded to double precision(-1.5707963267949) if d equals NegativeInfinity,
        ///     or π/2 rounded to double precision(1.5707963267949) if d equals PositiveInfinity.
        /// </returns>
        /// <remarks>
        ///     A positive return value represents a counterclockwise angle from the x-axis; a negative return value represents a
        ///     clockwise angle.
        /// </remarks>
        public double TanHalfAngle() => Math.Tan(Radians * 0.5);

        /// <summary>
        ///     Returns the hyperbolic arctangent of a number.
        /// </summary>
        /// <param name="rad">A number.</param>
        /// <returns>
        ///     A number that is the hyperbolic arctangent of the input, that is:
        ///     ∀x∊(-1,1), Atanh(x)=arctanh(x)= the unique y such that tanh(y)=x
        /// </returns>
        public static double Atanh(double rad) => 0.5 * Math.Log((1 + rad) / (1 - rad));

        /// <summary>
        ///     Returns the angle whose tangent is the specified number.
        /// </summary>
        /// <param name="tangent">A number representing a tangent.</param>
        /// <returns>
        ///     An angle, θ, measured in radians, such that -π/2 ≤θ≤π/2.
        ///     -or-
        ///     NaN if d equals NaN, -π/2 rounded to double precision(-1.5707963267949) if d equals NegativeInfinity,
        ///     or π/2 rounded to double precision(1.5707963267949) if d equals PositiveInfinity.
        /// </returns>
        /// <remarks>
        ///     A positive return value represents a counterclockwise angle from the x-axis; a negative return value represents a
        ///     clockwise angle.
        /// </remarks>
        public static Angle Atan(double tangent) => new(Math.Atan(tangent));

        /// <summary>
        ///     Returns the angle whose tangent is the quotient of two specified numbers.
        /// </summary>
        /// <param name="y">The y coordinate of a point.</param>
        /// <param name="x">The x coordinate of a point.</param>
        /// <returns>
        ///     An angle, θ, measured in radians, such that -π≤θ≤π, and tan(θ) = y / x, where (x, y) is a point in the Cartesian
        ///     plane. Observe the following:
        ///     - For(x, y) in quadrant 1, 0 &lt; θ&lt;π/2.
        ///     - For (x, y) in quadrant 2, π/2 &lt; θ&lt;π.
        ///     - For(x, y) in quadrant 3, -π&lt;θ&lt;-π/2.
        ///     - For (x, y) in quadrant 4, -π/2&lt;θ&lt;0.
        ///     For points on the boundaries of the quadrants, the return value is the following:
        ///     If y is 0 and x is not negative, θ = 0.
        ///     - If y is 0 and x is negative, θ = π.
        ///     - If y is positive and x is 0, θ = π/2.
        ///     - If y is negative and x is 0, θ = -π/2.
        ///     - If y is 0 and x is 0, θ = 0.
        ///     If x or y is NaN, or if x and y are either PositiveInfinity or NegativeInfinity, the method returns NaN.
        /// </returns>
        /// <remarks>
        ///     The return value is the angle in the Cartesian plane formed by the x-axis, and a vector starting from the origin,
        ///     (0,0), and terminating at the point, (x,y).
        /// </remarks>
        public static Angle Atan2(double y, double x) => new(Math.Atan2(y, x));

        /// <summary>
        ///     Returns the angle exactly between two angles.
        /// </summary>
        /// <param name="a">The first angle.</param>
        /// <param name="b">The second angle.</param>
        /// <returns>The angle exactly between the two angles.</returns>
        public static Angle MidAngle(Angle a, Angle b) => Average(a, b);

        /// <summary>
        ///     Returns the average of two angles.
        /// </summary>
        /// <param name="a">The first angle.</param>
        /// <param name="b">The second angle.</param>
        /// <returns>The angular average between the two angles.</returns>
        public static Angle Average(Angle a, Angle b)
        {
            if (a == null)
                throw new ArgumentNullException(nameof(a));
            if (b == null)
                throw new ArgumentNullException(nameof(b));

            return FromDegrees(0.5 * (a + b).Degrees);
        }

        /// <summary>
        ///     Clamps the angle to a minimum and maximum angle.
        /// </summary>
        /// <param name="val">The angle to clamp.</param>
        /// <param name="min">The minimum angle.</param>
        /// <param name="max">The maximum angle.</param>
        /// <returns>
        ///     The resultant angle, such that:
        ///     - If <paramref name="val" />&lt;<paramref name="min" />, return <paramref name="min" />
        ///     - If <paramref name="val" />&gt;<paramref name="max" />, return <paramref name="max" />
        ///     - Else return <paramref name="val" />
        /// </returns>
        public static Angle Clamp(Angle val, Angle min, Angle max)
        {
            if (val == null)
                throw new ArgumentNullException(nameof(val));
            if (min == null)
                throw new ArgumentNullException(nameof(min));
            if (max == null)
                throw new ArgumentNullException(nameof(max));

            return val.Degrees < min.Degrees ? min : val.Degrees > max.Degrees ? max : val;
        }

        /// <summary>
        ///     Returns an angle between 0 and 360 degrees.
        /// </summary>
        /// <returns>An angle between 0 and 360 degrees.</returns>
        public Angle Normalize()
        {
            double deg = Degrees;

            while (deg < 0)
                deg += 360;
            while (deg > 360)
                deg -= 360;

            return FromDegrees(deg % 360);
        }

        /// <summary>
        ///     Returns an angle between -90 and +90 degrees.
        /// </summary>
        /// <param name="lat">The angle to normalize.</param>
        /// <returns>The angle between -90 and +90 degrees.</returns>
        public static Angle NormalizeLatitude(Angle lat)
        {
            double latDeg = lat.Degrees % 180;

            return FromDegrees(latDeg > 90 ? 180 - latDeg :
                               latDeg < -90 ? -180 - latDeg : latDeg);
        }

        /// <summary>
        ///     Returns an angle between -180 and +180 degrees.
        /// </summary>
        /// <param name="lon">The angle to normalize.</param>
        /// <returns>The angle between -180 and +180 degrees.</returns>
        public static Angle NormalizeLongitude(Angle lon)
        {
            double lonDeg = lon.Degrees % 180;

            return FromDegrees(lonDeg > 180 ? lonDeg - 360 : lonDeg < -180 ? 360 + lonDeg : lonDeg);
        }

        #endregion

        #region Operators

        public static Angle operator +(Angle a, Angle b) => a.Add(b);

        public static Angle operator -(Angle a, Angle b) => a.Subtract(b);
        public static Angle operator *(Angle a, Angle b) => a.Multiply(b);
        public static Angle operator *(Angle a, int b) => FromRadians(a.Radians * b);
        public static Angle operator *(Angle a, double b) => FromRadians(a.Radians * b);
        public static Angle operator *(int a, Angle b) => FromRadians(b.Radians * a);
        public static Angle operator *(double a, Angle b) => FromRadians(b.Radians * a);

        public static Angle operator /(Angle a, Angle b) => a.Divide(b);

        public static Angle operator /(Angle a, int b) => FromRadians(a.Radians / b);

        public static Angle operator /(Angle a, double b)
        {
            if (a == null)
                throw new ArgumentNullException(nameof(a));
            return FromRadians(a.Radians / b);
        }

        public static Angle operator /(Angle a, float b) => FromRadians(a.Radians / b);

        public static bool operator <(Angle a, Angle b)
        {
            if (a == null)
                throw new ArgumentNullException(nameof(a));
            if (b == null)
                throw new ArgumentNullException(nameof(b));

            return a.Radians < b.Radians;
        }

        public static bool operator >(Angle a, Angle b)
        {
            if (a == null)
                throw new ArgumentNullException(nameof(a));
            if (b == null)
                throw new ArgumentNullException(nameof(b));

            return a.Radians > b.Radians;
        }

        public static Angle operator -(Angle a) => new(-a.Radians);

        #endregion

        #region Comparisons

        public int CompareTo(Angle other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            return Degrees < other.Degrees ? -1 : Degrees > other.Degrees ? 1 : 0;
        }

        public static Angle Min(Angle a, Angle b)
        {
            if (a == null)
                throw new ArgumentNullException(nameof(a));
            if (b == null)
                throw new ArgumentNullException(nameof(b));

            return a.Degrees < b.Degrees ? a : b;
        }

        public static Angle Max(Angle a, Angle b)
        {
            if (a == null)
                throw new ArgumentNullException(nameof(a));
            if (b == null)
                throw new ArgumentNullException(nameof(b));

            return a.Degrees > b.Degrees ? a : b;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            return obj.GetType() == GetType() && Equals((Angle)obj);
        }

        public bool Equals(Angle other) => Degrees.Equals(other.Degrees) && Radians.Equals(other.Radians);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Degrees.GetHashCode() * 397) ^ Radians.GetHashCode();
            }
        }

        public static bool operator ==(Angle left, Angle right) => Equals(left, right);

        public static bool operator !=(Angle left, Angle right) => !Equals(left, right);

        #endregion

        #region Conversions

        public DMSForm ToDMS()
        {
            int sgn = Degrees < 0 ? -1 : 1;
            double degAbs = Math.Abs(Degrees);

            double d = Math.Floor(degAbs);
            double m = Math.Floor((degAbs - d) * 60);
            double s = Math.Round((degAbs - d - m / 60.0) * 3600, 2);

            return new DMSForm(sgn * (int)d, (int)m, s);
        }

        public DMForm ToDM()
        {
            int sgn = Degrees < 0 ? -1 : 1;
            double degAbs = Math.Abs(Degrees);

            double d = Math.Floor(degAbs);
            double m = Math.Round((degAbs - d) * 60, 2);

            return new DMForm(sgn * (int)d, m);
        }

        public HMSForm ToHMS()
        {
            int sgn = Degrees < 0 ? -1 : 1;
            double degAbs = Math.Abs(Degrees);

            double h = Math.Floor(degAbs / 15.0);
            double m = Math.Floor((degAbs / 15.0 - h) * 60);
            double s = Math.Round((degAbs / 15.0 - h - m / 60.0) * 3600, 2);

            return new HMSForm(sgn * (int)h, (int)m, s);
        }

        public static Angle FromDMS(double d, double m, double s) =>
            FromDegrees((d < 0 ? -1 : 1) * (Math.Abs(d) + m / 60.0 + s / 3600.0));

        public static Angle FromDM(double d, double m) => FromDegrees(d + m / 60.0);
        public static Angle FromHMS(double h, double m, double s) => FromDegrees((h + m / 60.0 + s / 3600.0) * 15);

        /// <inheritdoc />
        public override string ToString() => $"{Degrees}°";

        #endregion

        #region Structures

        public readonly struct DMSForm
        {
            public readonly int Degrees;
            public readonly int Minutes;
            public readonly double Seconds;

            public DMSForm(int d, int m, double s)
            {
                Degrees = d;
                Minutes = m;
                Seconds = s;
            }

            public Angle ToAngle() => FromDegrees(Degrees + Minutes / 60.0 + Seconds / 3600.0);

            /// <inheritdoc />
            public override string ToString() => $"{Degrees}°{Minutes}'{Seconds}\"";
        }

        public readonly struct DMForm
        {
            public readonly int Degrees;
            public readonly double Minutes;

            public DMForm(int d, double m)
            {
                Degrees = d;
                Minutes = m;
            }

            public Angle ToAngle() => FromDegrees(Degrees + Minutes / 60.0);

            /// <inheritdoc />
            public override string ToString() => $"{Degrees}°{Minutes}'";
        }

        public readonly struct HMSForm
        {
            public readonly int Hours;
            public readonly int Minutes;
            public readonly double Seconds;

            public HMSForm(int h, int m, double s)
            {
                Hours = h;
                Minutes = m;
                Seconds = s;
            }

            public Angle ToAngle() => FromDegrees((Hours + Minutes / 60.0 + Seconds / 3600.0) * 15);

            /// <inheritdoc />
            public override string ToString() => $"{Hours}h{Minutes}m{Seconds}s";
        }

        #endregion
    }
}
