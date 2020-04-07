using System;
using System.ComponentModel;

namespace appbox.Drawing
{
    //[Serializable]
    //[ComVisible (true)]
    //[TypeConverter (typeof (RectangleConverter))]
    public struct Rectangle
    {

        /// <summary>
        ///	Empty Shared Field
        /// </summary>
        /// <remarks>
        ///	An uninitialized Rectangle Structure.
        /// </remarks>
        public static readonly Rectangle Empty;

        /// <summary>
        ///	Ceiling Shared Method
        /// </summary>
        /// <remarks>
        ///	Produces a Rectangle structure from a RectangleF 
        ///	structure by taking the ceiling of the X, Y, Width,
        ///	and Height properties.
        /// </remarks>
        public static Rectangle Ceiling(RectangleF value)
        {
            int x, y, w, h;
            checked
            {
                x = (int)Math.Ceiling(value.X);
                y = (int)Math.Ceiling(value.Y);
                w = (int)Math.Ceiling(value.Width);
                h = (int)Math.Ceiling(value.Height);
            }

            return new Rectangle(x, y, w, h);
        }

        /// <summary>
        ///	FromLTRB Shared Method
        /// </summary>
        /// <remarks>
        ///	Produces a Rectangle structure from left, top, right,
        ///	and bottom coordinates.
        /// </remarks>
        public static Rectangle FromLTRB(int left, int top, int right, int bottom)
        {
            return new Rectangle(left, top, right - left, bottom - top);
        }

        /// <summary>
        ///	Inflate Shared Method
        /// </summary>
        /// <remarks>
        ///	Produces a new Rectangle by inflating an existing 
        ///	Rectangle by the specified coordinate values.
        /// </remarks>
        public static Rectangle Inflate(Rectangle rect, int x, int y)
        {
            Rectangle r = new Rectangle(rect.Location, rect.Size);
            r.Inflate(x, y);
            return r;
        }

        /// <summary>
        ///	Inflate Method
        /// </summary>
        /// <remarks>
        ///	Inflates the Rectangle by a specified width and height.
        /// </remarks>
        public void Inflate(int width, int height)
        {
            Inflate(new Size(width, height));
        }

        /// <summary>
        ///	Inflate Method
        /// </summary>
        /// <remarks>
        ///	Inflates the Rectangle by a specified Size.
        /// </remarks>
        public void Inflate(Size size)
        {
            X -= size.Width;
            Top -= size.Height;
            Width += size.Width * 2;
            Height += size.Height * 2;
        }

        /// <summary>
        ///	Intersect Shared Method
        /// </summary>
        /// <remarks>
        ///	Produces a new Rectangle by intersecting 2 existing 
        ///	Rectangles. Returns null if there is no	intersection.
        /// </remarks>
        public static Rectangle Intersect(Rectangle a, Rectangle b)
        {
            // MS.NET returns a non-empty rectangle if the two rectangles
            // touch each other
            if (!a.IntersectsWithInclusive(b))
                return Empty;

            return Rectangle.FromLTRB(
                Math.Max(a.Left, b.Left),
                Math.Max(a.Top, b.Top),
                Math.Min(a.Right, b.Right),
                Math.Min(a.Bottom, b.Bottom));
        }

        /// <summary>
        ///	Intersect Method
        /// </summary>
        /// <remarks>
        ///	Replaces the Rectangle with the intersection of itself
        ///	and another Rectangle.
        /// </remarks>
        public void Intersect(Rectangle rect)
        {
            this = Rectangle.Intersect(this, rect);
        }

        /// <summary>
        ///	Round Shared Method
        /// </summary>
        /// <remarks>
        ///	Produces a Rectangle structure from a RectangleF by
        ///	rounding the X, Y, Width, and Height properties.
        /// </remarks>
        public static Rectangle Round(RectangleF value)
        {
            int x, y, w, h;
            checked
            {
                x = (int)Math.Round(value.X);
                y = (int)Math.Round(value.Y);
                w = (int)Math.Round(value.Width);
                h = (int)Math.Round(value.Height);
            }

            return new Rectangle(x, y, w, h);
        }

