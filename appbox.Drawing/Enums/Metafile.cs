namespace appbox.Drawing
{
	public enum EmfType
	{
		//
		// 摘要:
		//     指定图元文件中的所有记录都是 EMF 记录，它们可通过 GDI 或 GDI+ 来显示。
		EmfOnly = 3,
		//
		// 摘要:
		//     指定图元文件中的所有记录都是 EMF+ 记录，它们可通过 GDI+ 显示而不能通过 GDI 显示。
		EmfPlusOnly = 4,
		//
		// 摘要:
		//     指定图元文件中所有的 EMF+ 记录都与一个替换的 EMF 记录相关联。System.Drawing.Imaging.EmfType.EmfPlusDual
		//     类型的图元文件可通过 GDI 或 GDI+ 来显示。
		EmfPlusDual = 5
	}
}

