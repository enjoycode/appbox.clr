using System;
using System.Collections.Generic;
using System.Xml;

namespace appbox.Reporting.RDL
{
	///<summary>
	/// The collection of embedded images in the Report.
	///</summary>
	[Serializable]
	internal class EmbeddedImages : ReportLink
	{
		/// <summary>
		/// list of EmbeddedImage
		/// </summary>
		internal List<EmbeddedImage> Items { get; }

		internal EmbeddedImages(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
            Items = new List<EmbeddedImage>();
			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				if (xNodeLoop.Name == "EmbeddedImage")
				{
					EmbeddedImage ei = new EmbeddedImage(r, this, xNodeLoop);
					Items.Add(ei);
				}
				else
					OwnerReport.rl.LogError(4, "Unknown Report element '" + xNodeLoop.Name + "' ignored.");
			}
			if (Items.Count == 0)
				OwnerReport.rl.LogError(8, "For EmbeddedImages at least one EmbeddedImage is required.");
			else
                Items.TrimExcess();
		}
		
		override internal void FinalPass()
		{
			foreach (EmbeddedImage ei in Items)
			{
				ei.FinalPass();
			}
			return;
		}
    }
}
