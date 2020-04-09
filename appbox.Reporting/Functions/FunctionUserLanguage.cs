using System;
using System.IO;
using System.Globalization;
using appbox.Reporting.Resources;
using appbox.Reporting.RDL;


namespace appbox.Reporting.RDL
{
	/// <summary>
	/// The Language field in the User collection.
	/// </summary>
	[Serializable]
	internal class FunctionUserLanguage : IExpr
	{
		/// <summary>
		/// Client user language
		/// </summary>
		public FunctionUserLanguage() 
		{
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.String;
		}

		public bool IsConstant()
		{
			return false;
		}

		public IExpr ConstantOptimization()
		{	
			return this;
		}

		// Evaluate is for interpretation  
		public object Evaluate(Report rpt, Row row)
		{
			return EvaluateString(rpt, row);
		}
		
		public double EvaluateDouble(Report rpt, Row row)
		{	
			throw new Exception(Strings.FunctionUserLanguage_Error_ConvertToDouble);
		}
		
		public decimal EvaluateDecimal(Report rpt, Row row)
		{
			throw new Exception(Strings.FunctionUserLanguage_Error_ConvertToDecimal);
		}

        public int EvaluateInt32(Report rpt, Row row)
        {
            throw new Exception(Strings.FunctionUserLanguage_Error_ConvertToInt32);
        }
		public string EvaluateString(Report rpt, Row row)
		{
			if (rpt == null || rpt.ClientLanguage == null)
				return CultureInfo.CurrentCulture.ThreeLetterISOLanguageName;
			else
				return rpt.ClientLanguage;
		}

		public DateTime EvaluateDateTime(Report rpt, Row row)
		{
			throw new Exception(Strings.FunctionUserLanguage_Error_ConvertToDateTime);
		}

		public bool EvaluateBoolean(Report rpt, Row row)
		{
			throw new Exception(Strings.FunctionUserLanguage_Error_ConvertToBoolean);
		}
	}
}
