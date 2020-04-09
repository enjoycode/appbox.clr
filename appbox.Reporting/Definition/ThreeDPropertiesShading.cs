
using System;


namespace appbox.Reporting.RDL
{

	internal enum ThreeDPropertiesShadingEnum
	{
		None,
		Simple,
		Real
	}

	internal class ThreeDPropertiesShading
	{
		static internal ThreeDPropertiesShadingEnum GetStyle(string s, ReportLog rl)
		{
			ThreeDPropertiesShadingEnum sh;

			switch (s)
			{		
				case "None":
					sh = ThreeDPropertiesShadingEnum.None;
					break;
				case "Simple":
					sh = ThreeDPropertiesShadingEnum.Simple;
					break;
				case "Real":
					sh = ThreeDPropertiesShadingEnum.Real;
					break;
				default:	
					rl.LogError(4, "Unknown Shading '" + s + "'.  None assumed.");
					sh = ThreeDPropertiesShadingEnum.None;
					break;
			}
			return sh;
		}
	}


}
