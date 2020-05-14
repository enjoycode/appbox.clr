﻿using System;
using System.ComponentModel;

namespace appbox.Drawing
{
    //[Serializable]
    public struct RectangleF
    {

        /// <summary>
        ///	Empty Shared Field
        /// </summary>
        /// <remarks>
        ///	An uninitialized RectangleF Structure.
        /// </remarks>
        public static readonly RectangleF Empty;

        /// <summary>
        ///	FromLTRB Shared Method
        /// </summary>
        /// <remarks>
        ///	Produces a RectangleF structure from left, top, right,
        ///	and bottom coordinates.
        /// </remarks>
        public static RectangleF FromLTRB(float left, float top,
                           float right, float bottom)
        {
            return new RectangleF(left, top, right - left, bottom - top);
        }

        /// <summary>
        ///	Inflate Shared Method
        /// </summary>
        /// <remarks>
        ///	Produces a new RectangleF by inflating an existing 
        ///	RectangleF by the specified coordinate values.
        /// </remarks>
        public static RectangleF Inflate(RectangleF rect,
                          float x, float y)
        {
            RectangleF ir = new RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
            ir.Inflate(x, y);
            return ir;
        }

        /// <summary>
        ///	Inflate Method
        /// </summary>
        /// <remarks>
        ///	Inflates the RectangleF by a specified width and height.
        /// </remarks>
        public void Inflate(float x, float y)
        {
            Inflate(new SizeF(x, y));
        }

        /// <summary>
        ///	Inflate Method
        /// </summary>
        /// <remarks>
        ///	Inflates the RectangleF by a specified Size.
        /// </remarks>
        public void Inflate(SizeF size)
        {
            X -= size.Width;
            Y -= size.Height;
            Width += size.Width * 2;
            Height += size.Height * 2;
        }

        /// <summary>
        ///	Intersect Shared Method
        /// </summary>
        /// <remarks>
        ///	Produces a new RectangleF by intersecting 2 existing 
        ///	RectangleFs. Returns null if there is no intersection.
        /// </remarks>
        public static RectangleF Intersect(RectangleF a, RectangleF b)
        {
            // MS.NET returns a non-empty rectangle if the two rectangles
            // touch each other
            if (!a.IntersectsWithInclusive(b))
                return Empty;

            return FromLTRB(
                Math.Max(a.Left, b.Left),
                Math.Max(a.Top, b.Top),
                Math.Min(a.Right, b.Right),
                Math.Min(a.Bottom, b.Bottom));
        }

        /// <summary>
        ///	Intersect Method
        /// </summary>
        /// <remarks>
        ///	Replaces the RectangleF with the intersection of itself
        ///	and another RectangleF.
        /// </remarks>
        public void Intersect(RectangleF rect)
        {
            this = RectangleF.Intersect(this, rect);
        }

        /// <summary>
        ///	Union Shared Method
        /// </summary>
        /// <remarks>
        ///	Produces a new RectangleF from the union of 2 existing 
        ///	RectangleFs.
        /// </remarks>
        public static RectangleF Union(RectangleF a, RectangleF b)
        {
            return FromLTRB(Math.Min(a.Left, b.Left),
                     Math.Min(a.Top, b.Top),
                     Math.Max(a.Right, b.Right),
                     Math.Max(a.Bottom, b.Bottom));
        }

        /// <summary>
        ///	Equality Operator
        /// </summary>
        /// <remarks>
        ///	Compares two RectangleF objects. The return value is
        ///	based on the equivalence of the Location and Size 
        ///	properties of the two RectangleFs.
        /// </remarks>
        public static bool operator ==(RectangleF left, RectangleF right)
        {
            return (left.X == right.X) && (left.Y == right.Y) &&
                                (left.Width == right.Width) && (left.Height == right.Height);
        }

        /// <summary>
        ///	Inequality Operator
        /// </summary>
        /// <remarks>
        ///	Compares two RectangleF objects. The return value is
        ///	based on the equivalence of the Location and Size 
        ///	properties of the two RectangleFs.
        /// </remarks>
        public static bool operator !=(RectangleF left, RectangleF right)
        {
            return (left.X != right.X) || (left.Y != right.Y) ||
                                (left.Width != right.Width) || (left.Height != right.Height);
        }

        /// <summary>
        ///	Rectangle to RectangleF Conversion
        /// </summary>
        /// <remarks>
        ///	Converts a Rectangle object to a RectangleF.
        /// </remarks>
        public static implicit operator RectangleF(Rectangle r)
        {
            return new RectangleF(r.X, r.Y, r.Width, r.Height);
        }


        // -----------------------
        // Public Constructors
        // -----------------------

        /// <summary>
        ///	RectangleF Constructor
        /// </summary>
        /// <remarks>
        ///	Creates a RectangleF from PointF and SizeF values.
        /// </remarks>
        public RectangleF(PointF location, SizeF size)
        {
            X = location.X;
            Y = location.Y;
            Width = size.Width;
            Height = size.Height;
        }

        /// <summary>
        ///	RectangleF Constructor
        /// </summary>
        /// <remarks>
        ///	Creates a RectangleF from a specified x,y location and
        ///	width and height values.
        /// </remarks>
        public RectangleF(float x, float y, float width, float height)
        {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
        }

