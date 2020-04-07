using System;
using System.Globalization;
using System.ComponentModel;

namespace appbox.Drawing
{
	//[Serializable]
	public struct SizeF 
	{
        /// <summary>
        ///	Empty Shared Field
        /// </summary>
        /// <remarks>
        ///	An uninitialized SizeF Structure.
        /// </remarks>
        public static readonly SizeF Empty;

		/// <summary>
		///	Addition Operator
		/// </summary>
		/// <remarks>
		///	Addition of two SizeF structures.
		/// </remarks>
		public static SizeF operator +(SizeF sz1, SizeF sz2)
		{
			return new SizeF(sz1.Width + sz2.Width, sz1.Height + sz2.Height);
		}

		/// <summary>
		///	Equality Operator
		/// </summary>
		/// <remarks>
		///	Compares two SizeF objects. The return value is
		///	based on the equivalence of the Width and Height 
		///	properties of the two Sizes.
		/// </remarks>
		public static bool operator ==(SizeF sz1, SizeF sz2)
		{
			return ((sz1.Width == sz2.Width) && (sz1.Height == sz2.Height));
		}

		/// <summary>
		///	Inequality Operator
		/// </summary>
		/// <remarks>
		///	Compares two SizeF objects. The return value is
		///	based on the equivalence of the Width and Height 
		///	properties of the two Sizes.
		/// </remarks>
		public static bool operator !=(SizeF sz1, SizeF sz2)
		{
			return ((sz1.Width != sz2.Width) || (sz1.Height != sz2.Height));
		}

		/// <summary>
		///	Subtraction Operator
		/// </summary>
		/// <remarks>
		///	Subtracts two SizeF structures.
		/// </remarks>
		public static SizeF operator -(SizeF sz1, SizeF sz2)
		{
			return new SizeF(sz1.Width - sz2.Width,
					  sz1.Height - sz2.Height);
		}

		/// <summary>
		///	SizeF to PointF Conversion
		/// </summary>
		/// <remarks>
		///	Returns a PointF based on the dimensions of a given 
		///	SizeF. Requires explicit cast.
		/// </remarks>
		public static explicit operator PointF(SizeF size)
		{
			return new PointF(size.Width, size.Height);
		}


		// -----------------------
		// Public Constructors
		// -----------------------

		/// <summary>
		///	SizeF Constructor
		/// </summary>
		/// <remarks>
		///	Creates a SizeF from a PointF value.
		/// </remarks>
		public SizeF(PointF pt)
		{
			Width = pt.X;
			Height = pt.Y;
		}

		/// <summary>
		///	SizeF Constructor
		/// </summary>
		/// <remarks>
		///	Creates a SizeF from an existing SizeF value.
		/// </remarks>
		public SizeF(SizeF size)
		{
			Width = size.Width;
			Height = size.Height;
		}

		/// <summary>
		///	SizeF Constructor
		/// </summary>
		/// <remarks>
		///	Creates a SizeF from specified dimensions.
		/// </remarks>
		public SizeF(float width, float height)
		{
			Width = width;
			Height = height;
		}

		// -----------------------
		// Public Instance Members
		// -----------------------

		/// <summary>
		///	IsEmpty Property
		/// </summary>
		/// <remarks>
		///	Indicates if both Width and Height are zero.
		/// </remarks>
		[Browsable(false)]
		public bool IsEmpty
		{
			get { return ((Width == 0.0) && (Height == 0.0)); }
		}

        /// <summary>
        ///	Width Property
        /// </summary>
        /// <remarks>
        ///	The Width coordinate of the SizeF.
        /// </remarks>
        public float Width { get; set; }

        /// <summary>
        ///	Height Property
        /// </summary>
        /// <remarks>
        ///	The Height coordinate of the SizeF.
        /// </remarks>
        public float Height { get; set; }

        /// <summary>
        ///	Equals Method
        /// </summary>
        /// <remarks>
        ///	Checks equivalence of this SizeF and another object.
        /// </remarks>
        public override bool Equals(object obj)
		{
			if (!(obj is SizeF))
				return false;

			return (this == (SizeF)obj);
		}

		/// <summary>
		///	GetHashCode Method
		/// </summary>
		/// <remarks>
		///	Calculates a hashing value.
		/// </remarks>
		public override int GetHashCode()
		{
			return (int)Width ^ (int)Height;
		}

		public PointF ToPointF()
		{
			return new PointF(Width, Height);
		}

		public Size ToSize()
		{
			int w, h;
			checked
			{
				w = (int)Width;
				h = (int)Height;
			}

			return new Size(w, h);
		}

		/// <summary>
		///	ToString Method
		/// </summary>
		/// <remarks>
		///	Formats the SizeF as a string in coordinate notation.
		/// </remarks>
		public override string ToString()
		{
			return $"{{Width={Width.ToString(CultureInfo.CurrentCulture)}, Height={Height.ToString(CultureInfo.CurrentCulture)}}}";
		}
		
	}
}
