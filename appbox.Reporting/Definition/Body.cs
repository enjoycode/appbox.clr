using System;
using System.Xml;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// Body definition and processing.  Contains the elements of the report body.
    ///</summary>
    [Serializable]
    internal class Body : ReportLink
    {
        RSize _ColumnSpacing;
        /// <summary>
        /// Size Spacing between each column in multi-column
        /// </summary>
        internal RSize ColumnSpacing
        {
            get
            {
                if (_ColumnSpacing == null)
                    _ColumnSpacing = new RSize(OwnerReport, ".5 in");

                return _ColumnSpacing;
            }
            set { _ColumnSpacing = value; }
        }

        /// <summary>
        /// The region that contains the elements of the report body
        /// </summary>
        internal ReportItems ReportItems { get; }

        /// <summary>
        /// Height of the body
        /// </summary>
        internal RSize Height { get; set; }

        /// <summary>
        /// Number of columns for the report
        /// Default: 1. Min: 1. Max: 1000
        /// </summary>
        internal int Columns { get; set; }

        /// <summary>
        /// Default style information for the body
        /// </summary>
        internal Style Style { get; set; }

        internal Body(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            ReportItems = null;
            Height = null;
            Columns = 1;
            _ColumnSpacing = null;
            Style = null;

            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                switch (xNodeLoop.Name)
                {
                    case "ReportItems":
                        ReportItems = new ReportItems(r, this, xNodeLoop); // need a class for this
                        break;
                    case "Height":
                        Height = new RSize(r, xNodeLoop);
                        break;
                    case "Columns":
                        Columns = XmlUtil.Integer(xNodeLoop.InnerText);
                        break;
                    case "ColumnSpacing":
                        _ColumnSpacing = new RSize(r, xNodeLoop);
                        break;
                    case "Style":
                        Style = new Style(r, this, xNodeLoop);
                        break;
                    default:
                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, "Unknown Body element '" + xNodeLoop.Name + "' ignored.");
                        break;
                }
            }
            if (Height == null)
                OwnerReport.rl.LogError(8, "Body Height not specified.");
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
            ip.BodyStart(this);

            if (ReportItems != null)
                ReportItems.Run(ip, null); // not sure about the row here?

            ip.BodyEnd(this);
            return;
        }

        internal void RunPage(Pages pgs)
        {
            if (OwnerReport.Subreport == null)
            {   // Only set bottom of pages when on top level report
                pgs.BottomOfPage = OwnerReport.BottomOfPage;
                pgs.CurrentPage.YOffset = OwnerReport.TopOfPage;
            }
            this.SetCurrentColumn(pgs.Report, 0);

            if (ReportItems != null)
                ReportItems.RunPage(pgs, null, OwnerReport.LeftMargin.Points);

            return;
        }

        internal int GetCurrentColumn(Report rpt)
        {
            OInt cc = rpt.Cache.Get(this, "currentcolumn") as OInt;
            return cc == null ? 0 : cc.i;
        }

        internal int IncrCurrentColumn(Report rpt)
        {
            OInt cc = rpt.Cache.Get(this, "currentcolumn") as OInt;
            if (cc == null)
            {
                SetCurrentColumn(rpt, 0);
                cc = rpt.Cache.Get(this, "currentcolumn") as OInt;
            }
            cc.i++;
            return cc.i;
        }

        internal void SetCurrentColumn(Report rpt, int col)
        {
            OInt cc = rpt.Cache.Get(this, "currentcolumn") as OInt;
            if (cc == null)
                rpt.Cache.AddReplace(this, "currentcolumn", new OInt(col));
            else
                cc.i = col;
        }

        internal void RemoveWC(Report rpt)
        {
            if (ReportItems == null)
                return;

            foreach (ReportItem ri in this.ReportItems.Items)
            {
                ri.RemoveWC(rpt);
            }
        }
    }
}
