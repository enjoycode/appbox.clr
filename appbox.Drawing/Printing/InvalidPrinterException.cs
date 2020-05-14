using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace appbox.Drawing.Printing
{

    [Serializable]
    public class InvalidPrinterException : SystemException
    {

        //		private PrinterSettings settings;

        public InvalidPrinterException(PrinterSettings settings) : base(InvalidPrinterException.GetMessage(settings))
        {
            //			this.settings = settings;
        }

        protected InvalidPrinterException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");

            base.GetObjectData(info, context);
        }

        private static string GetMessage(PrinterSettings settings)
        {
            if (settings.PrinterName == null || settings.PrinterName == String.Empty)
                return "No Printers Installed";
            return String.Format("Tried to access printer '{0}' with invalid settings.", settings.PrinterName);
        }
    }
}
