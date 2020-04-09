
using System;

namespace appbox.Reporting.RDL
{
	///<summary>
	/// Handle MarkerType enumeration: Square, circle, ...
	///</summary>
	internal enum MarkerTypeEnum
	{
		None,
		Square,
		Circle,
		Diamond,
		Triangle,
		Cross,
		Auto
	}
	internal class MarkerType
	{
		static internal MarkerTypeEnum GetStyle(string s, ReportLog rl)
		{
			MarkerTypeEnum rs;

			switch (s)
			{		
				case "None":
					rs = MarkerTypeEnum.None;
					break;
				case "Square":
					rs = MarkerTypeEnum.Square;
					break;
				case "Circle":
					rs = MarkerTypeEnum.Circle;
					break;
				case "Diamond":
					rs = MarkerTypeEnum.Diamond;
					break;
				case "Triangle":
					rs = MarkerTypeEnum.Triangle;
					break;
				case "Cross":
					rs = MarkerTypeEnum.Cross;
					break;
				case "Auto":
					rs = MarkerTypeEnum.Auto;
					break;
				default:		
					rl.LogError(4, "Unknown MarkerType '" + s + "'.  None assumed.");
					rs = MarkerTypeEnum.None;
					break;
			}
			return rs;
		}
	}

}
