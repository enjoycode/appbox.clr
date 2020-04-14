using System;
using System.Xml;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// TableCell definition and processing.
    ///</summary>
    [Serializable]
    internal class TableCell : ReportLink
    {
        /// <summary>
        /// An element of the report layout (e.g. List, Textbox, Line).
        /// This ReportItems collection must contain exactly one ReportItem.
        /// The Top, Left, Height and Width for this ReportItem are ignored.
        /// The position is taken to be 0, 0 and the size to be 100%, 100%.
        /// Pagebreaks on report items inside a TableCell are ignored.
        /// </summary>
        internal ReportItems ReportItems { get; set; }

        /// <summary>
        /// Table that owns this column
        /// </summary>
        internal Table OwnerTable { get; }

        /// <summary>
        /// Indicates the number of columns this cell spans.1
        /// A ColSpan of 1 is the same as not specifying a ColSpan some bookkeeping fields
        /// </summary>
        internal int ColSpan { get; set; }

        /// <summary>
        /// Column number within table; used for
        /// xrefing with other parts of table columns; e.g. column headers with details
        /// </summary>
        internal int ColIndex { get; }

        /// <summary>
        /// true if tablecell is part of header; simplifies HTML processing
        /// </summary>
        internal bool InTableFooter { get; }

        /// <summary>
        /// true if tablecell is part of footer; simplifies HTML processing
        /// </summary>
        internal bool InTableHeader { get; }

        internal TableCell(ReportDefn r, ReportLink p, XmlNode xNode, int colIndex) : base(r, p)
        {
            ColIndex = colIndex;
            ReportItems = null;
            ColSpan = 1;

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
                    case "ColSpan":
                        ColSpan = XmlUtil.Integer(xNodeLoop.InnerText);
                        break;
                    default:
                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, "Unknown TableCell element '" + xNodeLoop.Name + "' ignored.");
                        break;
                }
            }
            // Must have exactly one ReportItems
            if (ReportItems == null)
                OwnerReport.rl.LogError(8, "ReportItems element is required with a TableCell but not specified.");
            else if (ReportItems.Items.Count != 1)
                OwnerReport.rl.LogError(8, "Only one element in ReportItems element is allowed within a TableCell.");

            // Obtain the tablecell's owner table;
            //		determine if tablecell is part of table header
            InTableHeader = false;
            ReportLink rl;
            for (rl = this.Parent; rl != null; rl = rl.Parent)
            {
                if (rl is Table)
                {
                    OwnerTable = (Table)rl;
                    break;
                }

                if (rl is Header && rl.Parent is Table) // Header and parent is Table (not TableGroup)
                {
                    InTableHeader = true;
                }

                if (rl is Footer && rl.Parent is Table) // Header and parent is Table (not TableGroup)
                {
                    InTableFooter = true;
                }
            }
            return;
        }

        override internal void FinalPass()
        {
            ReportItems.FinalPass();
            return;
        }

        internal void Run(IPresent ip, Row row)
        {
            // todo: visibility on the column should really only be evaluated once at the beginning
            //   of the table processing;  also this doesn't account for the affect of colspan correctly
            //   where if any of the spanned columns are visible the value would show??
            TableColumn tc = OwnerTable.TableColumns[ColIndex];
            if (tc.Visibility != null && tc.Visibility.IsHidden(ip.Report(), row))  // column visible?
                return;                                                 //  no nothing to do

            ip.TableCellStart(this, row);

            ReportItems.Items[0].Run(ip, row);

            ip.TableCellEnd(this, row);
            return;
        }

        internal void RunPage(Pages pgs, Row row)
        {
            // todo: visibility on the column should really only be evaluated once at the beginning
            //   of the table processing;  also this doesn't account for the affect of colspan correctly
            //   where if any of the spanned columns are visible the value would show??
            TableColumn tc = OwnerTable.TableColumns[ColIndex];
            if (tc.Visibility != null && tc.Visibility.IsHidden(pgs.Report, row))   // column visible?
                return;                                                 //  no nothing to do

            ReportItems.Items[0].RunPage(pgs, row);
            return;
        }

    }
}
