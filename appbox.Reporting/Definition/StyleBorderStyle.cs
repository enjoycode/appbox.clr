
using System;
using System.Xml;
using System.Text;

namespace appbox.Reporting.RDL
{
	///<summary>
	/// The type (dotted, solid, ...) of border.  Expressions for all sides as well as default expression.
	///</summary>
	[Serializable]
	internal class StyleBorderStyle : ReportLink
	{
		/// <summary>
		/// (Enum BorderStyle) Style of the border (unless overridden for a specific side)
		/// Default: none
		/// </summary>
		internal Expression Default { get; set; }

		/// <summary>
		/// (Enum BorderStyle) Style of the left border
		/// </summary>
		internal Expression Left { get; set; }

		/// <summary>
		/// (Enum BorderStyle) Style of the right border
		/// </summary>
		internal Expression Right { get; set; }

		/// <summary>
		/// (Enum BorderStyle) Style of the top border
		/// </summary>
		internal Expression Top { get; set; }

		/// <summary>
		/// (Enum BorderStyle) Style of the bottom border
		/// </summary>
		internal Expression Bottom { get; set; }

		internal StyleBorderStyle(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			Default=null;
			Left=null;
			Right=null;
			Top=null;
			Bottom=null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Default":
						Default = new Expression(r, this, xNodeLoop, ExpressionType.Enum);
						break;
					case "Left":
						Left = new Expression(r, this, xNodeLoop, ExpressionType.Enum);
						break;
					case "Right":
						Right = new Expression(r, this, xNodeLoop, ExpressionType.Enum);
						break;
					case "Top":
						Top = new Expression(r, this, xNodeLoop, ExpressionType.Enum);
						break;
					case "Bottom":
						Bottom = new Expression(r, this, xNodeLoop, ExpressionType.Enum);
						break;
					default:	
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown BorderStyle element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
		}

		// Handle parsing of function in final pass
		override internal void FinalPass()
		{
			if (Default != null)
				Default.FinalPass();
			if (Left != null)
				Left.FinalPass();
			if (Right != null)
				Right.FinalPass();
			if (Top != null)
				Top.FinalPass();
			if (Bottom != null)
				Bottom.FinalPass();
			return;
		}

		// Generate a CSS string from the specified styles
		internal string GetCSS(Report rpt, Row row, bool bDefaults)
		{
			StringBuilder sb = new StringBuilder();

			if (Default != null)
				sb.AppendFormat("border-style:{0};",Default.EvaluateString(rpt, row));
			else if (bDefaults)
				sb.Append("border-style:none;");

			if (Left != null)
				sb.AppendFormat("border-left-style:{0};",Left.EvaluateString(rpt, row));

			if (Right != null)
				sb.AppendFormat("border-right-style:{0};",Right.EvaluateString(rpt, row));

			if (Top != null)
				sb.AppendFormat("border-top-style:{0};",Top.EvaluateString(rpt, row));

			if (Bottom != null)
				sb.AppendFormat("border-bottom-style:{0};",Bottom.EvaluateString(rpt, row));

			return sb.ToString();
		}

		internal bool IsConstant()
		{
			bool rc = true;

			if (Default != null)
				rc = Default.IsConstant();

			if (!rc)
				return false;

			if (Left != null)
				rc = Left.IsConstant();

			if (!rc)
				return false;

			if (Right != null)
				rc = Right.IsConstant();

			if (!rc)
				return false;

			if (Top != null)
				rc = Top.IsConstant();

			if (!rc)
				return false;

			if (Bottom != null)
				rc = Bottom.IsConstant();

			return rc;
		}

		static internal string GetCSSDefaults()
		{
			return "border-style:none;";
		}

        internal BorderStyleEnum EvalDefault(Report rpt, Row r)
		{
			if (Default == null)
				return BorderStyleEnum.None;

			string bs = Default.EvaluateString(rpt, r);
			return GetBorderStyle(bs, BorderStyleEnum.Solid);
		}

        internal BorderStyleEnum EvalLeft(Report rpt, Row r)
		{
			if (Left == null)
				return EvalDefault(rpt, r);

			string bs = Left.EvaluateString(rpt, r);
			return GetBorderStyle(bs, BorderStyleEnum.Solid);
		}

        internal BorderStyleEnum EvalRight(Report rpt, Row r)
		{
			if (Right == null)
				return EvalDefault(rpt, r);

			string bs = Right.EvaluateString(rpt, r);
			return GetBorderStyle(bs, BorderStyleEnum.Solid);
		}

        internal BorderStyleEnum EvalTop(Report rpt, Row r)
		{
			if (Top == null)
				return EvalDefault(rpt, r);

			string bs = Top.EvaluateString(rpt, r);
			return GetBorderStyle(bs, BorderStyleEnum.Solid);
		}

        internal BorderStyleEnum EvalBottom(Report rpt, Row r)
		{
			if (Bottom == null)
				return EvalDefault(rpt, r);

			string bs = Bottom.EvaluateString(rpt, r);
			return GetBorderStyle(bs, BorderStyleEnum.Solid);
		}

		// return the BorderStyleEnum given a particular string value
		static internal BorderStyleEnum GetBorderStyle(string v, BorderStyleEnum def)
		{
			BorderStyleEnum bs;
			switch (v)
			{
				case "None":
					bs = BorderStyleEnum.None;
					break;
				case "Dotted":
					bs = BorderStyleEnum.Dotted;
					break;
				case "Dashed":
					bs = BorderStyleEnum.Dashed;
					break;
				case "Solid":
					bs = BorderStyleEnum.Solid;
					break;
				case "Double":
					bs = BorderStyleEnum.Double;
					break;
				case "Groove":
					bs = BorderStyleEnum.Groove;
					break;
				case "Ridge":
					bs = BorderStyleEnum.Ridge;
					break;
				case "Inset":
					bs = BorderStyleEnum.Inset;
					break;
				case "WindowInset":
					bs = BorderStyleEnum.WindowInset;
					break;
				case "Outset":
					bs = BorderStyleEnum.Outset;
					break;
				default:
					bs = def;
					break;
			}
			return bs;
		}
	}

	/// <summary>
	/// Allowed values for border styles.  Note: these may not be actually supported depending
	/// on the renderer used.
	/// </summary>
	public enum BorderStyleEnum
	{
		/// <summary>
		/// No border
		/// </summary>
		None,
		/// <summary>
		/// Dotted line border
		/// </summary>
		Dotted,
		/// <summary>
		/// Dashed lin border
		/// </summary>
		Dashed,
		/// <summary>
		/// Solid line border
		/// </summary>
		Solid,
		/// <summary>
		/// Double line border
		/// </summary>
		Double,
		/// <summary>
		/// Grooved border
		/// </summary>
		Groove,
		/// <summary>
		/// Ridge border
		/// </summary>
		Ridge,
		/// <summary>
		/// Inset border
		/// </summary>
		Inset,
		/// <summary>
		/// Windows Inset border
		/// </summary>
		WindowInset,
		/// <summary>
		/// Outset border
		/// </summary>
		Outset
	}
}
