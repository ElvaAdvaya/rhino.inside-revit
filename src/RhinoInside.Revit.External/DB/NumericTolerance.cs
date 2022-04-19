using System;

namespace RhinoInside.Revit.External.DB
{
  public static class NumericTolerance
  {
    /// <summary>
    /// Tolerance value that represents the minimum supported tolerance.
    /// </summary>
    public const double MinTolerance = 0D;

    /// <summary>
    /// Tolerance value used when tolerance parameter is omited.
    /// </summary>
    public const double DefaultTolerance = 1e-9;

    /// <summary>
    /// Represents the smallest number such that 1.0 + <see cref="MinDelta"/> != 1.0.
    /// This field is constant.
    /// </summary>
    /// <remarks>
    /// Same as DBL_EPSILON 2.2204460492503131e-16
    /// </remarks>
    public const double MinDelta = double.MaxValue * double.Epsilon / 4.0;

    /// <summary>
    /// Represents the smallest positive normalized finite representable value of a <see cref="double"/>.
    /// This field is constant.
    /// </summary>
    /// <remarks>
    /// Same as +DBL_MIN +2.2250738585072014e-308 
    /// </remarks>
    public const double DenormalUpperBound = 4.0 / double.MaxValue;

    /// <summary>
    /// Represents the largest negative normalized finite representable value of a <see cref="double"/>.
    /// This field is constant.
    /// </summary>
    /// <remarks>
    /// Same as -DBL_MIN -2.2250738585072014e-308 
    /// </remarks>
    public const double DenormalLowerBound = 4.0 / double.MinValue;

    #region AreAlmostEqual
    /// <summary>
    /// Compares two doubles and determines if they are equal within the specified maximum absolute error.
    /// </summary>
    /// <param name="x">First value</param>
    /// <param name="y">Second value</param>
    /// <param name="tolerance">The absolute accuracy required for being almost equal.</param>
    /// <returns>True if both doubles are almost equal up to the specified maximum absolute error, false otherwise.</returns>
    public static bool AreAlmostEqual(double x, double y, double tolerance = DefaultTolerance)
    {
      if (double.IsInfinity(x) || double.IsInfinity(y))
        return x == y;
    
      return Math.Abs(x - y) <= tolerance;
    }
    #endregion
  }

  /// <summary>
  /// This class represents a Summation ∑ of floating points values.
  /// </summary>
  /// <remarks>
  /// Implemented using Neumaier summation algorithm.
  /// </remarks>
  struct Sum
  {
    double sum; // An accumulator.
    double c;   // A running compensation for lost low-order bits.

    public double Value => sum + c;

    public Sum(double value, double c = 0.0) { sum = value; this.c = c; }

    public static Sum operator +(Sum sum, double value) { sum.Add(+value); return sum; }
    public static Sum operator -(Sum sum, double value) { sum.Add(-value); return sum; }

    public void Add(double value)
    {
      var t = sum + value;
      if (Math.Abs(sum) < Math.Abs(value))
        c += (value - t) + sum;
      else
        c += (sum - t) + value;
      sum = t;
    }

    public void Add(params double[] values)
    {
      for (int v = 0; v < values.Length; ++v)
        Add(values[v]);
    }
  }
}
