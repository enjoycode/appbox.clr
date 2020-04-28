using System;
using System.Xml;

namespace appbox.Reporting.RDL
{
	///<summary>
	/// Matrix row grouping definition.
	///</summary>
	[Serializable]
	internal class RowGrouping : ReportLink
	{

		/// <summary>
		/// Width of the row header
		/// </summary>
		internal RSize Width { get; set; }

		/// <summary>
		/// Dynamic row headings for this grouping
		/// </summary>
		internal DynamicRows DynamicRows { get; set; }

		/// <summary>
		/// Static row headings for this grouping
		/// </summary>
		internal StaticRows StaticRows { get; set; }

		internal RowGrouping(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			Width=null;
			DynamicRows=null;
			StaticRows=null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Width":
						Width = new RSize(r, xNodeLoop);
						break;
					case "DynamicRows":
						DynamicRows = new DynamicRows(r, this, xNodeLoop);
						break;
					case "StaticRows":
						StaticRows = new StaticRows(r, this, xNodeLoop);
						break;
					default:	
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown RowGrouping element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
			if (Width == null)
				OwnerReport.rl.LogError(8, "RowGrouping requires the Width element.");
		}
		
		override internal void FinalPass()
		{
			if (DynamicRows != null)
				DynamicRows.FinalPass();
			if (StaticRows != null)
				StaticRows.FinalPass();
			return;
		}

    }
}
