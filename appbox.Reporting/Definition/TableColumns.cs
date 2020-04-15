using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;

namespace appbox.Reporting.RDL
{
	///<summary>
	/// TableColumns definition and processing.
	///</summary>
	[Serializable]
	internal class TableColumns : ReportLink, IEnumerable<TableColumn>
	{
		/// <summary>
		/// list of TableColumn
		/// </summary>
		internal List<TableColumn> Items { get; }

		internal TableColumns(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			TableColumn tc;
            Items = new List<TableColumn>();
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "TableColumn":
						tc = new TableColumn(r, this, xNodeLoop);
						break;
					default:	
						tc=null;		// don't know what this is
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown TableColumns element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
				if (tc != null)
					Items.Add(tc);
			}
			if (Items.Count == 0)
				OwnerReport.rl.LogError(8, "For TableColumns at least one TableColumn is required.");
			else
                Items.TrimExcess();
		}

		internal TableColumn this[int ci]
		{
			get
			{
				return Items[ci] as TableColumn;
			}
		}
		
		override internal void FinalPass()
		{
			foreach (TableColumn tc in Items)
			{
				tc.FinalPass();
			}
			return;
		}

		internal void Run(IPresent ip, Row row)
		{
			foreach (TableColumn tc in Items)
			{
				tc.Run(ip, row);
			}
			return;
		}

		// calculate the XPositions of all the columns
		internal void CalculateXPositions(Report rpt, float startpos, Row row)
		{
			float x = startpos;

			foreach (TableColumn tc in Items)
			{
				if (tc.IsHidden(rpt, row))
					continue;
				tc.SetXPosition(rpt, x);
				x += tc.Width.Points;
			}
			return;
		}

        #region IEnumerable<TableColumn> Members
        public IEnumerator<TableColumn> GetEnumerator()
        {
            return Items.GetEnumerator();
        }
        #endregion

        #region IEnumerable Members
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Items.GetEnumerator();
        }
        #endregion
    }
}
