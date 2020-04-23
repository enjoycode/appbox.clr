using System;
using System.Collections.Generic;
using System.Xml;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// Collection of group expressions.
    ///</summary>
    [Serializable]
    internal class GroupExpressions : ReportLink
    {
        /// <summary>
        /// list of GroupExpression
        /// </summary>
        internal List<GroupExpression> Items { get; }

        internal GroupExpressions(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            GroupExpression g;
            Items = new List<GroupExpression>();
            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                switch (xNodeLoop.Name)
                {
                    case "GroupExpression":
                        g = new GroupExpression(r, this, xNodeLoop);
                        break;
                    default:
                        g = null;       // don't know what this is
                                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, $"Unknown GroupExpressions element '{xNodeLoop.Name}' ignored.");
                        break;
                }
                if (g != null)
                    Items.Add(g);
            }
            if (Items.Count == 0)
                OwnerReport.rl.LogError(8, "GroupExpressions require at least one GroupExpression be defined.");
            else
                Items.TrimExcess();
        }

        override internal void FinalPass()
        {
            foreach (GroupExpression g in Items)
            {
                g.FinalPass();
            }
            return;
        }

    }
}
