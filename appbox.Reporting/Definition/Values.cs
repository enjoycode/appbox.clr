using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// Ordered list of values used as a default for a parameter
    ///</summary>
    [Serializable]
    internal class Values : ReportLink, System.Collections.Generic.ICollection<Expression>
    {
        /// <summary>
        /// list of expression items
        /// </summary>
        internal List<Expression> Items { get; }

        internal Values(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            Expression v;
            Items = new List<Expression>();
            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                switch (xNodeLoop.Name)
                {
                    case "Value":
                        v = new Expression(r, this, xNodeLoop, ExpressionType.Variant);
                        break;
                    default:
                        v = null;
                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, $"Unknown Value element '{xNodeLoop.Name}' ignored.");
                        break;
                }
                if (v != null)
                    Items.Add(v);
            }
            if (Items.Count > 0)
                Items.TrimExcess();
        }

        // Handle parsing of function in final pass
        override internal void FinalPass()
        {
            foreach (Expression e in Items)
            {
                e.FinalPass();
            }
        }

        #region IEnumerable Members
        public IEnumerator GetEnumerator() => Items.GetEnumerator();
        #endregion

        #region ICollection<Expression> Members
        public void Add(Expression item) => Items.Add(item);

        public void Clear() => Items.Clear();

        public bool Contains(Expression item) => Items.Contains(item);

        public void CopyTo(Expression[] array, int arrayIndex) => Items.CopyTo(array, arrayIndex);

        public int Count => Items.Count;

        public bool IsReadOnly => false;

        public bool Remove(Expression item) => Items.Remove(item);
        #endregion

        #region IEnumerable<Expression> Members
        IEnumerator<Expression> IEnumerable<Expression>.GetEnumerator() => Items.GetEnumerator();
        #endregion
    }
}
