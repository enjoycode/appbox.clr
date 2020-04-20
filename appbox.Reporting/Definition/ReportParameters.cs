using System;
using System.Collections;
using System.Collections.Specialized;
using System.Xml;

namespace appbox.Reporting.RDL
{
	///<summary>
	/// Collection of report parameters.
	///</summary>
	[Serializable]
	internal class ReportParameters : ReportLink, ICollection
	{
		/// <summary>
		/// list of report items
		/// </summary>
		internal IDictionary Items { get; }

		internal ReportParameters(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			if (xNode.ChildNodes.Count < 10)
				Items = new ListDictionary();	// Hashtable is overkill for small lists
			else
				Items = new Hashtable(xNode.ChildNodes.Count);

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				if (xNodeLoop.Name == "ReportParameter")
				{
					ReportParameter rp = new ReportParameter(r, this, xNodeLoop);
                    if (rp.Name != null)
					    Items.Add(rp.Name.Nm, rp);
				}
				else
					OwnerReport.rl.LogError(4, "Unknown ReportParameters element '" + xNodeLoop.Name + "' ignored.");
			}
		}
		
		internal void SetRuntimeValues(Report rpt, IDictionary parms)
		{
			// Fill the values to use in the report parameters
			foreach (string pname in parms.Keys)	// Loop thru the passed parameters
			{
				ReportParameter rp = (ReportParameter) Items[pname];
				if (rp == null)
				{	// When not found treat it as a warning message
					if (!pname.StartsWith("rs:"))	// don't care about report server parameters
						rpt.rl.LogError(4, "Unknown ReportParameter passed '" + pname + "' ignored.");
					continue;
				}

                // Search for the valid values
                object parmValue = parms[pname];
                if (parmValue is string && rp.ValidValues != null)
                {
                    string[] dvs = rp.ValidValues.DisplayValues(rpt);
                    if (dvs != null && dvs.Length > 0)
                    {
                        for (int i = 0; i < dvs.Length; i++)
                        {
                            if (dvs[i] == (string) parmValue)
                            {
                                object[] dv = rp.ValidValues.DataValues(rpt);
                                parmValue = dv[i];
                                break;
                            }
                        }
                    }
                }
				rp.SetRuntimeValue(rpt, parmValue);
			}
		}

		override internal void FinalPass()
		{
			foreach (ReportParameter rp in Items.Values)
			{
				rp.FinalPass();
			}
			return;
		}

        #region ICollection Members
        public bool IsSynchronized => Items.IsSynchronized;

        public int Count => Items.Count;

        public void CopyTo(Array array, int index) => Items.CopyTo(array, index);

        public object SyncRoot => Items.SyncRoot;
        #endregion

        #region IEnumerable Members
        public IEnumerator GetEnumerator()
		{
			return Items.Values.GetEnumerator();
		}
		#endregion
	}
}
