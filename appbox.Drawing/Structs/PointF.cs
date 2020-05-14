using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;

namespace appbox.Drawing
{
	//[Serializable]
	//[ComVisible(true)]
	public struct PointF
	{

        // -----------------------
        // Public Shared Members
        // -----------------------

        /// <summary>
        ///	Empty Shared Field
        /// </summary>
        /// <remarks>
        ///	An uninitialized PointF Structure.
        /// </remarks>
        public static readonly PointF Empty;

		/// <summary>
		///	Addition Operator
		/// </summary>
		/// <remarks>
		///	Translates a PointF using the Width and Height
		///	properties of the given Size.
		/// </remarks>
		public static PointF operator +(PointF pt, Size sz)
		{
			return new PointF(pt.X + sz.Width, pt.Y + sz.Height);
		}

		public static PointF operator +(PointF pt, SizeF sz)
		{
			return new PointF(pt.X + sz.Width, pt.Y + sz.Height);
		}

		/// <summary>
		///	Equality Operator
		/// </summary>
		/// <remarks>
		///	Compares two PointF objects. The return value is
		///	based on the equivalence of the X and Y properties 
		///	of the two points.
		/// </remarks>
		public static bool operator ==(PointF left, PointF right)
		{
			return ((left.X == right.X) && (left.Y == right.Y));
		}

		/// <summary>
		///	Inequality Operator
		/// </summary>
		/// <remarks>
		///	Compares two PointF objects. The return value is
		///	based on the equivalence of the X and Y properties 
		///	of the two points.
		/// </remarks>
		public static bool operator !=(PointF left, PointF right)
		{
			return ((left.X != right.X) || (left.Y != right.Y));
		}

		/// <summary>
		///	Subtraction Operator
		/// </summary>
		/// <remarks>
		///	Translates a PointF using the negation of the Width 
		///	and Height properties of the given Size.
		/// </remarks>
		public static PointF operator -(PointF pt, Size sz)
		{
			return new PointF(pt.X - sz.Width, pt.Y - sz.Height);
		}

		public static PointF operator -(PointF pt, SizeF sz)
		{
			return new PointF(pt.X - sz.Width, pt.Y - sz.Height);
		}

		// -----------------------
		// Public Constructor
		// -----------------------

		/// <summary>
		///	PointF Constructor
		/// </summary>
		///
		/// <remarks>
		///	Creates a PointF from a specified x,y coordinate pair.
		/// </remarks>

		public PointF(float x, float y)
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
		public bool IsEmpty
		{
			get { return ((X == 0.0) && (Y == 0.0)); }
		}

        /// <summary>
        ///	X Property
        /// </summary>
        /// <remarks>
        ///	The X coordinate of the PointF.
        /// </remarks>
        public float X { get; set; }

        /// <summary>
        ///	Y Property
        /// </summary>
        /// <remarks>
        ///	The Y coordinate of the PointF.
        /// </remarks>
        public float Y { get; set; }

        /// <summary>
        ///	Equals Method
        /// </summary>
        /// <remarks>
        ///	Checks equivalence of this PointF and another object.
        /// </remarks>
        public override bool Equals(object obj)
		{
			if (!(obj is PointF))
				return false;

			return (this == (PointF)obj);
		}

		/// <summary>
		///	GetHashCode Method
		/// </summary>
		/// <remarks>
		///	Calculates a hashing value.
		/// </remarks>
		public override int GetHashCode()
		{
			return (int)X ^ (int)Y;
		}

		/// <summary>
		///	ToString Method
		/// </summary>
		/// <remarks>
		///	Formats the PointF as a string in coordinate notation.
		/// </remarks>
		public override string ToString()
		{
			return $"{{X={X.ToString(CultureInfo.CurrentCulture)}, Y={Y.ToString(CultureInfo.CurrentCulture)}}}";
		}

	}
}