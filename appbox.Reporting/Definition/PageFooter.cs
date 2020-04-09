
using System;
using System.Xml;
using System.IO;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// Defines the page footer of the report
    ///</summary>
    [Serializable]
    internal class PageFooter : ReportLink
    {
        /// <summary>
        /// Height of the page footer
        /// </summary>
        internal RSize Height { get; set; }

        /// <summary>
        /// Indicates if the page footer should be shown on
        /// the first page of the report
        /// </summary>
        internal bool PrintOnFirstPage { get; set; }

        /// <summary>
        /// Indicates if the page footer should be shown on
        /// the last page of the report. Not used in singlepage reports.
        /// </summary>
        internal bool PrintOnLastPage { get; set; }

        /// <summary>
        /// The region that contains the elements of the footer layout
        /// No data regions or subreports are allowed in the page footer
        /// </summary>
        internal ReportItems ReportItems { get; set; }

        /// <summary>
        /// Style information for the page footer
        /// </summary>
        internal Style Style { get; set; }

        internal PageFooter(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            Height = null;
            PrintOnFirstPage = false;
            PrintOnLastPage = false;
            ReportItems = null;
            Style = null;

            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                switch (xNodeLoop.Name)
                {
                    case "Height":
                        Height = new RSize(r, xNodeLoop);
                        break;
                    case "PrintOnFirstPage":
                        PrintOnFirstPage = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    case "PrintOnLastPage":
                        PrintOnLastPage = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    case "ReportItems":
                        ReportItems = new ReportItems(r, this, xNodeLoop);
                        break;
                    case "Style":
                        Style = new Style(r, this, xNodeLoop);
                        break;
                    default:
                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, "Unknown PageFooter element '" + xNodeLoop.Name + "' ignored.");
                        break;
                }
            }
            if (Height == null)
                OwnerReport.rl.LogError(8, "PageFooter Height is required.");
        }

        override internal void FinalPass()
        {
            if (ReportItems != null)
                ReportItems.FinalPass();
            if (Style != null)
                Style.FinalPass();
            return;
        }

        internal void Run(IPresent ip, Row row)
        {
            if (OwnerReport.Subreport != null)
                return;     // don't process page footers for sub-reports
            Report rpt = ip.Report();
            rpt.TotalPages = rpt.PageNumber = 1;
            ip.PageFooterStart(this);
            if (ReportItems != null)
                ReportItems.Run(ip, row);
            ip.PageFooterEnd(this);
        }

        internal void RunPage(Pages pgs)
        {
            if (OwnerReport.Subreport != null)
                return;     // don't process page footers for sub-reports
            if (ReportItems == null)
                return;
            Report rpt = pgs.Report;

            rpt.TotalPages = pgs.PageCount;
            for (int i = 0; i < rpt.TotalPages; i++)
            {

                rpt.CurrentPage = pgs[i];       // needs to know for page header/footer expr processing
                pgs[i].YOffset = OwnerReport.PageHeight.Points
                                    - OwnerReport.BottomMargin.Points
                                    - this.Height.Points;
                pgs[i].XOffset = 0;
                pgs.CurrentPage = pgs[i];
                rpt.PageNumber = pgs[i].PageNumber;
                if (pgs[i].PageNumber == 1 && pgs.Count > 1 && !PrintOnFirstPage)
                    continue;		// Don't put footer on the first page
                if (pgs[i].PageNumber == pgs.Count && !PrintOnLastPage)
                    continue;       // Don't put footer on the last page
                ReportItems.RunPage(pgs, null, OwnerReport.LeftMargin.Points);
            }
        }
    }
}
