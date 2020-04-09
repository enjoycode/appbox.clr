// 20022008 AJM GJL - Added Second Y axis support
using System;
using System.Xml;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// Chart series definition and processing.
    ///</summary>
    [Serializable]
    internal class ChartSeries : ReportLink
    {
        /// <summary>
        /// Data points within a series
        /// </summary>
        internal DataPoints Datapoints { get; set; }

        /// <summary>
        /// Indicates whether the series should be plotted
        /// as a line in a Column chart. If set to auto,
        /// should be plotted per the primary chart type.
        /// Auto (Default) | Line	
        /// </summary>
        internal PlotTypeEnum PlotType { get; set; }

        internal string Colour { get; set; }

        /// <summary>
        /// Indicates if the series uses the left or right axis. GJL 140208
        /// </summary>
        internal string YAxis { get; set; }

        /// <summary>
        /// Indicates if the series should not show its plot markers. GJL 300508
        /// </summary>
        internal bool NoMarker { get; set; }

        internal string LineSize { get; set; }

        internal ChartSeries(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            Datapoints = null;
            PlotType = PlotTypeEnum.Auto;
            YAxis = "Left";
            NoMarker = false;
            LineSize = "Regular";

            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                switch (xNodeLoop.Name)
                {
                    case "DataPoints":
                        Datapoints = new DataPoints(r, this, xNodeLoop);
                        break;
                    case "PlotType":
                        PlotType = RDL.PlotType.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    case "YAxis":
                        YAxis = xNodeLoop.InnerText;
                        break;
                    case "NoMarker":
                        NoMarker = bool.Parse(xNodeLoop.InnerText);
                        break;
                    case "LineSize":
                        LineSize = xNodeLoop.InnerText;
                        break;
                    case "Color":
                    case "Colour":
                        Colour = xNodeLoop.InnerText;
                        break;
                    default:
                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, "Unknown ChartSeries element '" + xNodeLoop.Name + "' ignored.");
                        break;
                }
            }
            if (Datapoints == null)
                OwnerReport.rl.LogError(8, "ChartSeries requires the DataPoints element.");
        }

        override internal void FinalPass()
        {
            if (Datapoints != null)
                Datapoints.FinalPass();
            return;
        }
        
    }
}
