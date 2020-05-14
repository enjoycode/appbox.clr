using System;
using System.IO;
using System.ComponentModel;

namespace appbox.Drawing.Printing
{

	public class PrintDocument : Component
    {
		private PageSettings defaultpagesettings;
		private PrinterSettings printersettings;
		private PrintController printcontroller;
		private string documentname;
		private bool originAtMargins = false; // .NET V1.1 Beta

		public PrintDocument() {
			documentname = "document"; //offical default.
			printersettings = new PrinterSettings(); // use default values
			defaultpagesettings = (PageSettings) printersettings.DefaultPageSettings.Clone ();
			printcontroller = new StandardPrintController();
		}

		// properties
		public PageSettings DefaultPageSettings{
			get{
				return defaultpagesettings;
			}
			set{
				defaultpagesettings = value;
			}
		}

		// Name of the document, not the file!
		[DefaultValue ("document")]
		public string DocumentName{
			get{
				return documentname;
			}
			set{
				documentname = value;
			}
		}

		public PrintController PrintController{
			get{
				return printcontroller;
			}
			set{
				printcontroller = value;
			}
		}

		public PrinterSettings PrinterSettings{
			get{
				return printersettings;
			}
			set{
				printersettings = value == null ? new PrinterSettings () : value;
			}
		}

		[DefaultValue (false)]
		//[SRDescription ("Determines if the origin is set at the specified margins.")]
		public bool OriginAtMargins{
			get{
				return originAtMargins;
			}
			set{
				originAtMargins = value;
			}
		}

		// methods
		public void Print(){
			PrintEventArgs printArgs = new PrintEventArgs();
			this.OnBeginPrint(printArgs);
			if (printArgs.Cancel)
				return;
			PrintController.OnStartPrint(this, printArgs);
			if (printArgs.Cancel)
				return;			
			
            // 原实现
			//Graphics g = null;
			//if (printArgs.GraphicsContext != null) {
			//	Graphics.FromHdc (printArgs.GraphicsContext.Hdc); //todo:
			//	printArgs.GraphicsContext.Graphics = g;
			//}

			// while there are more pages
			PrintPageEventArgs printPageArgs;
			do
			{
				QueryPageSettingsEventArgs queryPageSettingsArgs = new QueryPageSettingsEventArgs (
						DefaultPageSettings.Clone () as PageSettings);
				OnQueryPageSettings (queryPageSettingsArgs);
				
				PageSettings pageSettings = queryPageSettingsArgs.PageSettings;
				printPageArgs = new PrintPageEventArgs(
                        null/*g*/,
						pageSettings.Bounds,
						new Rectangle(0, 0, pageSettings.PaperSize.Width, pageSettings.PaperSize.Height),
						pageSettings);

				// 原TODO: We should create a graphics context for each page since they can have diferent paper
				// size, orientation, etc. We use a single graphic for now to keep Cairo using a single PDF file.
                // 现每个Page一个Graphics

				printPageArgs.GraphicsContext = printArgs.GraphicsContext;
				Graphics pg = PrintController.OnStartPage(this, printPageArgs);
				// assign Graphics in printPageArgs
				printPageArgs.SetGraphics(pg);
				
				if (!printPageArgs.Cancel)
					this.OnPrintPage(printPageArgs);				
				
				PrintController.OnEndPage(this, printPageArgs);				
				if (printPageArgs.Cancel)
					break;				
			} while (printPageArgs.HasMorePages);

			this.OnEndPrint(printArgs);
			PrintController.OnEndPrint(this, printArgs);			
		}

		public override string ToString(){
			return "[PrintDocument " + this.DocumentName + "]";
		}
		
		// events
		protected virtual void OnBeginPrint(PrintEventArgs e){
			//fire the event
			if (BeginPrint != null)
				BeginPrint(this, e);
		}
		
		protected virtual void OnEndPrint(PrintEventArgs e){
			//fire the event
			if (EndPrint != null)
				EndPrint(this, e);
		}
		
		protected virtual void OnPrintPage(PrintPageEventArgs e){
			//fire the event
			if (PrintPage != null)
				PrintPage(this, e);
		}
		
		protected virtual void OnQueryPageSettings(QueryPageSettingsEventArgs e){
			//fire the event
			if (QueryPageSettings != null)
				QueryPageSettings(this, e);
		}

		//[SRDescription ("Raised when printing begins")]
		public event PrintEventHandler BeginPrint;

		//[SRDescription ("Raised when printing ends")]
		public event PrintEventHandler EndPrint;

		//[SRDescription ("Raised when printing of a new page begins")]
		public event PrintPageEventHandler PrintPage;

		//[SRDescription ("Raised before printing of a new page begins")]
		public event QueryPageSettingsEventHandler QueryPageSettings;
	}
}
