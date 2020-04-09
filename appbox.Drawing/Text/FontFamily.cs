using System;

namespace appbox.Drawing
{
	public class FontFamily
	{
		public string Name;

		public FontFamily()
		{
		}

		public FontFamily(string name)
		{
			Name = name;
		}

		/// <summary>
		/// 指定的FontStyle枚举是否可用
		/// </summary>
		/// <returns><c>true</c>, if style available was ised, <c>false</c> otherwise.</returns>
		/// <param name="style">Style.</param>
		public bool IsStyleAvailable(FontStyle style)
		{
			return false; //todo
		}
	}
}

