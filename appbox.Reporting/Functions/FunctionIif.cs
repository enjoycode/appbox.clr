using System;
using System.Collections;
using System.IO;
using System.Reflection;


using appbox.Reporting.RDL;


namespace appbox.Reporting.RDL
{
	/// <summary>
	/// iif function of the form iif(boolean, expr1, expr2)
	/// </summary>
	[Serializable]
	internal class FunctionIif : IExpr
	{
		IExpr _If;		// boolean expression
		IExpr _IfTrue;		// result if true
		IExpr _IfFalse;		// result if false

		/// <summary>
		/// Handle iif operator
		/// </summary>
		public FunctionIif(IExpr ife, IExpr ifTrue, IExpr ifFalse) 
		{
			_If = ife;
			_IfTrue = ifTrue;
			_IfFalse = ifFalse;
		}

		public TypeCode GetTypeCode()
		{
			return _IfTrue.GetTypeCode();
		}

		public bool IsConstant()
		{
			return _If.IsConstant() && _IfTrue.IsConstant() && _IfFalse.IsConstant();
		}

		public IExpr ConstantOptimization()
		{
			_If = _If.ConstantOptimization();
			_IfTrue = _IfTrue.ConstantOptimization();
			_IfFalse = _IfFalse.ConstantOptimization();

			if (_If.IsConstant())
			{
				bool result = _If.EvaluateBoolean(null, null);
				return result? _IfTrue: _IfFalse;
			}

			return this;
		}

		// Evaluate is for interpretation  (and is relatively slow)
		public object Evaluate(Report rpt, Row row)
		{
			bool result = _If.EvaluateBoolean(rpt, row);
			if (result)
				return _IfTrue.Evaluate(rpt, row);

			object o = _IfFalse.Evaluate(rpt, row);
			// We may need to convert IfFalse to same type as IfTrue
			if (_IfTrue.GetTypeCode() == _IfFalse.GetTypeCode())
				return o;

			return Convert.ChangeType(o, _IfTrue.GetTypeCode());
		}

		public bool EvaluateBoolean(Report rpt, Row row)
		{
			object result = Evaluate(rpt, row);
			return Convert.ToBoolean(result);
		}
		
		public double EvaluateDouble(Report rpt, Row row)
		{
			object result = Evaluate(rpt, row);
			return Convert.ToDouble(result);
		}
		
		public decimal EvaluateDecimal(Report rpt, Row row)
		{
			object result = Evaluate(rpt, row);
			return Convert.ToDecimal(result);
		}

        public int EvaluateInt32(Report rpt, Row row)
        {
            object result = Evaluate(rpt, row);
            return Convert.ToInt32(result);
        }

		public string EvaluateString(Report rpt, Row row)
		{
			object result = Evaluate(rpt, row);
			return Convert.ToString(result);
		}

		public DateTime EvaluateDateTime(Report rpt, Row row)
		{
			object result = Evaluate(rpt, row);
			return Convert.ToDateTime(result);
		}
	}
}
