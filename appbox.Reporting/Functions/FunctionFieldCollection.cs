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
	/// Fields referenced dynamically (e.g. Fields(expr) are handled by this class.
	/// </summary>
	[Serializable]
	internal class FunctionFieldCollection : IExpr
	{
		private IDictionary _Fields;
		private IExpr _ArgExpr;

		/// <summary>
		/// obtain value of Field
		/// </summary>
		public FunctionFieldCollection(IDictionary fields, IExpr arg) 
		{
			_Fields = fields;
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
					throw new Exception(Strings.FunctionFieldCollection_Error_FieldCollectionNull); 
				Field f = _Fields[o] as Field;
				if (f == null)
					throw new Exception(string.Format(Strings.FunctionFieldCollection_Error_FieldCollectionInvalid, o)); 
				return new FunctionField(f);
			}

			return this;
		}

		// 
		public virtual object Evaluate(Report rpt, Row row)
		{
			if (row == null)
				return null;
			Field f;
			string field = _ArgExpr.EvaluateString(rpt, row);
			if (field == null)
				return null;
			f = _Fields[field] as Field;
			if (f == null)
				return null;

			object o;
			if (f.Value != null)
				o = f.Value.Evaluate(rpt, row);
			else
				o = row.Data[f.ColumnNumber];

			if (o == DBNull.Value)
				return null;

			if (f.RunType == TypeCode.String && o is char)	// work around; mono odbc driver confuses string and char
				o = Convert.ChangeType(o, TypeCode.String);
			
			return o;
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
