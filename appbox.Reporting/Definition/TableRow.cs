using System;
using System.Xml;
using System.Collections.Generic;
using appbox.Reporting.Resources;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// TableRow represents a Row in a table.  This can be part of a header, footer, or detail definition.
    ///</summary>
    [Serializable]
    internal class TableRow : ReportLink
    {
        /// <summary>
        /// Contents of the row. One cell per column
        /// </summary>
        internal TableCells TableCells { get; set; }

        /// <summary>
        /// Height of the row
        /// </summary>
        internal RSize Height { get; set; }

        /// <summary>
        /// Indicates if the row should be hidden
        /// </summary>
        internal Visibility Visibility { get; set; }

        /// <summary>
        /// indicates that row height can increase in size
        /// </summary>
        internal bool CanGrow { get; private set; }

        /// <summary>
        /// list of TextBox's that need to be checked for growth
        /// </summary>
        internal List<Textbox> GrowList { get; private set; }

        internal TableRow(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            TableCells = null;
            Height = null;
            Visibility = null;
            CanGrow = false;
            GrowList = null;

            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                switch (xNodeLoop.Name)
                {
                    case "TableCells":
                        TableCells = new TableCells(r, this, xNodeLoop);
                        break;
                    case "Height":
                        Height = new RSize(r, xNodeLoop);
                        break;
                    case "Visibility":
                        Visibility = new Visibility(r, this, xNodeLoop);
                        break;
                    default:
                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, "Unknown TableRow element '" + xNodeLoop.Name + "' ignored.");
                        break;
                }
            }
            if (TableCells == null)
                OwnerReport.rl.LogError(8, "TableRow requires the TableCells element.");
            if (Height == null)
                OwnerReport.rl.LogError(8, "TableRow requires the Height element.");
        }

        override internal void FinalPass()
        {
            TableCells.FinalPass();
            if (Visibility != null)
                Visibility.FinalPass();

            foreach (TableCell tc in TableCells.Items)
            {
                ReportItem ri = tc.ReportItems.Items[0] as ReportItem;
                if (!(ri is Textbox))
                    continue;
                Textbox tb = ri as Textbox;
                if (tb.CanGrow)
                {
                    if (this.GrowList == null)
                        GrowList = new List<Textbox>();
                    GrowList.Add(tb);
                    CanGrow = true;
                }
            }

            if (CanGrow)				// shrink down the resulting list
                GrowList.TrimExcess();

            return;
        }

        internal void Run(IPresent ip, Row row)
        {
            if (Visibility != null && Visibility.IsHidden(ip.Report(), row))
                return;

            ip.TableRowStart(this, row);
            TableCells.Run(ip, row);
            ip.TableRowEnd(this, row);
            return;
        }

        internal void RunPage(Pages pgs, Row row)
        {
            if (Visibility != null && Visibility.IsHidden(pgs.Report, row))
                return;

            TableCells.RunPage(pgs, row);

            WorkClass wc = GetWC(pgs.Report);
            pgs.CurrentPage.YOffset += wc.CalcHeight;
            return;
        }

        internal float HeightOfRow(Pages pgs, Row r)
        {
            return HeightOfRow(pgs.Report, pgs.G, r);
        }

        internal float HeightOfRow(Report rpt, Drawing.Graphics g, Row r)
        {
            WorkClass wc = GetWC(rpt);
            if (Visibility != null && Visibility.IsHidden(rpt, r))
            {
                wc.CalcHeight = 0;
                return 0;
            }

            float defnHeight = Height.Points;
            if (!CanGrow)
            {
                wc.CalcHeight = defnHeight;
                return defnHeight;
            }

            TableColumns tcs = this.Table.TableColumns;
            float height = 0;
            foreach (Textbox tb in this.GrowList)
            {
                int ci = tb.TC.ColIndex;
                if (tcs[ci].IsHidden(rpt, r))    // if column is hidden don't use in calculation
                    continue;
                height = Math.Max(height, tb.RunTextCalcHeight(rpt, g, r));
            }
            wc.CalcHeight = Math.Max(height, defnHeight);
            return wc.CalcHeight;
        }

        internal float HeightCalc(Report rpt)
        {
            WorkClass wc = GetWC(rpt);
            return wc.CalcHeight;
        }

        private Table Table
        {
            get
            {
                ReportLink p = Parent;
                while (p != null)
                {
                    if (p is Table)
                        return p as Table;
                    p = p.Parent;
                }
                throw new Exception(Strings.TableRow_Error_TableRowNotRelatedToTable);
            }
        }

        private WorkClass GetWC(Report rpt)
        {
            WorkClass wc = rpt.Cache.Get(this, "wc") as WorkClass;
            if (wc == null)
            {
                wc = new WorkClass(this);
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
            internal float CalcHeight;      // dynamic when CanGrow true
            internal WorkClass(TableRow tr)
            {
                CalcHeight = tr.Height.Points;
            }
        }
    }
}
