namespace appbox.Drawing
{
	//
	// 摘要:
	//     指定在复合期间使用的质量等级。
	public enum CompositingQuality
	{
		//
		// 摘要:
		//     无效质量。
		Invalid = -1,
		//
		// 摘要:
		//     默认质量。
		Default = 0,
		//
		// 摘要:
		//     高速度、低质量。
		HighSpeed = 1,
		//
		// 摘要:
		//     高质量、低速度复合。
		HighQuality = 2,
		//
		// 摘要:
		//     使用灰度校正。
		GammaCorrected = 3,
		//
		// 摘要:
		//     假定线性值。
		AssumeLinear = 4
	}
}

