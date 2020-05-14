using System;
using System.ComponentModel;

namespace appbox.Drawing.Printing
{

	[Serializable]
	public class Margins : ICloneable {
		int left;
		int right;
		int top;
		int bottom;

		public Margins ()
		{
			left = 100;
			right = 100;
			top = 100;
			bottom = 100;
		}

		public Margins (int left, int right, int top, int bottom)
		{
			Left = left;
			Right = right;
			Top = top;
			Bottom = bottom;
		}

		public int Left {
			get {
				return left;
			}
			set {
				if (value < 0)
					InvalidMargin ("left");
				left = value;
			}
		}

		public int Right {
			get {
				return right;
			}
			set {
				if (value < 0)
					InvalidMargin ("right");
				right = value;
			}
		}

		public int Top {
			get {
				return top;
			}
			set {
				if (value < 0)
					InvalidMargin ("top");
				top = value;
			}
		}

		public int Bottom {
			get {
				return bottom;
			}
			set {
				if (value < 0)
					InvalidMargin ("bottom");
				bottom = value;
			}
		}

		private void InvalidMargin (string property)
		{
			//string msg = Locale.GetText ("All Margins must be greater than 0");
			throw new System.ArgumentException ("All Margins must be greater than 0", property);
		}
		
		public object Clone ()
		{
			return new Margins (left, right, top, bottom);
		}

		public override bool Equals (object obj)
		{	
			return Equals (obj as Margins);
		}

		private bool Equals (Margins m)
		{
			// avoid recursion with == operator
			if ((object)m == null)
				return false;
			return ((m.Left == left) && (m.Right == right) && (m.Top == top) && (m.Bottom == bottom));
		}

		public override int GetHashCode ()
		{
			return left | (right << 8) | (right >> 24) | (top << 16) | (top >> 16) | (bottom << 24) | (bottom >> 8);
		}
		
		public override string ToString ()
		{
			string ret = "[Margins Left={0} Right={1} Top={2} Bottom={3}]";
			return String.Format (ret, left, right, top, bottom);
		}
		public static bool operator == (Margins m1, Margins m2)
		{
			// avoid recursion with == operator
			if ((object)m1 == null)
				return ((object)m2 == null);
			return m1.Equals (m2);
		}

		public static bool operator != (Margins m1, Margins m2)
		{
			// avoid recursion with == operator
			if ((object)m1 == null)
				return ((object)m2 != null);
			return !m1.Equals (m2);
		}

	}
}
