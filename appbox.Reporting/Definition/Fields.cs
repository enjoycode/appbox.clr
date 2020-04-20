using System;
using System.Collections;
using System.Collections.Specialized;
using System.Xml;

namespace appbox.Reporting.RDL
{
	///<summary>
	/// Collection of fields for a DataSet.
	///</summary>
	[Serializable]
	internal class Fields : ReportLink, ICollection
	{
		/// <summary>
		/// dictionary of items
		/// </summary>
		internal IDictionary Items { get; }

		internal Field this[string s] => Items[s] as Field;

		internal Fields(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			Field f;
			if (xNode.ChildNodes.Count < 10)
				Items = new ListDictionary();	// Hashtable is overkill for small lists
			else
				Items = new Hashtable(xNode.ChildNodes.Count);

			// Loop thru all the child nodes
			int iCol=0;
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Field":
						f = new Field(r, this, xNodeLoop);
						f.ColumnNumber = iCol++;			// Assign the column number
						break;
					default:	
						f=null;	
						r.rl.LogError(4, "Unknown element '" + xNodeLoop.Name + "' in fields list."); 
						break;
				}
				if (f != null)
				{
					if (Items.Contains(f.Name.Nm))
					{
						r.rl.LogError(4, "Field " + f.Name + " has duplicates."); 
					}
					else	
						Items.Add(f.Name.Nm, f);
				}
			}
		}

        override internal void FinalPass()
		{
			foreach (Field f in Items.Values)
			{
				f.FinalPass();
			}
			return;
		}

        #region ICollection Members
        public bool IsSynchronized => Items.Values.IsSynchronized;

        public int Count => Items.Values.Count;

        public void CopyTo(Array array, int index)
		{
			Items.Values.CopyTo(array, index);
		}

        public object SyncRoot => Items.Values.SyncRoot;
        #endregion

        #region IEnumerable Members
        public IEnumerator GetEnumerator()
		{
			return Items.Values.GetEnumerator();
		}
		#endregion

	}
}
