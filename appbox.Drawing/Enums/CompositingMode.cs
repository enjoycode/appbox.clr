namespace appbox.Drawing
{
	//
	// 摘要:
	//     指定源色与背景色组合的方式。
	public enum CompositingMode
	{
		//
		// 摘要:
		//     指定在呈现颜色时，它与背景色混合。该混合由所呈现的颜色的 alpha 成分确定。
		SourceOver = 0,
		//
		// 摘要:
		//     指定在呈现颜色时，它覆盖背景色。
		SourceCopy = 1
	}
}

