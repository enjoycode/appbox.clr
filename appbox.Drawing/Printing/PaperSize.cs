﻿using System;

namespace appbox.Drawing.Printing
{
	/// <summary>
	/// Summary description for PaperSize.
	/// </summary>
	[Serializable]
	public class PaperSize
	{
		string name;
		int width;
		int height;
		PaperKind kind;
		internal bool is_default;


		public PaperSize() { }


		public PaperSize(string name, int width, int height)
		{
			this.width = width;
			this.height = height;
			this.name = name;
		}

		internal PaperSize(string name, int width, int height, PaperKind kind, bool isDefault)
		{
			this.width = width;
			this.height = height;
			this.name = name;
			this.is_default = isDefault;
		}

		public int Width
		{
			get
			{
				return width;
			}
			set
			{
				if (kind != PaperKind.Custom)
					throw new ArgumentException();
				width = value;
			}
		}
		public int Height
		{
			get
			{
				return height;
			}
			set
			{
				if (kind != PaperKind.Custom)
					throw new ArgumentException();
				height = value;
			}
		}

		public string PaperName
		{
			get
			{
				return name;
			}
			set
			{
				if (kind != PaperKind.Custom)
					throw new ArgumentException();
				name = value;
			}
		}

		public PaperKind Kind
		{
			get
			{
				// .net ignores the values that are less than 0
				// the value returned is not used internally, however.
				if (kind > PaperKind.PrcEnvelopeNumber10Rotated)
					return PaperKind.Custom;

				return kind;
			}
		}

		public int RawKind {
			get {
				return (int)kind;
			}
			set {
				kind = (PaperKind)value;
			}
		}

		internal bool IsDefault
		{
			get { return this.is_default; }
			set { this.is_default = value; }
		}


		internal void SetKind(PaperKind k) { kind = k; }

		public override string ToString()
		{
			string ret = "[PaperSize {0} Kind={1} Height={2} Width={3}]";
			return String.Format(ret, this.PaperName, this.Kind, this.Height, this.Width);
		}
	}
}
