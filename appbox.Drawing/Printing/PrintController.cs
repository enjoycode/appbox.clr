namespace appbox.Drawing.Printing {

	public abstract class PrintController {

#if NET_2_0		
		public virtual bool IsPreview { 
			get { return false; }
		}
#else
		public PrintController ()
		{
		}		
#endif
		public virtual void OnEndPage (PrintDocument document, PrintPageEventArgs e)
		{
		}

		public virtual void OnStartPrint (PrintDocument document, PrintEventArgs e)
		{
		}

		public virtual void OnEndPrint (PrintDocument document, PrintEventArgs e)
		{
		}

		public virtual Graphics OnStartPage (PrintDocument document, PrintPageEventArgs e)
		{
			return null;
		}
	}
}
