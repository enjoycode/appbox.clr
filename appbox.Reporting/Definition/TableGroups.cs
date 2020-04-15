using System;
using System.Collections.Generic;
using System.Xml;

namespace appbox.Reporting.RDL
{
	///<summary>
	/// TableGroups definition and processing.
	///</summary>
	[Serializable]
	internal class TableGroups : ReportLink
	{
		/// <summary>
		/// list of TableGroup entries
		/// </summary>
		internal List<TableGroup> Items { get; }

		internal TableGroups(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			TableGroup tg;
            Items = new List<TableGroup>();
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "TableGroup":
						tg = new TableGroup(r, this, xNodeLoop);
						break;
					default:	
						tg=null;		// don't know what this is
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown TableGroups element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
				if (tg != null)
					Items.Add(tg);
			}
			if (Items.Count == 0)
				OwnerReport.rl.LogError(8, "For TableGroups at least one TableGroup is required.");
			else
                Items.TrimExcess();
		}
		
		override internal void FinalPass()
		{
			foreach(TableGroup tg in Items)
			{
				tg.FinalPass();
			}

			return;
		}

		internal float DefnHeight()
		{
			float height=0;
			foreach(TableGroup tg in Items)
			{
				height += tg.DefnHeight();
			}
			return height;
		}

    }
}