        /// <summary>
        ///	Truncate Shared Method
        /// </summary>
        /// <remarks>
        ///	Produces a Rectangle structure from a RectangleF by
        ///	truncating the X, Y, Width, and Height properties.
        /// </remarks>
        // LAMESPEC: Should this be floor, or a pure cast to int?
        public static Rectangle Truncate(RectangleF value)
        {
            int x, y, w, h;
            checked
            {
                x = (int)value.X;
                y = (int)value.Y;
                w = (int)value.Width;
                h = (int)value.Height;
            }

            return new Rectangle(x, y, w, h);
        }

        /// <summary>
        ///	Union Shared Method
        /// </summary>
        /// <remarks>
        ///	Produces a new Rectangle from the union of 2 existing 
        ///	Rectangles.
        /// </remarks>
        public static Rectangle Union(Rectangle a, Rectangle b)
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
        ///	Compares two Rectangle objects. The return value is
        ///	based on the equivalence of the Location and Size 
        ///	properties of the two Rectangles.
        /// </remarks>
        public static bool operator ==(Rectangle left, Rectangle right)
        {
            return ((left.Location == right.Location) &&
                (left.Size == right.Size));
        }

        /// <summary>
        ///	Inequality Operator
        /// </summary>
        /// <remarks>
        ///	Compares two Rectangle objects. The return value is
        ///	based on the equivalence of the Location and Size 
        ///	properties of the two Rectangles.
        /// </remarks>
        public static bool operator !=(Rectangle left, Rectangle right)
        {
            return ((left.Location != right.Location) ||
                (left.Size != right.Size));
        }

        // -----------------------
        // Public Constructors
        // -----------------------

        /// <summary>
        ///	Rectangle Constructor
        /// </summary>
        /// <remarks>
        ///	Creates a Rectangle from Point and Size values.
        /// </remarks>
        public Rectangle(Point location, Size size)
        {
            X = location.X;
            Top = location.Y;
            Width = size.Width;
            Height = size.Height;
        }

        /// <summary>
        ///	Rectangle Constructor
        /// </summary>
        /// <remarks>
        ///	Creates a Rectangle from a specified x,y location and
        ///	width and height values.
        /// </remarks>
        public Rectangle(int x, int y, int width, int height)
        {
            this.X = x;
            this.Top = y;
            this.Width = width;
            this.Height = height;
        }

        /// <summary>
        ///	Bottom Property
        /// </summary>
        /// <remarks>
        ///	The Y coordinate of the bottom edge of the Rectangle.
        ///	Read only.
        /// </remarks>
        [Browsable(false)]
        public int Bottom => Top + Height;

        /// <summary>
        ///	Height Property
        /// </summary>
        /// <remarks>
        ///	The Height of the Rectangle.
        /// </remarks>
        public int Height { get; set; }

        /// <summary>
        ///	IsEmpty Property
        /// </summary>
        /// <remarks>
        ///	Indicates if the width or height are zero. Read only.
        /// </remarks>		
        [Browsable(false)]
        public bool IsEmpty => ((X == 0) && (Top == 0) && (Width == 0) && (Height == 0));

        /// <summary>
        ///	Left Property
        /// </summary>
        /// <remarks>
        ///	The X coordinate of the left edge of the Rectangle.
        ///	Read only.
        /// </remarks>
        [Browsable(false)]
        public int Left => X;

        /// <summary>
        ///	Location Property
        /// </summary>
        /// <remarks>
        ///	The Location of the top-left corner of the Rectangle.
        /// </remarks>
        [Browsable(false)]
        public Point Location
        {
            get
            {
                return new Point(X, Top);
            }
            set
            {
                X = value.X;
                Top = value.Y;
            }
        }

        /// <summary>
        ///	Right Property
        /// </summary>
        /// <remarks>
        ///	The X coordinate of the right edge of the Rectangle.
        ///	Read only.
        /// </remarks>
        [Browsable(false)]
        public int Right => X + Width;

