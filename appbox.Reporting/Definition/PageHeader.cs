using System;
using System.Xml;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// Defines the page header of the report
    ///</summary>
    [Serializable]
    internal class PageHeader : ReportLink
    {
        /// <summary>
        /// Height of the page header
        /// </summary>
        internal RSize Height { get; set; }

        /// <summary>
        /// Indicates if the page header should be shown on
        /// the first page of the report
        /// </summary>
        internal bool PrintOnFirstPage { get; set; }

        /// <summary>
        /// Indicates if the page header should be shown on
        /// the last page of the report. Not used in singlepage reports.
        /// </summary>
        internal bool PrintOnLastPage { get; set; }

        /// <summary>
        /// The region that contains the elements of the header layout
        /// No data regions or subreports are allowed in the page header
        /// </summary>
        internal ReportItems ReportItems { get; set; }

        /// <summary>
        /// Style information for the page header
        /// </summary>
        internal Style Style { get; set; }

        internal PageHeader(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
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
                        OwnerReport.rl.LogError(4, "Unknown PageHeader element '" + xNodeLoop.Name + "' ignored.");
                        break;
                }
            }
            if (Height == null)
                OwnerReport.rl.LogError(8, "PageHeader Height is required.");
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
                return;     // don't process page headers for sub-reports
            Report rpt = ip.Report();
            rpt.TotalPages = rpt.PageNumber = 1;
            ip.PageHeaderStart(this);
            if (ReportItems != null)
                ReportItems.Run(ip, row);
            ip.PageHeaderEnd(this);
        }

        internal void RunPage(Pages pgs)
        {
            if (OwnerReport.Subreport != null)
                return;     // don't process page headers for sub-reports
            if (ReportItems == null)
                return;

            Report rpt = pgs.Report;
            rpt.TotalPages = pgs.PageCount;
            foreach (Page p in pgs)
            {
                rpt.CurrentPage = p;        // needs to know for page header/footer expr processing
                p.YOffset = OwnerReport.TopMargin.Points;
                p.XOffset = 0;
                pgs.CurrentPage = p;
                rpt.PageNumber = p.PageNumber;
                if (p.PageNumber == 1 && pgs.Count > 1 && !PrintOnFirstPage)
                    continue;       // Don't put header on the first page
                if (p.PageNumber == pgs.Count && !PrintOnLastPage)
                    continue;       // Don't put header on the last page
                ReportItems.RunPage(pgs, null, OwnerReport.LeftMargin.Points);
            }
        }
    }
}
