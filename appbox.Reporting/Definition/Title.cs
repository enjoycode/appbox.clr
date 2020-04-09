
using System;
using System.Xml;

namespace appbox.Reporting.RDL
{
	///<summary>
	/// Chart (or axis) title definition.
	///</summary>
	[Serializable]
	internal class Title : ReportLink
	{
		Expression _Caption;	//(string) Caption of the title
		Style _Style;			// Defines text, border and background style
								// properties for the title.
								// All Textbox properties apply.
		TitlePositionEnum _Position;	// The position of the title; Default: center
	
		internal Title(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_Caption=null;
			_Style=null;
			_Position=TitlePositionEnum.Center;

			// Loop thru all the child nodes
			foreach(XmlNode xNodeLoop in xNode.ChildNodes)
			{
				if (xNodeLoop.NodeType != XmlNodeType.Element)
					continue;
				switch (xNodeLoop.Name)
				{
					case "Caption":
						_Caption = new Expression(r, this, xNodeLoop, ExpressionType.String);
						break;
					case "Style":
						_Style = new Style(r, this, xNodeLoop);
						break;
					case "Position":
						_Position = TitlePosition.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
						break;
					default:
						// don't know this element - log it
						OwnerReport.rl.LogError(4, "Unknown Title element '" + xNodeLoop.Name + "' ignored.");
						break;
				}
			}
		}

		// Handle parsing of function in final pass
		override internal void FinalPass()
		{
			if (_Caption != null)
				_Caption.FinalPass();
			if (_Style != null)
				_Style.FinalPass();
			return;
		}

		internal Expression Caption
		{
			get { return  _Caption; }
			set {  _Caption = value; }
		}

		internal Style Style
		{
			get { return  _Style; }
			set {  _Style = value; }
		}

		internal TitlePositionEnum Position
		{
			get { return  _Position; }
			set {  _Position = value; }
		}
	}
}
