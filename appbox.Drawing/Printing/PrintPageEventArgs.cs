using System;

namespace appbox.Drawing.Printing
{

    /// <summary>
    /// Summary description for PrintPageEventHandler.
    /// </summary>
    public delegate void PrintPageEventHandler(object sender, PrintPageEventArgs e);

    /// <summary>
    /// Summary description for PrintPageEventArgs.
    /// </summary>
    public class PrintPageEventArgs : EventArgs
    {
        bool cancel;
        Graphics graphics;
        bool hasmorePages;
        Rectangle marginBounds;
        Rectangle pageBounds;
        PageSettings pageSettings;
        GraphicsPrinter graphics_context;

        public PrintPageEventArgs(Graphics graphics, Rectangle marginBounds,
            Rectangle pageBounds, PageSettings pageSettings)
        {
            this.graphics = graphics;
            this.marginBounds = marginBounds;
            this.pageBounds = pageBounds;
            this.pageSettings = pageSettings;
        }
        public bool Cancel
        {
            get
            {
                return cancel;
            }
            set
            {
                cancel = value;
            }
        }
        public Graphics Graphics
        {
            get
            {
                return graphics;
            }
        }
        public bool HasMorePages
        {
            get
            {
                return hasmorePages;
            }
            set
            {
                hasmorePages = value;
            }
        }
        public Rectangle MarginBounds
        {
            get
            {
                return marginBounds;
            }
        }
        public Rectangle PageBounds
        {
            get
            {
                return pageBounds;
            }
        }
        public PageSettings PageSettings
        {
            get
            {
                return pageSettings;
            }
        }

        // used in PrintDocument.Print()
        internal void SetGraphics(Graphics g)
        {
            graphics = g;
        }

        internal GraphicsPrinter GraphicsContext
        {
            get { return graphics_context; }
            set { graphics_context = value; }
        }
    }
}