        /// <summary>
        ///	Bottom Property
        /// </summary>
        /// <remarks>
        ///	The Y coordinate of the bottom edge of the RectangleF.
        ///	Read only.
        /// </remarks>
        public float Bottom => Y + Height;

        /// <summary>
        ///	Height Property
        /// </summary>
        /// <remarks>
        ///	The Height of the RectangleF.
        /// </remarks>
        public float Height { get; set; }

        /// <summary>
        ///	IsEmpty Property
        /// </summary>
        /// <remarks>
        ///	Indicates if the width or height are zero. Read only.
        /// </remarks>
        public bool IsEmpty => (Width <= 0 || Height <= 0);

        /// <summary>
        ///	Left Property
        /// </summary>
        /// <remarks>
        ///	The X coordinate of the left edge of the RectangleF.
        ///	Read only.
        /// </remarks>
        public float Left => X;

        /// <summary>
        ///	Location Property
        /// </summary>
        /// <remarks>
        ///	The Location of the top-left corner of the RectangleF.
        /// </remarks>
        public PointF Location
        {
            get
            {
                return new PointF(X, Y);
            }
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        /// <summary>
        ///	Right Property
        /// </summary>
        /// <remarks>
        ///	The X coordinate of the right edge of the RectangleF.
        ///	Read only.
        /// </remarks>
        public float Right => X + Width;

        /// <summary>
        ///	Size Property
        /// </summary>
        /// <remarks>
        ///	The Size of the RectangleF.
        /// </remarks>
        public SizeF Size
        {
            get
            {
                return new SizeF(Width, Height);
            }
            set
            {
                Width = value.Width;
                Height = value.Height;
            }
        }

        /// <summary>
        ///	Top Property
        /// </summary>
        /// <remarks>
        ///	The Y coordinate of the top edge of the RectangleF.
        ///	Read only.
        /// </remarks>
        public float Top => Y;

        /// <summary>
        ///	Width Property
        /// </summary>
        /// <remarks>
        ///	The Width of the RectangleF.
        /// </remarks>
        public float Width { get; set; }

        /// <summary>
        ///	X Property
        /// </summary>
        /// <remarks>
        ///	The X coordinate of the RectangleF.
        /// </remarks>
        public float X { get; set; }

        /// <summary>
        ///	Y Property
        /// </summary>
        /// <remarks>
        ///	The Y coordinate of the RectangleF.
        /// </remarks>
        public float Y { get; set; }

        /// <summary>
        ///	Contains Method
        /// </summary>
        /// <remarks>
        ///	Checks if an x,y coordinate lies within this RectangleF.
        /// </remarks>
        public bool Contains(float x, float y)
        {
            return ((x >= Left) && (x < Right) &&
                (y >= Top) && (y < Bottom));
        }

        /// <summary>
        ///	Contains Method
        /// </summary>
        /// <remarks>
        ///	Checks if a Point lies within this RectangleF.
        /// </remarks>
        public bool Contains(PointF pt)
        {
            return Contains(pt.X, pt.Y);
        }

        /// <summary>
        ///	Contains Method
        /// </summary>
        /// <remarks>
        ///	Checks if a RectangleF lies entirely within this 
        ///	RectangleF.
        /// </remarks>
        public bool Contains(RectangleF rect)
        {
            return X <= rect.X && Right >= rect.Right && Y <= rect.Y && Bottom >= rect.Bottom;
        }

        /// <summary>
        ///	Equals Method
        /// </summary>
        /// <remarks>
        ///	Checks equivalence of this RectangleF and an object.
        /// </remarks>
        public override bool Equals(object obj)
        {
            if (!(obj is RectangleF))
                return false;

            return (this == (RectangleF)obj);
        }

        /// <summary>
        ///	GetHashCode Method
        /// </summary>
        /// <remarks>
        ///	Calculates a hashing value.
        /// </remarks>
        public override int GetHashCode()
        {
            return (int)(X + Y + Width + Height);
        }

        /// <summary>
        ///	IntersectsWith Method
        /// </summary>
        /// <remarks>
        ///	Checks if a RectangleF intersects with this one.
        /// </remarks>
        public bool IntersectsWith(RectangleF rect)
        {
            return !((Left >= rect.Right) || (Right <= rect.Left) ||
                (Top >= rect.Bottom) || (Bottom <= rect.Top));
        }

        private bool IntersectsWithInclusive(RectangleF r)
        {
            return !((Left > r.Right) || (Right < r.Left) ||
                (Top > r.Bottom) || (Bottom < r.Top));
        }

        /// <summary>
        ///	Offset Method
        /// </summary>
        /// <remarks>
        ///	Moves the RectangleF a specified distance.
        /// </remarks>
        public void Offset(float x, float y)
        {
            X += x;
            Y += y;
        }

        /// <summary>
        ///	Offset Method
        /// </summary>
        /// <remarks>
        ///	Moves the RectangleF a specified distance.
        /// </remarks>
        public void Offset(PointF pos)
        {
            Offset(pos.X, pos.Y);
        }

        /// <summary>
        ///	ToString Method
        /// </summary>
        /// <remarks>
        ///	Formats the RectangleF in (x,y,w,h) notation.
        /// </remarks>
        public override string ToString()
        {
            return $"{{X={X},Y={Y},Width={Width},Height={Height}}}";
        }

    }
}
