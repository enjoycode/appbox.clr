
using System;


namespace appbox.Reporting.RDL
{
	
	internal enum ThreeDPropertiesDrawingStyleEnum
	{
		Cylinder,
		Cube
	}

	internal class ThreeDPropertiesDrawingStyle
	{
		static internal ThreeDPropertiesDrawingStyleEnum GetStyle(string s, ReportLog rl)
		{
			ThreeDPropertiesDrawingStyleEnum ds;

			switch (s)
			{		
				case "Cylinder":
					ds = ThreeDPropertiesDrawingStyleEnum.Cylinder;
					break;
				case "Cube":
					ds = ThreeDPropertiesDrawingStyleEnum.Cube;
					break;
				default:	
					rl.LogError(4, "Unknown DrawingStyle '" + s + "'.  Cube assumed.");
					ds = ThreeDPropertiesDrawingStyleEnum.Cube;
					break;
			}
			return ds;
		}
	}
}
