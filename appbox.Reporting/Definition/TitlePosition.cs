using System;

namespace appbox.Reporting.RDL
{
	///<summary>
	/// Handle title position enumeration: center, near, far.
	///</summary>
	internal enum TitlePositionEnum
	{
		Center,
		Near,
		Far
	}
	internal class TitlePosition
	{
		static internal TitlePositionEnum GetStyle(string s, ReportLog rl)
		{
			TitlePositionEnum rs;

			switch (s)
			{		
				case "Center":
					rs = TitlePositionEnum.Center;
					break;
				case "Near":
					rs = TitlePositionEnum.Near;
					break;
				case "Far":
					rs = TitlePositionEnum.Far;
					break;
				default:	
					rl.LogError(4, "Unknown TitlePosition '" + s + "'.  Center assumed.");
					rs = TitlePositionEnum.Center;
					break;
			}
			return rs;
		}
	}

}
