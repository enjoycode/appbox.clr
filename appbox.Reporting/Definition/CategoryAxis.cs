using System;
using System.Xml;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// CategoryAxis definition and processing.
    ///</summary>
    [Serializable]
    internal class CategoryAxis : ReportLink
    {
        internal Axis Axis { get; set; }

        internal CategoryAxis(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            Axis = null;

            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                switch (xNodeLoop.Name)
                {
                    case "Axis":
                        Axis = new Axis(r, this, xNodeLoop);
                        break;
                    default:
                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, "Unknown CategoryAxis element '" + xNodeLoop.Name + "' ignored.");
                        break;
                }
            }
        }

        override internal void FinalPass()
        {
            if (Axis != null)
                Axis.FinalPass();
            return;
        }

    }

}
