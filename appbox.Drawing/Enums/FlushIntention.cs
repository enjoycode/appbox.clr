namespace appbox.Drawing
{
	//
	// 摘要:
	//     指定是立即终止（刷新）还是尽快执行图形堆栈中的命令。
	public enum FlushIntention
	{
		//
		// 摘要:
		//     指定立即刷新所有图形操作的堆栈。
		Flush = 0,
		//
		// 摘要:
		//     指定尽快执行堆栈上的所有图形操作。这将同步图形状态。
		Sync = 1
	}
}

