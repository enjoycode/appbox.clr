using System;
using System.Xml;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// Definition of the header rows for a table.
    ///</summary>
    [Serializable]
    internal class Header : ReportLink, ICacheData
    {
        /// <summary>
        /// The header rows for the table or group
        /// </summary>
        internal TableRows TableRows { get; set; }

        /// <summary>
        /// Indicates this header should be displayed on
        /// each page that the table (or group) is displayed	
        /// </summary>
        internal bool RepeatOnNewPage { get; set; }

        internal Header(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            TableRows = null;
            RepeatOnNewPage = false;

            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                switch (xNodeLoop.Name)
                {
                    case "TableRows":
                        TableRows = new TableRows(r, this, xNodeLoop);
                        break;
                    case "RepeatOnNewPage":
                        RepeatOnNewPage = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    default:
                        break;
                }
            }
            if (TableRows == null)
                OwnerReport.rl.LogError(8, "Header requires the TableRows element.");
        }

        override internal void FinalPass()
        {
            TableRows.FinalPass();

            OwnerReport.DataCache.Add(this);
            return;
        }

        internal void Run(IPresent ip, Row row)
        {
            TableRows.Run(ip, row);
            return;
        }

        internal void RunPage(Pages pgs, Row row)
        {
            WorkClass wc = this.GetValue(pgs.Report);

            if (wc.OutputRow == row && wc.OutputPage == pgs.CurrentPage)
                return;

            Page p = pgs.CurrentPage;

            float height = p.YOffset + HeightOfRows(pgs, row);
            height += OwnerTable.GetPageFooterHeight(pgs, row);
            if (height > pgs.BottomOfPage)
            {
                Table t = OwnerTable;
                t.RunPageFooter(pgs, row, false);
                p = t.RunPageNew(pgs, p);
                t.RunPageHeader(pgs, row, false, null);
                if (this.RepeatOnNewPage)
                    return;     // should already be on the page
            }

            TableRows.RunPage(pgs, row);
            wc.OutputRow = row;
            wc.OutputPage = pgs.CurrentPage;
            return;
        }

        internal Table OwnerTable
        {
            get
            {
                for (ReportLink rl = this.Parent; rl != null; rl = rl.Parent)
                {
                    if (rl is Table)
                        return rl as Table;
                }

                return null;
            }
        }

        internal float HeightOfRows(Pages pgs, Row r)
        {
            return TableRows.HeightOfRows(pgs, r);
        }
        
        #region ====ICacheData Members====
        public void ClearCache(Report rpt)
        {
            rpt.Cache.Remove(this, "wc");
        }
        #endregion

        private WorkClass GetValue(Report rpt)
        {
            WorkClass wc = rpt.Cache.Get(this, "wc") as WorkClass;
            if (wc == null)
            {
                wc = new WorkClass();
                rpt.Cache.Add(this, "wc", wc);
            }
            return wc;
        }

        private void SetValue(Report rpt, WorkClass w)
        {
            rpt.Cache.AddReplace(this, "wc", w);
        }

        class WorkClass
        {
            internal Row OutputRow;     // the previous outputed row
            internal Page OutputPage;   // the previous outputed row
            internal WorkClass()
            {
                OutputRow = null;
                OutputPage = null;
            }
        }
    }
}
