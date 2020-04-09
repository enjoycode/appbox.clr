
using System;


namespace appbox.Reporting.RDL
{
	
	internal enum PlotTypeEnum
	{
		Auto,
		Line
	}

	internal class PlotType
	{
		static internal PlotTypeEnum GetStyle(string s, ReportLog rl)
		{
			PlotTypeEnum pt;

			switch (s)
			{		
				case "Auto":
					pt = PlotTypeEnum.Auto;
					break;
				case "Line":
					pt = PlotTypeEnum.Line;
					break;
				default:		
					rl.LogError(4, "Unknown PlotType '" + s + "'.  Auto assumed.");
					pt = PlotTypeEnum.Auto;
					break;
			}
			return pt;
		}
	}


}
