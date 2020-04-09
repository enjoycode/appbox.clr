
using System;


namespace appbox.Reporting.RDL
{
	///<summary>
	/// Three value state; true, false, auto (dependent on context)
	///</summary>
	internal enum TrueFalseAutoEnum
	{
		True,
		False,
		Auto
	}
	
	internal class TrueFalseAuto
	{
		static internal TrueFalseAutoEnum GetStyle(string s, ReportLog rl)
		{
			TrueFalseAutoEnum rs;

			switch (s)
			{		
				case "True":
					rs = TrueFalseAutoEnum.True;
					break;
				case "False":
					rs = TrueFalseAutoEnum.False;
					break;
				case "Auto":
					rs = TrueFalseAutoEnum.Auto;
					break;
				default:		
					rl.LogError(4, "Unknown True False Auto value of '" + s + "'.  Auto assumed.");
					rs = TrueFalseAutoEnum.Auto;
					break;
			}
			return rs;
		}
	}
}
