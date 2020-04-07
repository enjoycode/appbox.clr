using System;
using System.ComponentModel;
using System.Globalization;

namespace appbox.Drawing
{
    //[Serializable]
    //[TypeConverter(typeof(PointConverter))]
    public struct Point
    {

        // -----------------------
        // Public Shared Members
        // -----------------------

        /// <summary>
        ///	Empty Shared Field
        /// </summary>
        /// <remarks>
        ///	An uninitialized Point Structure.
        /// </remarks>
        public static readonly Point Empty;

        /// <summary>
        ///	Ceiling Shared Method
        /// </summary>
        /// <remarks>
        ///	Produces a Point structure from a PointF structure by
        ///	taking the ceiling of the X and Y properties.
        /// </remarks>
        public static Point Ceiling(PointF value)
        {
            int x, y;
            checked
            {
                x = (int)Math.Ceiling(value.X);
                y = (int)Math.Ceiling(value.Y);
            }

            return new Point(x, y);
        }

        /// <summary>
        ///	Round Shared Method
        /// </summary>
        /// <remarks>
        ///	Produces a Point structure from a PointF structure by
        ///	rounding the X and Y properties.
        /// </remarks>
        public static Point Round(PointF value)
        {
            int x, y;
            checked
            {
                x = (int)Math.Round(value.X);
                y = (int)Math.Round(value.Y);
            }

            return new Point(x, y);
        }

        /// <summary>
        ///	Truncate Shared Method
        /// </summary>
        /// <remarks>
        ///	Produces a Point structure from a PointF structure by
        ///	truncating the X and Y properties.
        /// </remarks>
        // LAMESPEC: Should this be floor, or a pure cast to int?
        public static Point Truncate(PointF value)
        {
            int x, y;
            checked
            {
                x = (int)value.X;
                y = (int)value.Y;
            }

            return new Point(x, y);
        }

        /// <summary>
        ///	Addition Operator
        /// </summary>
        /// <remarks>
        ///	Translates a Point using the Width and Height
        ///	properties of the given <typeref>Size</typeref>.
        /// </remarks>
        public static Point operator +(Point pt, Size sz)
        {
            return new Point(pt.X + sz.Width, pt.Y + sz.Height);
        }

        /// <summary>
        ///	Equality Operator
        /// </summary>
        /// <remarks>
        ///	Compares two Point objects. The return value is
        ///	based on the equivalence of the X and Y properties 
        ///	of the two points.
        /// </remarks>
        public static bool operator ==(Point left, Point right)
        {
            return ((left.X == right.X) && (left.Y == right.Y));
        }

        /// <summary>
        ///	Inequality Operator
        /// </summary>
        /// <remarks>
        ///	Compares two Point objects. The return value is
        ///	based on the equivalence of the X and Y properties 
        ///	of the two points.
        /// </remarks>
        public static bool operator !=(Point left, Point right)
        {
            return ((left.X != right.X) || (left.Y != right.Y));
        }

        /// <summary>
        ///	Subtraction Operator
        /// </summary>
        /// <remarks>
        ///	Translates a Point using the negation of the Width 
        ///	and Height properties of the given Size.
        /// </remarks>
        public static Point operator -(Point pt, Size sz)
        {
            return new Point(pt.X - sz.Width, pt.Y - sz.Height);
        }

        /// <summary>
        ///	Point to Size Conversion
        /// </summary>
        /// <remarks>
        ///	Returns a Size based on the Coordinates of a given 
        ///	Point. Requires explicit cast.
        /// </remarks>
        public static explicit operator Size(Point p)
        {
            return new Size(p.X, p.Y);
        }

        /// <summary>
        ///	Point to PointF Conversion
        /// </summary>
        /// <remarks>
        ///	Creates a PointF based on the coordinates of a given 
        ///	Point. No explicit cast is required.
        /// </remarks>
        public static implicit operator PointF(Point p)
        {
            return new PointF(p.X, p.Y);
        }

        // -----------------------
        // Public Constructors
        // -----------------------

        /// <summary>
        ///	Point Constructor
        /// </summary>
        /// <remarks>
        ///	Creates a Point from an integer which holds the Y
        ///	coordinate in the high order 16 bits and the X
        ///	coordinate in the low order 16 bits.
        /// </remarks>
        public Point(int dw)
        {
            Y = dw >> 16;
            X = dw & 0xffff;
        }

        /// <summary>
        ///	Point Constructor
        /// </summary>
        /// <remarks>
        ///	Creates a Point from a Size value.
        /// </remarks>
        public Point(Size sz)
        {
            X = sz.Width;
            Y = sz.Height;
        }

        /// <summary>
        ///	Point Constructor
        /// </summary>
        /// <remarks>
        ///	Creates a Point from a specified x,y coordinate pair.
        /// </remarks>
        public Point(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        // -----------------------
        // Public Instance Members
        // -----------------------

        /// <summary>
        ///	IsEmpty Property
        /// </summary>
        /// <remarks>
        ///	Indicates if both X and Y are zero.
        /// </remarks>
        [Browsable(false)]
        public bool IsEmpty
        {
            get { return ((X == 0) && (Y == 0)); }
        }

        /// <summary>
        ///	X Property
        /// </summary>
        /// <remarks>
        ///	The X coordinate of the Point.
        /// </remarks>
        public int X { get; set; }

        /// <summary>
        ///	Y Property
        /// </summary>
        /// <remarks>
        ///	The Y coordinate of the Point.
        /// </remarks>
        public int Y { get; set; }

        /// <summary>
        ///	Equals Method
        /// </summary>
        /// <remarks>
        ///	Checks equivalence of this Point and another object.
        /// </remarks>
        public override bool Equals(object obj)
        {
            if (!(obj is Point))
                return false;

            return (this == (Point)obj);
        }

        /// <summary>
        ///	GetHashCode Method
        /// </summary>
        /// <remarks>
        ///	Calculates a hashing value.
        /// </remarks>
        public override int GetHashCode()
        {
            return X ^ Y;
        }

        /// <summary>
        ///	Offset Method
        /// </summary>
        /// <remarks>
        ///	Moves the Point a specified distance.
        /// </remarks>
        public void Offset(int dx, int dy)
        {
            X += dx;
            Y += dy;
        }

        /// <summary>
        ///	ToString Method
        /// </summary>
        /// <remarks>
        ///	Formats the Point as a string in coordinate notation.
        /// </remarks>
        public override string ToString()
        {
            return $"{{X={X.ToString(CultureInfo.InvariantCulture)},Y={Y.ToString(CultureInfo.InvariantCulture)}}}";
        }

    }
}
