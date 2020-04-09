using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Globalization;
using appbox.Reporting.Resources;
using appbox.Reporting.RDL;


namespace appbox.Reporting.RDL
{
	/// <summary>
	/// Parameters referenced via the collection.  For example, Parameters(expr)
	/// </summary>
	[Serializable]
	internal class FunctionParameterCollection : IExpr
	{
		private IDictionary _Parameters;
		private IExpr _ArgExpr;

		/// <summary>
		/// obtain value of Field
		/// </summary>
		public FunctionParameterCollection(IDictionary parameters, IExpr arg) 
		{
			_Parameters = parameters;
			_ArgExpr = arg;
		}

		public virtual TypeCode GetTypeCode()
		{
			return TypeCode.Object;		// we don't know the typecode until we run the function
		}

		public virtual bool IsConstant()
		{
			return false;
		}

		public virtual IExpr ConstantOptimization()
		{	
			_ArgExpr = _ArgExpr.ConstantOptimization();

			if (_ArgExpr.IsConstant())
			{
				string o = _ArgExpr.EvaluateString(null, null);
				if (o == null)
					throw new Exception(Strings.FunctionParameterCollection_Error_ParameterCollectionNull); 
				ReportParameter rp = _Parameters[o] as ReportParameter;
				if (rp == null)
					throw new Exception(string.Format(Strings.FunctionParameterCollection_Error_ParameterCollectionInvalid, o)); 
				return new FunctionReportParameter(rp);
			}

			return this;
		}

		// 
		public virtual object Evaluate(Report rpt, Row row)
		{
			string o = _ArgExpr.EvaluateString(rpt, row);
			if (o == null)
				return null; 
			ReportParameter rp = _Parameters[o] as ReportParameter;
			if (rp == null)
                return null;

			return rp.GetRuntimeValue(rpt);
		}
		
		public virtual double EvaluateDouble(Report rpt, Row row)
		{
			if (row == null)
				return Double.NaN;
			return Convert.ToDouble(Evaluate(rpt, row), NumberFormatInfo.InvariantInfo);
		}
		
		public virtual decimal EvaluateDecimal(Report rpt, Row row)
		{
			if (row == null)
				return decimal.MinValue;
			return Convert.ToDecimal(Evaluate(rpt, row), NumberFormatInfo.InvariantInfo);
		}

        public virtual int EvaluateInt32(Report rpt, Row row)
        {
            if (row == null)
                return int.MinValue;
            return Convert.ToInt32(Evaluate(rpt, row), NumberFormatInfo.InvariantInfo);
        }

		public virtual string EvaluateString(Report rpt, Row row)
		{
			if (row == null)
				return null;
			return Convert.ToString(Evaluate(rpt, row));
		}

		public virtual DateTime EvaluateDateTime(Report rpt, Row row)
		{
			if (row == null)
				return DateTime.MinValue;
			return Convert.ToDateTime(Evaluate(rpt, row));
		}

		public virtual bool EvaluateBoolean(Report rpt, Row row)
		{
			if (row == null)
				return false;
			return Convert.ToBoolean(Evaluate(rpt, row));
		}
	}
}
