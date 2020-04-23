using System;
using System.Xml;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// For tabular reports, defines the detail rows with grouping and sorting.
    ///</summary>
    [Serializable]
    internal class Details : ReportLink
    {
        /// <summary>
        /// The details rows for the table. The details rows
        /// cannot contain any DataRegions in any of their TableCells.
        /// </summary>
        internal TableRows TableRows { get; set; }

        /// <summary>
        /// The expressions to group the detail data by
        /// </summary>
        internal Grouping Grouping { get; set; }

        /// <summary>
        /// The expressions to sort the detail data by
        /// </summary>
        internal Sorting Sorting { get; set; }

        /// <summary>
        /// Indicates if the details should be hidden
        /// </summary>
        internal Visibility Visibility { get; set; }

        /// <summary>
        /// resolved TextBox for toggling visibility
        /// </summary>
        internal Textbox ToggleTextbox { get; private set; }

        internal Table OwnerTable => (Table)Parent;

        internal Details(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            TableRows = null;
            Grouping = null;
            Sorting = null;
            Visibility = null;
            ToggleTextbox = null;

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
                    case "Grouping":
                        Grouping = new Grouping(r, this, xNodeLoop);
                        break;
                    case "Sorting":
                        Sorting = new Sorting(r, this, xNodeLoop);
                        break;
                    case "Visibility":
                        Visibility = new Visibility(r, this, xNodeLoop);
                        break;
                    default:
                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, "Unknown Details element " + xNodeLoop.Name + " ignored.");
                        break;
                }
            }
            if (TableRows == null)
                OwnerReport.rl.LogError(8, "Details requires the TableRows element.");
        }

        override internal void FinalPass()
        {
            TableRows.FinalPass();
            if (Grouping != null)
                Grouping.FinalPass();
            if (Sorting != null)
                Sorting.FinalPass();
            if (Visibility != null)
            {
                Visibility.FinalPass();
                if (Visibility.ToggleItem != null)
                {
                    ToggleTextbox = (Textbox)(OwnerReport.LUReportItems[Visibility.ToggleItem]);
                    if (ToggleTextbox != null)
                        ToggleTextbox.IsToggle = true;
                }
            }
            return;
        }

        internal void Run(IPresent ip, Rows rs, int start, int end)
        {
            // if no rows output or rows just leave
            if (rs == null || rs.Data == null)
                return;
            if (this.Visibility != null && Visibility.IsHidden(ip.Report(), rs.Data[start]) && Visibility.ToggleItem == null)
                return;                 // not visible

            for (int r = start; r <= end; r++)
            {
                TableRows.Run(ip, rs.Data[r]);
            }
            return;
        }

        internal void RunPage(Pages pgs, Rows rs, int start, int end, float footerHeight)
        {
            // if no rows output or rows just leave
            if (rs == null || rs.Data == null)
                return;

            if (Visibility != null && Visibility.IsHidden(pgs.Report, rs.Data[start]))
                return;                 // not visible

            Page p;

            Row row;
            for (int r = start; r <= end; r++)
            {
                p = pgs.CurrentPage;            // this can change after running a row
                row = rs.Data[r];
                float hrows = HeightOfRows(pgs, row);   // height of all the rows in the details
                float height = p.YOffset + hrows;

                // add the footerheight that must be on every page
                height += OwnerTable.GetPageFooterHeight(pgs, row);

                if (r == end)
                    height += footerHeight;     // on last row; may need additional room for footer
                if (height > pgs.BottomOfPage)
                {
                    OwnerTable.RunPageFooter(pgs, row, false);
                    p = OwnerTable.RunPageNew(pgs, p);
                    OwnerTable.RunPageHeader(pgs, row, false, null);
                    TableRows.RunPage(pgs, row, true);   // force checking since header + hrows might be > BottomOfPage
                }
                else
                    TableRows.RunPage(pgs, row, hrows > pgs.BottomOfPage);
            }
            return;
        }

        internal float HeightOfRows(Pages pgs, Row r)
        {
            if (Visibility != null && Visibility.IsHidden(pgs.Report, r))
            {
                return 0;
            }

            return TableRows.HeightOfRows(pgs, r);
        }

    }
}
