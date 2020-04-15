using System;
using System.Xml;

namespace appbox.Reporting.RDL
{
    ///<summary>
    ///  Definition of footer rows for a table or group.
    ///</summary>
    [Serializable]
    internal class Footer : ReportLink
    {
        /// <summary>
        /// The footer rows for the table or group
        /// </summary>
        internal TableRows TableRows { get; set; }

        /// <summary>
        /// Indicates this footer should be displayed on
        /// each page that the table (or group) is displayed
        /// </summary>
        internal bool RepeatOnNewPage { get; set; }

        internal Footer(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
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
                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, "Unknown Footer element '" + xNodeLoop.Name + "' ignored.");
                        break;
                }
            }
            if (TableRows == null)
                OwnerReport.rl.LogError(8, "TableRows element is required with a Footer but not specified.");
        }

        override internal void FinalPass()
        {
            TableRows.FinalPass();
            return;
        }

        internal void Run(IPresent ip, Row row)
        {
            TableRows.Run(ip, row);
            return;
        }

        internal void RunPage(Pages pgs, Row row)
        {

            Page p = pgs.CurrentPage;
            if (p.YOffset + HeightOfRows(pgs, row) > pgs.BottomOfPage)
            {
                p = OwnerTable.RunPageNew(pgs, p);
                OwnerTable.RunPageHeader(pgs, row, false, null);
            }
            TableRows.RunPage(pgs, row);

            return;
        }

        internal float HeightOfRows(Pages pgs, Row r)
        {
            return TableRows.HeightOfRows(pgs, r);
        }

        internal Table OwnerTable
        {
            get
            {
                ReportLink rl = this.Parent;
                while (rl != null)
                {
                    if (rl is Table)
                        return rl as Table;
                    rl = rl.Parent;
                }
                return null;
            }
        }
    }
}
