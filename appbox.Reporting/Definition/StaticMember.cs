
using System;
using System.Xml;

namespace appbox.Reporting.RDL
{
	///<summary>
	/// Chart static member.
	///</summary>
	[Serializable]
	internal class StaticMember : ReportLink
	{
		Expression _Label;	//(Variant) The label for the static member (displayed either on
							// the category axis or legend, as appropriate).		
	
		internal StaticMember(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_Label=null;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Label":
						_Label = new Expression(r, this, xNodeLoop, ExpressionType.Variant);
						break;
					default:
						break;
				}
			}
			if (_Label == null)
				OwnerReport.rl.LogError(8, "StaticMember requires the Label element.");
		}

		// Handle parsing of function in final pass
		override internal void FinalPass()
		{
			if (_Label != null)
				_Label.FinalPass();
			return;
		}

		internal Expression Label
		{
			get { return  _Label; }
			set {  _Label = value; }
		}
	}
}
