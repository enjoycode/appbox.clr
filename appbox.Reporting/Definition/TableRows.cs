using System;
using System.Collections.Generic;
using System.Xml;
using appbox.Reporting.Resources;

namespace appbox.Reporting.RDL
{
	///<summary>
	/// TableRows definition and processing.
	///</summary>
	[Serializable]
	internal class TableRows : ReportLink
	{
		/// <summary>
		/// list of TableRow
		/// </summary>
		internal List<TableRow> Items { get; }

        private float _HeightOfRows;        // height of contained rows
        private bool _CanGrow;				// if any TableRow contains a TextBox with CanGrow

		internal TableRows(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			TableRow t;
            Items = new List<TableRow>();
			_CanGrow = false;
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "TableRow":
						t = new TableRow(r, this, xNodeLoop);
						break;
					default:	
						t=null;		// don't know what this is
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown TableRows element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
				if (t != null)
					Items.Add(t);
			}
			if (Items.Count == 0)
				OwnerReport.rl.LogError(8, "For TableRows at least one TableRow is required.");
			else
                Items.TrimExcess();
		}
		
		override internal void FinalPass()
		{
			_HeightOfRows = 0;
			foreach (TableRow t in Items)
			{
				_HeightOfRows += t.Height.Points;
				t.FinalPass();
				_CanGrow |= t.CanGrow;
			}

			return;
		}

		internal void Run(IPresent ip, Row row)
		{
			foreach (TableRow t in Items)
			{
				t.Run(ip, row);
			}
			return;
		}

		internal void RunPage(Pages pgs, Row row)
		{
			RunPage(pgs, row, false);
		}

		internal void RunPage(Pages pgs, Row row, bool bCheckRows)
		{
			if (bCheckRows)
			{	// we need to check to see if a row will fit on the page
				foreach (TableRow t in Items)
				{
					Page p = pgs.CurrentPage;			// this can change after running a row
					float hrows = t.HeightOfRow(pgs, row);	// height of this row
					float height = p.YOffset + hrows;
					if (height > pgs.BottomOfPage)
					{
						p = OwnerTable.RunPageNew(pgs, p);
						OwnerTable.RunPageHeader(pgs, row, false, null);
					}
					t.RunPage(pgs, row);
				}
			}
			else
			{	// all rows will fit on the page
				foreach (TableRow t in Items)
					t.RunPage(pgs, row);
			}
			return;
		}

		internal Table OwnerTable
		{
			get 
			{
				for (ReportLink rl = this.Parent; rl != null; rl = rl.Parent)
				{
					if (rl is Table)
						return rl as Table;
				}

				throw new Exception(Strings.TableRows_Error_TableRowsMustOwnedTable);
			}
		}

		internal float DefnHeight()
		{
			float height=0;
			foreach (TableRow tr in this.Items)
			{
				height += tr.Height.Points;
			}
			return height;
		}

		internal float HeightOfRows(Pages pgs, Row r)
		{
			if (!this._CanGrow)
				return _HeightOfRows;
			
			float height=0;
			foreach (TableRow tr in this.Items)
			{
				height += tr.HeightOfRow(pgs, r);
			}

			return Math.Max(height, _HeightOfRows);
		}

    }
}
