
using System;


namespace appbox.Reporting.RDL
{
	internal enum ThreeDPropertiesProjectionModeEnum
	{
		Perspective,
		Orthographic
	}

	internal class ThreeDPropertiesProjectionMode
	{
		static internal ThreeDPropertiesProjectionModeEnum GetStyle(string s)
		{
			ThreeDPropertiesProjectionModeEnum pm;

			switch (s)
			{		
				case "Perspective":
					pm = ThreeDPropertiesProjectionModeEnum.Perspective;
					break;
				case "Orthographic":
					pm = ThreeDPropertiesProjectionModeEnum.Orthographic;
					break;
				default:
					pm = ThreeDPropertiesProjectionModeEnum.Perspective;
					break;
			}
			return pm;
		}
	}


}
