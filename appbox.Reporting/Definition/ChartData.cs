using System;
using System.Collections.Generic;
using System.Xml;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// ChartData definition and processing.
    ///</summary>
    [Serializable]
    internal class ChartData : ReportLink
    {
        /// <summary>
        /// list of chart series
        /// </summary>
        internal List<ChartSeries> Items { get; }

        internal ChartData(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            ChartSeries cs;
            Items = new List<ChartSeries>();
            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                switch (xNodeLoop.Name)
                {
                    case "ChartSeries":
                        cs = new ChartSeries(r, this, xNodeLoop);
                        break;
                    default:
                        cs = null;      // don't know what this is
                                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, "Unknown ChartData element '" + xNodeLoop.Name + "' ignored.");
                        break;
                }
                if (cs != null)
                    Items.Add(cs);
            }
            if (Items.Count == 0)
                OwnerReport.rl.LogError(8, "For ChartData at least one ChartSeries is required.");
            else
                Items.TrimExcess();
        }

        override internal void FinalPass()
        {
            foreach (ChartSeries cs in Items)
            {
                cs.FinalPass();
            }
            return;
        }

    }
}
