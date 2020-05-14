using System;

namespace appbox.Drawing.Printing
{
	public class StandardPrintController : PrintController
	{		
		public StandardPrintController()
		{
		}
		
		public override void OnEndPage (PrintDocument document, PrintPageEventArgs e)
		{
			SysPrn.GlobalService.EndPage(e);
		}
		
		public override void OnStartPrint (PrintDocument document, PrintEventArgs e)
		{			
			SysPrn.GlobalService.CreateGraphicsContext (document);
            e.GraphicsContext = new GraphicsPrinter (document);
			SysPrn.GlobalService.StartDoc (e.GraphicsContext, document.DocumentName, string.Empty);			
		}
		
		public override void OnEndPrint (PrintDocument document, PrintEventArgs e)
		{			
			SysPrn.GlobalService.EndDoc (e.GraphicsContext);
		}
		
		public override Graphics OnStartPage (PrintDocument document, PrintPageEventArgs e)
		{				
			SysPrn.GlobalService.StartPage (e);
			return e.Graphics;
		}
	}
}
