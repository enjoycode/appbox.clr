using System;
using System.Xml;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// TableGroup definition and processing.
    ///</summary>
    [Serializable]
    internal class TableGroup : ReportLink
    {
        /// <summary>
        /// The expressions to group the data by.
        /// </summary>
        internal Grouping Grouping { get; set; }

        /// <summary>
        /// The expressions to sort the data by.
        /// </summary>
        internal Sorting Sorting { get; set; }

        /// <summary>
        /// A group header row.
        /// </summary>
        internal Header Header { get; set; }

        internal int HeaderCount => Header == null ? 0 : Header.TableRows.Items.Count;

        /// <summary>
        /// A group footer row.
        /// </summary>
        internal Footer Footer { get; set; }

        internal int FooterCount => Footer == null ? 0 : Footer.TableRows.Items.Count;

        /// <summary>
        /// Indicates if the group (and all groups embedded within it) should be hidden.	
        /// </summary>
        internal Visibility Visibility { get; set; }

        /// <summary>
        /// resolved TextBox for toggling visibility
        /// </summary>
        internal Textbox ToggleTextbox { get; private set; }

        internal TableGroup(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            Grouping = null;
            Sorting = null;
            Header = null;
            Footer = null;
            Visibility = null;
            ToggleTextbox = null;

            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                switch (xNodeLoop.Name)
                {
                    case "Grouping":
                        Grouping = new Grouping(r, this, xNodeLoop);
                        break;
                    case "Sorting":
                        Sorting = new Sorting(r, this, xNodeLoop);
                        break;
                    case "Header":
                        Header = new Header(r, this, xNodeLoop);
                        break;
                    case "Footer":
                        Footer = new Footer(r, this, xNodeLoop);
                        break;
                    case "Visibility":
                        Visibility = new Visibility(r, this, xNodeLoop);
                        break;
                    default:
                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, "Unknown TableGroup element '" + xNodeLoop.Name + "' ignored.");
                        break;
                }
            }
            if (Grouping == null)
                OwnerReport.rl.LogError(8, "TableGroup requires the Grouping element.");
        }

        override internal void FinalPass()
        {
            if (Grouping != null)
                Grouping.FinalPass();
            if (Sorting != null)
                Sorting.FinalPass();
            if (Header != null)
                Header.FinalPass();
            if (Footer != null)
                Footer.FinalPass();
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

        internal float DefnHeight()
        {
            float height = 0;
            if (Header != null)
                height += Header.TableRows.DefnHeight();

            if (Footer != null)
                height += Footer.TableRows.DefnHeight();

            return height;
        }

    }
}