        /// <summary>
        ///	Size Property
        /// </summary>
        /// <remarks>
        ///	The Size of the Rectangle.
        /// </remarks>
        [Browsable(false)]
        public Size Size
        {
            get
            {
                return new Size(Width, Height);
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
        ///	The Y coordinate of the top edge of the Rectangle.
        ///	Read only.
        /// </remarks>
        [Browsable(false)]
        public int Top { get; private set; }

        /// <summary>
        ///	Width Property
        /// </summary>
        /// <remarks>
        ///	The Width of the Rectangle.
        /// </remarks>
        public int Width { get; set; }

        /// <summary>
        ///	X Property
        /// </summary>
        /// <remarks>
        ///	The X coordinate of the Rectangle.
        /// </remarks>
        public int X { get; set; }

        /// <summary>
        ///	Y Property
        /// </summary>
        /// <remarks>
        ///	The Y coordinate of the Rectangle.
        /// </remarks>
        public int Y
        {
            get { return Top; }
            set { Top = value; }
        }

        /// <summary>
        ///	Contains Method
        /// </summary>
        /// <remarks>
        ///	Checks if an x,y coordinate lies within this Rectangle.
        /// </remarks>
        public bool Contains(int x, int y)
        {
            return ((x >= Left) && (x < Right) &&
                (y >= Top) && (y < Bottom));
        }

        /// <summary>
        ///	Contains Method
        /// </summary>
        /// <remarks>
        ///	Checks if a Point lies within this Rectangle.
        /// </remarks>
        public bool Contains(Point pt)
        {
            return Contains(pt.X, pt.Y);
        }

        /// <summary>
        ///	Contains Method
        /// </summary>
        /// <remarks>
        ///	Checks if a Rectangle lies entirely within this 
        ///	Rectangle.
        /// </remarks>
        public bool Contains(Rectangle rect)
        {
            return (rect == Intersect(this, rect));
        }

        /// <summary>
        ///	Equals Method
        /// </summary>
        /// <remarks>
        ///	Checks equivalence of this Rectangle and another object.
        /// </remarks>
        public override bool Equals(object obj)
        {
            if (!(obj is Rectangle))
                return false;

            return (this == (Rectangle)obj);
        }

        /// <summary>
        ///	GetHashCode Method
        /// </summary>
        /// <remarks>
        ///	Calculates a hashing value.
        /// </remarks>
        public override int GetHashCode()
        {
            return (Height + Width) ^ X + Top;
        }

        /// <summary>
        ///	IntersectsWith Method
        /// </summary>
        /// <remarks>
        ///	Checks if a Rectangle intersects with this one.
        /// </remarks>
        public bool IntersectsWith(Rectangle rect)
        {
            return !((Left >= rect.Right) || (Right <= rect.Left) ||
                (Top >= rect.Bottom) || (Bottom <= rect.Top));
        }

        private bool IntersectsWithInclusive(Rectangle r)
        {
            return !((Left > r.Right) || (Right < r.Left) ||
                (Top > r.Bottom) || (Bottom < r.Top));
        }

        /// <summary>
        ///	Offset Method
        /// </summary>
        /// <remarks>
        ///	Moves the Rectangle a specified distance.
        /// </remarks>
        public void Offset(int x, int y)
        {
            this.X += x;
            this.Top += y;
        }

        /// <summary>
        ///	Offset Method
        /// </summary>
        /// <remarks>
        ///	Moves the Rectangle a specified distance.
        /// </remarks>
        public void Offset(Point pos)
        {
            X += pos.X;
            Top += pos.Y;
        }

        /// <summary>
        ///	ToString Method
        /// </summary>
        /// <remarks>
        ///	Formats the Rectangle as a string in (x,y,w,h) notation.
        /// </remarks>
        public override string ToString()
        {
            return $"{{X={X},Y={Top},Width={Width},Height={Height}}}";
        }

        public RectangleF ToRectangleF()
        {
            return new RectangleF(X, Y, Width, Height);
        }

    }
}