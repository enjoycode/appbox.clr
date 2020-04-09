
using System;
using System.Xml;

namespace appbox.Reporting.RDL
{
	///<summary>
	/// A value used in a filter.
	///</summary>
	[Serializable]
	internal class FilterValue : ReportLink
	{
		Expression _Expression;			// 

		internal FilterValue(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_Expression = new Expression(r,this,xNode, ExpressionType.Variant);
		}

		// Handle parsing of function in final pass
		override internal void FinalPass()
		{
			if (_Expression != null)
				_Expression.FinalPass();
			return;
		}

		internal Expression Expression
		{
			get { return  _Expression; }
			set {  _Expression = value; }
		}
	}
}
