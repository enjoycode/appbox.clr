using System;
using System.Xml;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// Definition of a subtotal column or row.
    ///</summary>
    [Serializable]
    internal class Subtotal : ReportLink
    {
        /// <summary>
        /// The header cell for a subtotal column or row.
        /// This ReportItems collection must contain
        /// exactly one Textbox. The Top, Left, Height
        /// and Width for this ReportItem are ignored.
        /// The position is taken to be 0, 0 and the size to be 100%, 100%.
        /// </summary>
        internal ReportItems ReportItems { get; set; }

        /// <summary>
        /// Style properties that override the style
        /// properties for all top-level report items
        /// contained in the subtotal column/row
        /// At Subtotal Column/Row intersections, Row style takes priority
        /// </summary>
        internal Style Style { get; set; }

        /// <summary>
        /// Before | After (default)
        /// Indicates whether this subtotal column/row
        /// should appear before (left/above) or after
        /// (right/below) the detail columns/rows.
        /// </summary>
        internal SubtotalPositionEnum Position { get; set; }

        /// <summary>
        /// The name to use for this subtotal. Default: �Total�
        /// </summary>
        internal string DataElementName { get; set; }

        /// <summary>
        /// Indicates whether the subtotal should appear in a data rendering.
        /// Default: NoOutput
        /// </summary>
        internal DataElementOutputEnum DataElementOutput { get; set; }

        internal Subtotal(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            ReportItems = null;
            Style = null;
            Position = SubtotalPositionEnum.After;
            DataElementName = "Total";
            DataElementOutput = DataElementOutputEnum.NoOutput;

            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                switch (xNodeLoop.Name)
                {
                    case "ReportItems":
                        ReportItems = new ReportItems(r, this, xNodeLoop);
                        break;
                    case "Style":
                        Style = new Style(r, this, xNodeLoop);
                        break;
                    case "Position":
                        Position = SubtotalPosition.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    case "DataElementName":
                        DataElementName = xNodeLoop.InnerText;
                        break;
                    case "DataElementOutput":
                        DataElementOutput = RDL.DataElementOutput.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    default:
                        break;
                }
            }
            if (ReportItems == null)
                OwnerReport.rl.LogError(8, "Subtotal requires the ReportItems element.");
        }

        override internal void FinalPass()
        {
            if (ReportItems != null)
                ReportItems.FinalPass();
            if (Style != null)
                Style.FinalPass();
            return;
        }

    }
}
