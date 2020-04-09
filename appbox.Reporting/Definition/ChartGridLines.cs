using System;
using System.Xml;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// ChartGridLines definition and processing.
    ///</summary>
    [Serializable]
    internal class ChartGridLines : ReportLink
    {
        /// <summary>
        /// Indicates the gridlines should be shown
        /// </summary>
        internal bool ShowGridLines { get; set; }

        /// <summary>
        /// Line style properties for the gridlines and tickmarks
        /// </summary>
        internal Style Style { get; set; }

        internal ChartGridLines(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            ShowGridLines = true;
            Style = null;

            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                switch (xNodeLoop.Name)
                {
                    case "ShowGridLines":
                        ShowGridLines = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    case "Style":
                        Style = new Style(r, this, xNodeLoop);
                        break;
                    default:    // TODO
                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, "Unknown ChartGridLines element '" + xNodeLoop.Name + "' ignored.");
                        break;
                }
            }
        }

        override internal void FinalPass()
        {
            if (Style != null)
                Style.FinalPass();
            return;
        }
    }
}
