using System;

namespace appbox.Reporting.RDL
{
	///<summary>
	/// Handle the LegendLayout enumeration: Column, Row, Table
	///</summary>
	public enum LegendLayoutEnum
	{
		Column,
		Row,
		Table
	}
	public class LegendLayout
	{
        static public LegendLayoutEnum GetStyle(string s)
        {
            return LegendLayout.GetStyle(s, null);
        }

		static internal LegendLayoutEnum GetStyle(string s, ReportLog rl)
		{
			LegendLayoutEnum rs;

			switch (s)
			{		
				case "Column":
					rs = LegendLayoutEnum.Column;
					break;
				case "Row":
					rs = LegendLayoutEnum.Row;
					break;
				case "Table":
					rs = LegendLayoutEnum.Table;
					break;
				default:		
                    if (rl != null)
					    rl.LogError(4, "Unknown LegendLayout '" + s + "'.  Column assumed.");
					rs = LegendLayoutEnum.Column;
					break;
			}
			return rs;
		}
	}

}
