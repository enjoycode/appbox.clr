using System;
using System.Xml;
using System.Text;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// The width of the border.  Expressions for all sides as well as default expression.
    ///</summary>
    [Serializable]
    internal class StyleBorderWidth : ReportLink
    {
        /// <summary>
        /// (Size) Width of the border (unless overridden for a specific side)
        /// Borders are centered on the edge of the object
        /// Default: 1 pt Max: 20 pt Min: 0.25 pt
        /// </summary>
        internal Expression Default { get; set; }

        /// <summary>
        /// (Size) Width of the left border. Max: 20 pt Min: 0.25 pt
        /// </summary>
        internal Expression Left { get; set; }

        /// <summary>
        /// (Size) Width of the right border. Max: 20 pt Min: 0.25 pt
        /// </summary>
        internal Expression Right { get; set; }

        /// <summary>
        /// (Size) Width of the top border. Max: 20 pt Min: 0.25 pt
        /// </summary>
        internal Expression Top { get; set; }

        /// <summary>
        /// (Size) Width of the bottom border. Max: 20 pt Min: 0.25 pt
        /// </summary>
        internal Expression Bottom { get; set; }

        internal StyleBorderWidth(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            Default = null;
            Left = null;

            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                switch (xNodeLoop.Name)
                {
                    case "Default":
                        Default = new Expression(r, this, xNodeLoop, ExpressionType.ReportUnit);
                        break;
                    case "Left":
                        Left = new Expression(r, this, xNodeLoop, ExpressionType.ReportUnit);
                        break;
                    case "Right":
                        Right = new Expression(r, this, xNodeLoop, ExpressionType.ReportUnit);
                        break;
                    case "Top":
                        Top = new Expression(r, this, xNodeLoop, ExpressionType.ReportUnit);
                        break;
                    case "Bottom":
                        Bottom = new Expression(r, this, xNodeLoop, ExpressionType.ReportUnit);
                        break;
                    default:
                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, "Unknown BorderWidth element '" + xNodeLoop.Name + "' ignored.");
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
                sb.AppendFormat("border-width:{0};", Default.EvaluateString(rpt, row));
            else if (bDefaults)
                sb.Append("border-width:1pt;");

            if (Left != null)
                sb.AppendFormat("border-left-width:{0};", Left.EvaluateString(rpt, row));

            if (Right != null)
                sb.AppendFormat("border-right-width:{0};", Right.EvaluateString(rpt, row));

            if (Top != null)
                sb.AppendFormat("border-top-width:{0};", Top.EvaluateString(rpt, row));

            if (Bottom != null)
                sb.AppendFormat("border-bottom-width:{0};", Bottom.EvaluateString(rpt, row));

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
            return "border-width:1pt;";
        }

        internal float EvalDefault(Report rpt, Row r)   // return points
        {
            if (Default == null)
                return 1;

            string sw;
            sw = Default.EvaluateString(rpt, r);

            RSize rs = new RSize(this.OwnerReport, sw);
            return rs.Points;
        }

        internal float EvalLeft(Report rpt, Row r)  // return points
        {
            if (Left == null)
                return EvalDefault(rpt, r);

            string sw = Left.EvaluateString(rpt, r);
            RSize rs = new RSize(this.OwnerReport, sw);
            return rs.Points;
        }

        internal float EvalRight(Report rpt, Row r) // return points
        {
            if (Right == null)
                return EvalDefault(rpt, r);

            string sw = Right.EvaluateString(rpt, r);
            RSize rs = new RSize(this.OwnerReport, sw);
            return rs.Points;
        }

        internal float EvalTop(Report rpt, Row r)   // return points
        {
            if (Top == null)
                return EvalDefault(rpt, r);

            string sw = Top.EvaluateString(rpt, r);
            RSize rs = new RSize(this.OwnerReport, sw);
            return rs.Points;
        }

        internal float EvalBottom(Report rpt, Row r)    // return points
        {
            if (Bottom == null)
                return EvalDefault(rpt, r);

            string sw = Bottom.EvaluateString(rpt, r);
            RSize rs = new RSize(this.OwnerReport, sw);
            return rs.Points;
        }
    }
}
