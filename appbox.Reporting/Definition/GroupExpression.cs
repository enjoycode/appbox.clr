using System;
using System.Xml;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// Definition of an expression within a group.
    ///</summary>
    [Serializable]
    internal class GroupExpression : ReportLink
    {
        internal Expression Expression { get; set; }

        internal GroupExpression(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            Expression = new Expression(r, this, xNode, ExpressionType.Variant);
        }

        // Handle parsing of function in final pass
        override internal void FinalPass()
        {
            if (Expression != null)
                Expression.FinalPass();
            return;
        }

    }
}
