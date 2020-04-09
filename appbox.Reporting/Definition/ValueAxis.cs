
using System;
using System.Xml;

namespace appbox.Reporting.RDL
{
	///<summary>
	/// Chart value axis definition.
	///</summary>
	[Serializable]
	internal class ValueAxis : ReportLink
	{
		Axis _Axis;		// Display properties for the value axis.		
	
		internal ValueAxis(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_Axis=null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Axis":
						_Axis = new Axis(r, this, xNodeLoop);
						break;
					default:	
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown ValueAxis element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
		}
		
		override internal void FinalPass()
		{
			if (_Axis != null)
				_Axis.FinalPass();
			return;
		}

		internal Axis Axis
		{
			get { return  _Axis; }
			set {  _Axis = value; }
		}
	}
}
