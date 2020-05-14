
using System;

namespace appbox.Drawing.Printing
{

	/// <summary>
	/// Summary description for PrintEventHandler.
	/// </summary>
	public delegate void PrintEventHandler(object sender, PrintEventArgs e);

	/// <summary>
	/// Summary description for PrintEventArgs.
	/// </summary>
	public class PrintEventArgs : System.ComponentModel.CancelEventArgs
	{
		private GraphicsPrinter graphics_context;
#if NET_2_0
		internal PrintAction action;
#endif		
		
		public PrintEventArgs()
		{
		}
#if NET_2_0
		public PrintAction PrintAction {
			get { return action; }
		}
#endif

		internal GraphicsPrinter GraphicsContext {
			get { return graphics_context; }
			set { graphics_context = value; }
		}
	}
}
