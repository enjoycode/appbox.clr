
using System;
using System.Xml;

namespace appbox.Reporting.RDL
{
	///<summary>
	/// Definition of an expression within a group.
	///</summary>
	[Serializable]
	internal class GroupExpression : ReportLink
	{
		Expression _Expression;			// 

		internal GroupExpression(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
		{
			_Expression = new Expression(r,this,xNode,ExpressionType.Variant);
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
