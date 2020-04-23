using System;
using System.Xml;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// The default value for a parameter.
    ///</summary>
    [Serializable]
    internal class DefaultValue : ReportLink
    {
        // Only one of Values and DataSetReference can be specified.

        /// <summary>
        /// The query to execute to obtain the default value(s) for the parameter.
        /// The default is the first value of the ValueField.
        /// </summary>
        internal DataSetReference DataSetReference { get; set; }

        /// <summary>
        /// The default values for the parameter
        /// </summary>
        internal Values Values { get; set; }

        internal DefaultValue(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            DataSetReference = null;
            Values = null;

            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                switch (xNodeLoop.Name)
                {
                    case "DataSetReference":
                        DataSetReference = new DataSetReference(r, this, xNodeLoop);
                        break;
                    case "Values":
                        Values = new Values(r, this, xNodeLoop);
                        break;
                    default:
                        break;
                }
            }
        }

        override internal void FinalPass()
        {
            if (DataSetReference != null)
                DataSetReference.FinalPass();
            if (Values != null)
                Values.FinalPass();
            return;
        }

        internal object[] GetValue(Report rpt)
        {
            if (Values != null)
                return ValuesCalc(rpt);
            object[] dValues = this.GetDataValues(rpt);
            if (dValues != null)
                return dValues;

            string[] dsValues;
            if (DataSetReference != null)
                DataSetReference.SupplyValues(rpt, out dsValues, out dValues);

            this.SetDataValues(rpt, dValues);
            return dValues;
        }

        internal object[] ValuesCalc(Report rpt)
        {
            if (Values == null)
                return null;
            object[] result = new object[Values.Count];
            int index = 0;
            foreach (Expression v in Values)
            {
                result[index++] = v.Evaluate(rpt, null);
            }
            return result;
        }

        private object[] GetDataValues(Report rpt)
        {
            return rpt.Cache.Get(this, "datavalues") as object[];
        }

        private void SetDataValues(Report rpt, object[] vs)
        {
            if (vs == null)
                rpt.Cache.Remove(this, "datavalues");
            else
                rpt.Cache.AddReplace(this, "datavalues", vs);
        }
    }
}
