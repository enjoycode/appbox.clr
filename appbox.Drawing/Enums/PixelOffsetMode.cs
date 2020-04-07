using System;
namespace appbox.Drawing
{
	//
	// 摘要:
	//     指定在呈现期间像素偏移的方式。
	public enum PixelOffsetMode
	{
		//
		// 摘要:
		//     指定一个无效模式。
		Invalid = -1,
		//
		// 摘要:
		//     指定默认模式。
		Default = 0,
		//
		// 摘要:
		//     指定高速度、低质量呈现。
		HighSpeed = 1,
		//
		// 摘要:
		//     指定高质量、低速度呈现。
		HighQuality = 2,
		//
		// 摘要:
		//     指定没有任何像素偏移。
		None = 3,
		//
		// 摘要:
		//     指定像素在水平和垂直距离上均偏移 -.5 个单位，以进行高速锯齿消除。
		Half = 4
	}
}

