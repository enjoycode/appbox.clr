using System;

namespace appbox.Drawing.Printing
{

    [Serializable]
    public class PrinterResolution
    {
        private PrinterResolutionKind kind = PrinterResolutionKind.Custom;
        private int x;
        private int y;

        public PrinterResolution()
        {
        }

        internal PrinterResolution(int x, int y, PrinterResolutionKind kind)
        {
            this.x = x;
            this.y = y;
            this.kind = kind;
        }

        public int X
        {
            get { return x; }
            set { x = value; }
        }

        public int Y
        {
            get { return y; }
            set { y = value; }
        }

        public PrinterResolutionKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        public override string ToString()
        {
            if (kind != PrinterResolutionKind.Custom)
                return "[PrinterResolution " + kind.ToString() + "]";

            return "[PrinterResolution X=" + x + " Y=" + y + "]";
        }
    }
}
