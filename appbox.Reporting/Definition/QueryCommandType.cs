namespace appbox.Reporting.RDL
{
	///<summary>
	/// Three value state; true, false, auto (dependent on context)
	///</summary>
	internal enum QueryCommandTypeEnum
	{
		Text,
		StoredProcedure,
		TableDirect
	}

	internal class QueryCommandType
	{
		static internal QueryCommandTypeEnum GetStyle(string s, ReportLog rl)
		{
			QueryCommandTypeEnum rs;

			switch (s)
			{		
				case "Text":
					rs = QueryCommandTypeEnum.Text;
					break;
				case "StoredProcedure":
					rs = QueryCommandTypeEnum.StoredProcedure;
					break;
				case "TableDirect":
					rs = QueryCommandTypeEnum.TableDirect;
					break;
				default:		// user error just force to normal TODO
					rl.LogError(4, "Unknown Query CommandType '" + s + "'.  Text assumed.");
					rs = QueryCommandTypeEnum.Text;
					break;
			}
			return rs;
		}
	}

}
