using System;
using System.Collections;
using System.Collections.Specialized;
using System.Xml;

namespace appbox.Reporting.RDL
{
	///<summary>
	/// The sets of data (defined by DataSet) that are retrieved as part of the Report.
	///</summary>
	[Serializable]
	internal class DataSetsDefn : ReportLink
	{
        /// <summary>
        /// list of report items
        /// </summary>
        internal IDictionary Items { get; }

        internal DataSetsDefn(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
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
				if (xNodeLoop.Name == "DataSet")
				{
					DataSetDefn ds = new DataSetDefn(r, this, xNodeLoop);
					if (ds != null && ds.Name != null)
						Items.Add(ds.Name.Nm, ds);
				}
			}
		}

        internal DataSetDefn this[string name] => Items[name] as DataSetDefn;

        override internal void FinalPass()
		{
			foreach (DataSetDefn ds in Items.Values)
			{
				ds.FinalPass();
			}
			return;
		}

		internal bool GetData(Report rpt)
		{
            bool haveRows = false;
			foreach (DataSetDefn ds in Items.Values)
			{
				haveRows |= ds.GetData(rpt);
			}

			return haveRows;
		}

	}
}
