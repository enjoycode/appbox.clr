namespace appbox.Drawing
{
	//
	// 摘要:
	//     指定可用线帽样式，System.Drawing.Pen 对象以该线帽结束一段直线。
	public enum LineCap
	{
		//
		// 摘要:
		//     指定平线帽。
		Flat = 0,
		//
		// 摘要:
		//     指定方线帽。
		Square = 1,
		//
		// 摘要:
		//     指定圆线帽。
		Round = 2,
		//
		// 摘要:
		//     指定三角线帽。
		Triangle = 3,
		//
		// 摘要:
		//     指定没有锚。
		NoAnchor = 16,
		//
		// 摘要:
		//     指定方锚头帽。
		SquareAnchor = 17,
		//
		// 摘要:
		//     指定圆锚头帽。
		RoundAnchor = 18,
		//
		// 摘要:
		//     指定菱形锚头帽。
		DiamondAnchor = 19,
		//
		// 摘要:
		//     指定箭头状锚头帽。
		ArrowAnchor = 20,
		//
		// 摘要:
		//     指定用于检查线帽是否为锚头帽的掩码。
		AnchorMask = 240,
		//
		// 摘要:
		//     指定自定义线帽。
		Custom = 255
	}
}

