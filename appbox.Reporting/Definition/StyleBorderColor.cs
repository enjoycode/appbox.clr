
using System;
using System.Xml;
using System.Text;
using appbox.Drawing;
using System.Globalization;

namespace appbox.Reporting.RDL
{
	///<summary>
	/// The style of the border colors.  Expressions for all sides as well as default expression.
	///</summary>
	[Serializable]
	internal class StyleBorderColor : ReportLink
	{
		/// <summary>
		/// (Color) Color of the border (unless overridden for a specific side). Default: Black.
		/// </summary>
		internal Expression Default { get; set; }

		/// <summary>
		/// (Color) Color of the left border
		/// </summary>
		internal Expression Left { get; set; }

		/// <summary>
		/// (Color) Color of the right border
		/// </summary>
		internal Expression Right { get; set; }

		/// <summary>
		/// (Color) Color of the top border
		/// </summary>
		internal Expression Top { get; set; }

		/// <summary>
		/// (Color) Color of the bottom border
		/// </summary>
		internal Expression Bottom { get; set; }

		internal StyleBorderColor(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
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
						Default = new Expression(r, this, xNodeLoop, ExpressionType.Color);
						break;
					case "Left":
						Left = new Expression(r, this, xNodeLoop, ExpressionType.Color);
						break;
					case "Right":
						Right = new Expression(r, this, xNodeLoop, ExpressionType.Color);
						break;
					case "Top":
						Top = new Expression(r, this, xNodeLoop, ExpressionType.Color);
						break;
					case "Bottom":
						Bottom = new Expression(r, this, xNodeLoop, ExpressionType.Color);
						break;
					default:
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown BorderColor element '" + xNodeLoop.Name + "' ignored.");
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
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "border-color:{0};",Default.EvaluateString(rpt, row));
			else if (bDefaults)
				sb.Append("border-color:black;");

			if (Left != null)
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "border-left:{0};",Left.EvaluateString(rpt, row));

			if (Right != null)
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "border-right:{0};",Right.EvaluateString(rpt, row));

			if (Top != null)
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "border-top:{0};",Top.EvaluateString(rpt, row));

			if (Bottom != null)
				sb.AppendFormat(NumberFormatInfo.InvariantInfo, "border-bottom:{0};",Bottom.EvaluateString(rpt, row));

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
			return "border-color:black;";
		}

        internal Color EvalDefault(Report rpt, Row r)
		{
			if (Default == null)
				return appbox.Drawing.Color.Black;
			
			string c = Default.EvaluateString(rpt, r);
			return XmlUtil.ColorFromHtml(c, appbox.Drawing.Color.Black, rpt);
		}

        internal Color EvalLeft(Report rpt, Row r)
		{
			if (Left == null)
				return EvalDefault(rpt, r);
			
			string c = Left.EvaluateString(rpt, r);
			return XmlUtil.ColorFromHtml(c, appbox.Drawing.Color.Black, rpt);
		}

        internal Color EvalRight(Report rpt, Row r)
		{
			if (Right == null)
				return EvalDefault(rpt, r);
			
			string c = Right.EvaluateString(rpt, r);
			return XmlUtil.ColorFromHtml(c, appbox.Drawing.Color.Black, rpt);
		}

        internal Color EvalTop(Report rpt, Row r)
		{
			if (Top == null)
				return EvalDefault(rpt, r);
			
			string c = Top.EvaluateString(rpt, r);
			return XmlUtil.ColorFromHtml(c, appbox.Drawing.Color.Black, rpt);
		}

        internal Color EvalBottom(Report rpt, Row r)
		{
			if (Bottom == null)
				return EvalDefault(rpt, r);
			
			string c = Bottom.EvaluateString(rpt, r);
			return XmlUtil.ColorFromHtml(c, appbox.Drawing.Color.Black, rpt);
		}
	}
}
