using System;
using System.Xml;
using System.IO;
using appbox.Drawing;
using System.Threading;
using System.Net;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// Represents an image.  Source of image can from database, external or embedded. 
    ///</summary>
    [Serializable]
    internal class Image : ReportItem
    {
        /// <summary>
        /// Identifies the source of the image
        /// </summary>
        internal ImageSourceEnum ImageSource { get; set; }

        /// <summary>
        /// See Source. Expected datatype is string or
        /// binary, depending on Source. If the Value is
        /// null, no image is displayed.
        /// </summary>
        internal Expression Value { get; set; }

        /// <summary>
        /// (string) An expression, the value of which is the
        ///	MIMEType for the image.
        ///	Valid values are: image/bmp, image/jpeg,
        ///	image/gif, image/png, image/x-png
        /// Required if Source = Database. Ignored otherwise.
        /// </summary>
        internal Expression MIMEType { get; set; }

        /// <summary>
        /// Defines the behavior if the image does not fit within the specified size.
        /// </summary>
        internal ImageSizingEnum Sizing { get; set; }

        /// <summary>
        /// true if Image is a constant at runtime
        /// </summary>
        internal bool ConstantImage { get; private set; }

        /// <summary>
        /// only for RenderHtml and embeddedImage. we need the embedded image code for html.
        /// </summary>
        internal string EmbeddedImageData { get; private set; }

        /// <summary>
        /// Only gets set for Images which contain urls rather than coming from the database etc..
        /// </summary>
        public string ImageUrl { get; private set; }

        private static void CopyStream(Stream src, Stream dst)
        {
            byte[] buffer = new byte[16 * 1024];
            int bytesRead;

            while ((bytesRead = src.Read(buffer, 0, buffer.Length)) > 0)
            {
                dst.Write(buffer, 0, bytesRead);
            }
        }

        internal Image(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p, xNode)
        {
            ImageSource = ImageSourceEnum.Unknown;
            Value = null;
            MIMEType = null;
            Sizing = ImageSizingEnum.AutoSize;
            ConstantImage = false;

            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                switch (xNodeLoop.Name)
                {
                    case "Source":
                        ImageSource = RDL.ImageSource.GetStyle(xNodeLoop.InnerText);
                        break;
                    case "Value":
                        Value = new Expression(r, this, xNodeLoop, ExpressionType.Variant);
                        break;
                    case "MIMEType":
                        MIMEType = new Expression(r, this, xNodeLoop, ExpressionType.String);
                        break;
                    case "Sizing":
                        Sizing = ImageSizing.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    default:
                        if (ReportItemElement(xNodeLoop))   // try at ReportItem level
                            break;
                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, "Unknown Image element " + xNodeLoop.Name + " ignored.");
                        break;
                }
            }
            if (ImageSource == ImageSourceEnum.Unknown)
                OwnerReport.rl.LogError(8, "Image requires a Source element.");
            if (Value == null)
                OwnerReport.rl.LogError(8, "Image requires the Value element.");
        }

        // Handle parsing of function in final pass
        override internal void FinalPass()
        {
            base.FinalPass();

            Value.FinalPass();
            if (MIMEType != null)
                MIMEType.FinalPass();

            ConstantImage = this.IsConstant();

            return;
        }

        // Returns true if the image and style remain constant at runtime
        bool IsConstant()
        {

            if (Value.IsConstant())
            {
                if (MIMEType == null || MIMEType.IsConstant())
                {
                    //					if (this.Style == null || this.Style.ConstantStyle)
                    //						return true;
                    return true;    // ok if style changes
                }
            }
            return false;
        }

        override internal void Run(IPresent ip, Row row)
        {
            base.Run(ip, row);

            Stream strm = null;
            try
            {
                strm = GetImageStream(ip.Report(), row, out string mtype);
                ip.Image(this, row, null, strm);
            }
            catch
            {
                // image failed to load;  continue processing
            }
            finally
            {
                if (strm != null)
                    strm.Close();
            }
            return;
        }

        override internal void RunPage(Pages pgs, Row row)
        {
            Report r = pgs.Report;
            bool bHidden = IsHidden(r, row);

            WorkClass wc = GetWC(r);

            SetPagePositionBegin(pgs);
            if (bHidden)
            {
                PageImage pi = new PageImage(null, 0, 0);
                SetPagePositionAndStyle(r, pi, row);
                SetPagePositionEnd(pgs, pi.Y + pi.H);
                return;
            }

            if (wc.PgImage != null)
            {   // have we already generated this one
                // reuse most of the work; only position will likely change
                PageImage pi = new PageImage(wc.PgImage.Image, wc.PgImage.SamplesW, wc.PgImage.SamplesH);
                pi.Name = wc.PgImage.Name;              // this is name it will be shared under
                pi.Sizing = Sizing;
                SetPagePositionAndStyle(r, pi, row);
                pgs.CurrentPage.AddObject(pi);
                SetPagePositionEnd(pgs, pi.Y + pi.H);
                return;
            }

            try
            {
                using var strm = GetImageStream(r, row, out string mtype);
                if (strm == null)
                {
                    r.rl.LogError(4, $"Unable to load image {Name.Nm}.");
                    return;
                }
                var im = Drawing.Image.FromStream(strm);
                int height = im.Height;
                int width = im.Width;
                PageImage pi = new PageImage(im, width, height);
                pi.Sizing = Sizing;
                SetPagePositionAndStyle(r, pi, row);

                pgs.CurrentPage.AddObject(pi);
                if (ConstantImage)
                {
                    wc.PgImage = pi;
                    // create unique name; PDF generation uses this to optimize the saving of the image only once
                    pi.Name = "pi" + Interlocked.Increment(ref Parser.Counter).ToString();  // create unique name
                }

                SetPagePositionEnd(pgs, pi.Y + pi.H);
            }
            catch (Exception e)
            {
                // image failed to load, continue processing
                r.rl.LogError(4, "Image load failed.  " + e.Message);
            }
        }

        private Stream GetImageStream(Report rpt, Row row, out string mtype)
        {
            mtype = null;
            Stream strm = null;
            try
            {
                switch (ImageSource)
                {
                    case ImageSourceEnum.Database:
                        if (MIMEType == null)
                            return null;
                        mtype = MIMEType.EvaluateString(rpt, row);
                        object o = Value.Evaluate(rpt, row);
                        strm = new MemoryStream((byte[])o);
                        break;
                    case ImageSourceEnum.Embedded:
                        string name = Value.EvaluateString(rpt, row);
                        EmbeddedImage ei = (EmbeddedImage)OwnerReport.LUEmbeddedImages[name];
                        mtype = ei.MIMEType;
                        byte[] ba = Convert.FromBase64String(ei.ImageData);
                        EmbeddedImageData = ei.ImageData; // we need this for html embedded image
                        strm = new MemoryStream(ba);
                        break;
                    case ImageSourceEnum.External:
                        //Added Image URL from forum, User: solidstate
                        string fname = ImageUrl = Value.EvaluateString(rpt, row);
                        mtype = GetMimeType(fname);
                        if (fname.StartsWith("http:") ||
                            fname.StartsWith("file:") ||
                            fname.StartsWith("https:"))
                        {
                            WebRequest wreq = WebRequest.Create(fname);
                            WebResponse wres = wreq.GetResponse();
                            strm = wres.GetResponseStream();
                        }
                        else
                            strm = new FileStream(fname, FileMode.Open, FileAccess.Read);
                        break;
                    default:
                        return null;
                }
            }
            catch (Exception e)
            {
                if (strm != null)
                {
                    strm.Close();
                    strm = null;
                }
                rpt.rl.LogError(4, $"Unable to load image. {e.Message}");
            }

            return strm;
        }

        static internal string GetMimeType(string file)
        {
            string fileExt;
            int startPos = file.LastIndexOf(".") + 1;
            fileExt = file.Substring(startPos).ToLower();

            switch (fileExt)
            {
                case "bmp":
                    return "image/bmp";
                case "jpeg":
                case "jpe":
                case "jpg":
                case "jfif":
                    return "image/jpeg";
                case "gif":
                    return "image/gif";
                case "png":
                    return "image/png";
                case "tif":
                case "tiff":
                    return "image/tiff";
                default:
                    return null;
            }
        }

        private WorkClass GetWC(Report rpt)
        {
            WorkClass wc = rpt.Cache.Get(this, "wc") as WorkClass;
            if (wc == null)
            {
                wc = new WorkClass();
                rpt.Cache.Add(this, "wc", wc);
            }
            return wc;
        }

        private void RemoveImageWC(Report rpt)
        {
            rpt.Cache.Remove(this, "wc");
        }

        class WorkClass
        {
            internal PageImage PgImage; // When ConstantImage is true this will save the PageImage for reuse
            internal WorkClass()
            {
                PgImage = null;
            }
        }

    }
}
