using System;
using System.Xml;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// TableColumn definition and processing.
    ///</summary>
    [Serializable]
    internal class TableColumn : ReportLink
    {
        /// <summary>
        ///  Width of the column
        /// </summary>
        internal RSize Width { get; set; }

        /// <summary>
        /// Indicates if the column should be hidden	
        /// </summary>
        internal Visibility Visibility { get; set; }

        private bool _FixedHeader = false; // Header of this column should be display even when scrolled

        internal TableColumn(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            Width = null;
            Visibility = null;

            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                switch (xNodeLoop.Name)
                {
                    case "Width":
                        Width = new RSize(r, xNodeLoop);
                        break;
                    case "Visibility":
                        Visibility = new Visibility(r, this, xNodeLoop);
                        break;
                    case "FixedHeader":
                        _FixedHeader = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    default:
                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, "Unknown TableColumn element '" + xNodeLoop.Name + "' ignored.");
                        break;
                }
            }
            if (Width == null)
                OwnerReport.rl.LogError(8, "TableColumn requires the Width element.");
        }

        override internal void FinalPass()
        {
            if (Visibility != null)
                Visibility.FinalPass();
            return;
        }

        internal void Run(IPresent ip, Row row)
        {
        }

        internal float GetXPosition(Report rpt)
        {
            WorkClass wc = GetWC(rpt);
            return wc.XPosition;
        }

        internal void SetXPosition(Report rpt, float xp)
        {
            WorkClass wc = GetWC(rpt);
            wc.XPosition = xp;
        }

        internal bool IsHidden(Report rpt, Row r)
        {
            if (Visibility == null)
                return false;
            return Visibility.IsHidden(rpt, r);
        }

        private WorkClass GetWC(Report rpt)
        {
            if (rpt == null)
                return new WorkClass();

            WorkClass wc = rpt.Cache.Get(this, "wc") as WorkClass;
            if (wc == null)
            {
                wc = new WorkClass();
                rpt.Cache.Add(this, "wc", wc);
            }
            return wc;
        }

        private void RemoveWC(Report rpt)
        {
            rpt.Cache.Remove(this, "wc");
        }

        class WorkClass
        {
            internal float XPosition;   // Set at runtime by Page processing; potentially dynamic at runtime
                                        //  since visibility is an expression
            internal WorkClass()
            {
                XPosition = 0;
            }
        }
    }
}
