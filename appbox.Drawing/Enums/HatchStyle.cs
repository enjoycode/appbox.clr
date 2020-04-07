namespace appbox.Drawing
{
	//
	// 摘要:
	//     指定可用于 System.Drawing.Drawing2D.HatchBrush 对象的不同图案。
	public enum HatchStyle
	{
		//
		// 摘要:
		//     水平线的图案。
		Horizontal = 0,
		//
		// 摘要:
		//     Specifies hatch style System.Drawing.Drawing2D.HatchStyle.Horizontal.
		Min = 0,
		//
		// 摘要:
		//     垂直线的图案。
		Vertical = 1,
		//
		// 摘要:
		//     从左上到右下的对角线的线条图案。
		ForwardDiagonal = 2,
		//
		// 摘要:
		//     从右上到左下的对角线的线条图案。
		BackwardDiagonal = 3,
		//
		// 摘要:
		//     指定交叉的水平线和垂直线。
		Cross = 4,
		//
		// 摘要:
		//     指定阴影样式 System.Drawing.Drawing2D.HatchStyle.Cross。
		LargeGrid = 4,
		//
		// 摘要:
		//     Specifies hatch style System.Drawing.Drawing2D.HatchStyle.SolidDiamond.
		Max = 4,
		//
		// 摘要:
		//     交叉对角线的图案。
		DiagonalCross = 5,
		//
		// 摘要:
		//     指定 5% 阴影。前景色与背景色的比例为 5:100。
		Percent05 = 6,
		//
		// 摘要:
		//     指定 10% 阴影。前景色与背景色的比例为 10:100。
		Percent10 = 7,
		//
		// 摘要:
		//     指定 20% 阴影。前景色与背景色的比例为 20:100。
		Percent20 = 8,
		//
		// 摘要:
		//     指定 25% 阴影。前景色与背景色的比例为 25:100。
		Percent25 = 9,
		//
		// 摘要:
		//     指定 30% 阴影。前景色与背景色的比例为 30:100。
		Percent30 = 10,
		//
		// 摘要:
		//     指定 40% 阴影。前景色与背景色的比例为 40:100。
		Percent40 = 11,
		//
		// 摘要:
		//     指定 50% 阴影。前景色与背景色的比例为 50:100。
		Percent50 = 12,
		//
		// 摘要:
		//     指定 60% 阴影。前景色与背景色的比例为 60:100。
		Percent60 = 13,
		//
		// 摘要:
		//     指定 70% 阴影。前景色与背景色的比例为 70:100。
		Percent70 = 14,
		//
		// 摘要:
		//     指定 75% 阴影。前景色与背景色的比例为 75:100。
		Percent75 = 15,
		//
		// 摘要:
		//     指定 80% 阴影。前景色与背景色的比例为 80:100。
		Percent80 = 16,
		//
		// 摘要:
		//     指定 90% 阴影。前景色与背景色的比例为 90:100。
		Percent90 = 17,
		//
		// 摘要:
		//     指定从顶点到底点向右倾斜的对角线，其两边夹角比 System.Drawing.Drawing2D.HatchStyle.ForwardDiagonal 小
		//     50%，但它们不是锯齿消除的。
		LightDownwardDiagonal = 18,
		//
		// 摘要:
		//     指定从顶点到底点向左倾斜的对角线，其两边夹角比 System.Drawing.Drawing2D.HatchStyle.BackwardDiagonal
		//     小 50%，但这些直线不是锯齿消除的。
		LightUpwardDiagonal = 19,
		//
		// 摘要:
		//     指定从顶点到底点向右倾斜的对角线，其两边夹角比 System.Drawing.Drawing2D.HatchStyle.ForwardDiagonal 小
		//     50%，宽度是其两倍。此阴影图案不是锯齿消除的。
		DarkDownwardDiagonal = 20,
		//
		// 摘要:
		//     指定从顶点到底点向左倾斜的对角线，其两边夹角比 System.Drawing.Drawing2D.HatchStyle.BackwardDiagonal
		//     小 50%，宽度是其两倍，但这些直线不是锯齿消除的。
		DarkUpwardDiagonal = 21,
		//
		// 摘要:
		//     指定从顶点到底点向右倾斜的对角线，其间距与阴影样式 System.Drawing.Drawing2D.HatchStyle.ForwardDiagonal
		//     相同，宽度是其三倍，但它们不是锯齿消除的。
		WideDownwardDiagonal = 22,
		//
		// 摘要:
		//     指定从顶点到底点向左倾斜的对角线，其间距与阴影样式 System.Drawing.Drawing2D.HatchStyle.BackwardDiagonal
		//     相同，宽度是其三倍，但它们不是锯齿消除的。
		WideUpwardDiagonal = 23,
		//
		// 摘要:
		//     指定垂直线的两边夹角比 System.Drawing.Drawing2D.HatchStyle.Vertical 小 50%。
		LightVertical = 24,
		//
		// 摘要:
		//     指定水平线，其两边夹角比 System.Drawing.Drawing2D.HatchStyle.Horizontal 小 50%。
		LightHorizontal = 25,
		//
		// 摘要:
		//     指定垂直线的两边夹角比阴影样式 System.Drawing.Drawing2D.HatchStyle.Vertical 小 75%（或者比 System.Drawing.Drawing2D.HatchStyle.LightVertical
		//     小 25%）。
		NarrowVertical = 26,
		//
		// 摘要:
		//     指定水平线的两边夹角比阴影样式 System.Drawing.Drawing2D.HatchStyle.Horizontal 小 75%（或者比 System.Drawing.Drawing2D.HatchStyle.LightHorizontal
		//     小 25%）。
		NarrowHorizontal = 27,
		//
		// 摘要:
		//     指定垂直线的两边夹角比 System.Drawing.Drawing2D.HatchStyle.Vertical 小 50% 并且宽度是其两倍。
		DarkVertical = 28,
		//
		// 摘要:
		//     指定水平线的两边夹角比 System.Drawing.Drawing2D.HatchStyle.Horizontal 小 50% 并且宽度是 System.Drawing.Drawing2D.HatchStyle.Horizontal
		//     的两倍。
		DarkHorizontal = 29,
		//
		// 摘要:
		//     指定虚线对角线，这些对角线从顶点到底点向右倾斜。
		DashedDownwardDiagonal = 30,
		//
		// 摘要:
		//     指定虚线对角线，这些对角线从顶点到底点向左倾斜。
		DashedUpwardDiagonal = 31,
		//
		// 摘要:
		//     指定虚线水平线。
		DashedHorizontal = 32,
		//
		// 摘要:
		//     指定虚线垂直线。
		DashedVertical = 33,
		//
		// 摘要:
		//     指定带有五彩纸屑外观的阴影。
		SmallConfetti = 34,
		//
		// 摘要:
		//     指定具有五彩纸屑外观的阴影，并且它是由比 System.Drawing.Drawing2D.HatchStyle.SmallConfetti 更大的片构成的。
		LargeConfetti = 35,
		//
		// 摘要:
		//     指定由 Z 字形构成的水平线。
		ZigZag = 36,
		//
		// 摘要:
		//     指定由代字号“~”构成的水平线。
		Wave = 37,
		//
		// 摘要:
		//     指定具有分层砖块外观的阴影，它从顶点到底点向左倾斜。
		DiagonalBrick = 38,
		//
		// 摘要:
		//     指定具有水平分层砖块外观的阴影。
		HorizontalBrick = 39,
		//
		// 摘要:
		//     指定具有织物外观的阴影。
		Weave = 40,
		//
		// 摘要:
		//     指定具有格子花呢材料外观的阴影。
		Plaid = 41,
		//
		// 摘要:
		//     指定具有草皮层外观的阴影。
		Divot = 42,
		//
		// 摘要:
		//     指定互相交叉的水平线和垂直线，每一直线都是由点构成的。
		DottedGrid = 43,
		//
		// 摘要:
		//     指定互相交叉的正向对角线和反向对角线，每一对角线都是由点构成的。
		DottedDiamond = 44,
		//
		// 摘要:
		//     指定带有对角分层鹅卵石外观的阴影，它从顶点到底点向右倾斜。
		Shingle = 45,
		//
		// 摘要:
		//     指定具有格架外观的阴影。
		Trellis = 46,
		//
		// 摘要:
		//     指定具有球体彼此相邻放置的外观的阴影。
		Sphere = 47,
		//
		// 摘要:
		//     指定互相交叉的水平线和垂直线，其两边夹角比阴影样式 System.Drawing.Drawing2D.HatchStyle.Cross 小 50%。
		SmallGrid = 48,
		//
		// 摘要:
		//     指定带有棋盘外观的阴影。
		SmallCheckerBoard = 49,
		//
		// 摘要:
		//     指定具有棋盘外观的阴影，棋盘所具有的方格大小是 System.Drawing.Drawing2D.HatchStyle.SmallCheckerBoard
		//     大小的两倍。
		LargeCheckerBoard = 50,
		//
		// 摘要:
		//     指定互相交叉的正向对角线和反向对角线，但这些对角线不是锯齿消除的。
		OutlinedDiamond = 51,
		//
		// 摘要:
		//     指定具有对角放置的棋盘外观的阴影。
		SolidDiamond = 52
	}
}

