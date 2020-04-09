
using System;
using System.Xml;

namespace appbox.Reporting.RDL
{
	///<summary>
	/// Matrix static row definition.
	///</summary>
	[Serializable]
	internal class StaticRow : ReportLink
	{
		ReportItems _ReportItems;	// The elements of the row header layout
							// This ReportItems collection must contain exactly one
							// ReportItem. The Top, Left, Height and Width for this
							// ReportItem are ignored. The position is taken to be 0,
							// 0 and the size to be 100%, 100%.		
	
		internal StaticRow(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_ReportItems=null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "ReportItems":
						_ReportItems = new ReportItems(r, this, xNodeLoop);
						break;
					default:
						break;
				}
			}
			if (_ReportItems == null)
				OwnerReport.rl.LogError(8, "StaticRow requires the ReportItems element.");
		}
		
		override internal void FinalPass()
		{
			if (_ReportItems != null)
				_ReportItems.FinalPass();
			return;
		}

		internal ReportItems ReportItems
		{
			get { return  _ReportItems; }
			set {  _ReportItems = value; }
		}
	}
}
