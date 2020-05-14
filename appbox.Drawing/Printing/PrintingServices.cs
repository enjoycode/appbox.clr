using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.ComponentModel;

namespace appbox.Drawing.Printing
{
	/// <summary>
	/// This class is designed to cache the values retrieved by the 
	/// native printing services, as opposed to GlobalPrintingServices, which
	/// doesn't cache any values.
	/// </summary>
	internal abstract class PrintingServices
	{
		#region Properties
		internal abstract string DefaultPrinter { get; }
		#endregion

		#region Methods
		internal abstract bool IsPrinterValid(string printer);
		internal abstract void LoadPrinterSettings (string printer, PrinterSettings settings);
		internal abstract void LoadPrinterResolutions (string printer, PrinterSettings settings);

		// Used from SWF
		internal abstract void GetPrintDialogInfo (string printer, ref string port, ref string type, ref string status, ref string comment);
		
		internal void LoadDefaultResolutions (PrinterSettings.PrinterResolutionCollection col)
		{
			col.Add (new PrinterResolution ((int) PrinterResolutionKind.High, -1, PrinterResolutionKind.High));
			col.Add (new PrinterResolution ((int) PrinterResolutionKind.Medium, -1, PrinterResolutionKind.Medium));
			col.Add (new PrinterResolution ((int) PrinterResolutionKind.Low, -1, PrinterResolutionKind.Low));
			col.Add (new PrinterResolution ((int) PrinterResolutionKind.Draft, -1, PrinterResolutionKind.Draft));
		}
		#endregion
	}
	
	internal abstract class GlobalPrintingServices
	{
		#region Properties
		internal abstract PrinterSettings.StringCollection InstalledPrinters { get; }
		#endregion

		#region Methods
        internal abstract void CreateGraphicsContext (PrintDocument document);

		internal abstract bool StartDoc (GraphicsPrinter gr, string doc_name, string output_file);
        internal abstract bool StartPage (PrintPageEventArgs e);
		internal abstract bool EndPage (PrintPageEventArgs e);
		internal abstract bool EndDoc (GraphicsPrinter gr);
		#endregion
	
	}

	internal class SysPrn
	{
		static GlobalPrintingServices global_printing_services;
		static bool is_unix;

		static SysPrn ()
		{
            is_unix = Environment.OSVersion.Platform == PlatformID.MacOSX
                                 || Environment.OSVersion.Platform == PlatformID.Unix;
		}
		
		internal static PrintingServices CreatePrintingService () {
			// if (is_unix)
			// 	return new PrintingServicesUnix ();
            throw new NotImplementedException();
			//return new PrintingServicesWin32 ();				
		}			

		internal static GlobalPrintingServices GlobalService {
			get {
				if (global_printing_services == null) {
                    // if (is_unix)
                    //     global_printing_services = new GlobalPrintingServicesUnix();
                    // else
                        throw new NotImplementedException();//global_printing_services = new GlobalPrintingServicesWin32 ();
				}

				return global_printing_services;
			}
		}

		internal static void GetPrintDialogInfo (string printer, ref string port, ref string type, ref string status, ref string comment) 
		{
			CreatePrintingService().GetPrintDialogInfo (printer, ref port, ref type, ref status, ref comment);
		}

		internal class Printer {
			public readonly string Name;
			public readonly string Comment;
			public readonly string Port;
			public readonly string Type;
			public readonly string Status;
			public PrinterSettings Settings;
			public bool IsDefault;
			
			public Printer (string port, string type, string status, string comment) {
				Port = port;
				Type = type;
				Status = status;
				Comment = comment;
			}
		}
	}
	
	internal class GraphicsPrinter
	{
		//private	Graphics graphics;
		//private IntPtr	hDC;

        private PrintDocument printDocument;
        internal PdfDocument PdfDocument;
		 
		//internal GraphicsPrinter (Graphics gr, IntPtr dc)
		//{
		//	graphics = gr;
		//	hDC = dc;
		//}

        internal GraphicsPrinter(PrintDocument document)
        {
            this.printDocument = document;
        }
						
		//internal Graphics Graphics { 
		//	get { return graphics; }
		//	set { graphics = value; }
		//}

        internal PrintDocument PrintDocument
        {
            get { return printDocument; }
        }

		//internal IntPtr Hdc { get { return hDC; }}
	}
}


