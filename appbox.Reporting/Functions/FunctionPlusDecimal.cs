using System;
using System.Collections;
using System.IO;
using System.Reflection;


using appbox.Reporting.RDL;


namespace appbox.Reporting.RDL
{
	/// <summary>
	/// Plus operator  of form lhs + rhs where both operands are decimal
	/// </summary>
	[Serializable]
	internal class FunctionPlusDecimal : FunctionBinary , IExpr
	{
		/// <summary>
		/// Do plus on decimal data types
		/// </summary>
		public FunctionPlusDecimal() 
		{
		}

		public FunctionPlusDecimal(IExpr lhs, IExpr rhs) 
		{
			_lhs = lhs;
			_rhs = rhs;
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.Decimal;
		}

		public IExpr ConstantOptimization()
		{
			_lhs = _lhs.ConstantOptimization();
			_rhs = _rhs.ConstantOptimization();
			bool bLeftConst = _lhs.IsConstant();
			bool bRightConst = _rhs.IsConstant();
			if (bLeftConst && bRightConst)
			{
				decimal d = EvaluateDecimal(null, null);
				return new ConstantDecimal(d);
			}
			else if (bRightConst)
			{
				decimal d = _rhs.EvaluateDecimal(null, null);
				if (d == 0m)
					return _lhs;
			}
			else if (bLeftConst)
			{
				decimal d = _lhs.EvaluateDecimal(null, null);
				if (d == 0m)
					return _rhs;
			}

			return this;
		}

		// Evaluate is for interpretation  (and is relatively slow)
		public object Evaluate(Report rpt, Row row)
		{
			return EvaluateDecimal(rpt, row);
		}
		
		public double EvaluateDouble(Report rpt, Row row)
		{
			decimal result = EvaluateDecimal(rpt, row);

			return Convert.ToDouble(result);
		}

        public int EvaluateInt32(Report rpt, Row row)
        {
            decimal result = EvaluateDecimal(rpt, row);

            return Convert.ToInt32(result);
        }

		public decimal EvaluateDecimal(Report rpt, Row row)
		{
			decimal lhs = _lhs.EvaluateDecimal(rpt, row);
			decimal rhs = _rhs.EvaluateDecimal(rpt, row);

			return (decimal) (lhs+rhs);
		}

		public string EvaluateString(Report rpt, Row row)
		{
			decimal result = EvaluateDecimal(rpt, row);
			return result.ToString();
		}

		public DateTime EvaluateDateTime(Report rpt, Row row)
		{
			decimal result = EvaluateDecimal(rpt, row);
			return Convert.ToDateTime(result);
		}

		public bool EvaluateBoolean(Report rpt, Row row)
		{
			decimal result = EvaluateDecimal(rpt, row);
			return Convert.ToBoolean(result);
		}
	}
}
