using System;
using System.Xml;
using System.Collections.Generic;
using appbox.Drawing;
using System.IO;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// CustomReportItem describes a report item that is not natively defined in RDL.  The 
    /// RdlEngineConfig.xml file (loaded by RdlEngineConfig.cs) contains a list of the extensions.
    ///</summary>
    [Serializable]
    internal class CustomReportItem : Rectangle
    {
        //static readonly ImageFormat IMAGEFORMAT = ImageFormat.Jpeg;

        /// <summary>
        /// The type of the custom report item. Interpreted by a
        /// report design tool or server.
        /// </summary>
        internal string Type { get; set; }

        private readonly XmlNode xNode;
        private readonly List<CustomProperty> _Properties;

        internal CustomReportItem(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p, xNode, false)
        {
            Type = null;
            ReportItems ris = null;
            bool bVersion2 = true;
            this.xNode = xNode;
            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                switch (xNodeLoop.Name)
                {
                    case "Type":
                        Type = xNodeLoop.InnerText;
                        break;
                    case "ReportItems":         // Version 1 of the specification
                        ris = new ReportItems(r, this, xNodeLoop);
                        bVersion2 = false;
                        break;
                    case "AltReportItem":       // Verstion 2 of the specification
                        ris = new ReportItems(r, this, xNodeLoop);
                        break;
                    case "CustomProperties":
                        _Properties = CustomProperties(xNodeLoop);
                        break;
                    default:
                        if (ReportItemElement(xNodeLoop))   // try at ReportItem level
                            break;
                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, $"Unknown CustomReportItem element {xNodeLoop.Name} ignored.");
                        break;
                }
            }
            ReportItems = ris;
            if (bVersion2 && ris != null)
            {
                if (ris.Items.Count != 1)
                    OwnerReport.rl.LogError(8, "Only one element is allowed within an AltReportItem.");
            }

            if (Type == null)
                OwnerReport.rl.LogError(8, "CustomReportItem requires the Type element.");
        }

        override internal void FinalPass()
        {
            base.FinalPass();       // this handles the finalpass of the AltReportItems

            // Handle the final pass for the Custom Properties
            if (_Properties != null)
            {
                foreach (CustomProperty cp in _Properties)
                {
                    cp.Name.FinalPass();
                    cp.Value.FinalPass();
                }
            }

            // Find out whether the type is known
            ICustomReportItem cri = null;
            try
            {
                cri = RdlEngineConfig.CreateCustomReportItem(Type);
            }
            catch (Exception ex)
            {   // Not an error since we'll simply use the ReportItems
                OwnerReport.rl.LogError(4, $"CustomReportItem load of {Type} failed: {ex.Message}");
            }
            finally
            {
                if (cri != null)
                    cri.Dispose();
            }

            return;
        }

        override internal void Run(IPresent ip, Row row)
        {
            throw new NotImplementedException();
            //        Report rpt = ip.Report();

            //        ICustomReportItem cri = null;
            //        try
            //        {
            //            cri = RdlEngineConfig.CreateCustomReportItem(_Type);
            //Type a = cri.GetType();
            //Bitmap bm = null;
            //SetProperties(rpt, row, cri);
            //int width = WidthCalc(rpt, null) -
            //	(Style == null ? 0 :
            //		(Style.EvalPaddingLeftPx(rpt, row) + Style.EvalPaddingRightPx(rpt, row)));
            //int height = RSize.PixelsFromPoints(this.HeightOrOwnerHeight) -
            //	(Style == null ? 0 :
            //		(Style.EvalPaddingTopPx(rpt, row) + Style.EvalPaddingBottomPx(rpt, row)));
            //bm = new Bitmap(width, height);
            //cri.DrawImage(ref bm);

            //MemoryStream ostrm = new MemoryStream();
            //// 06122007AJM Changed to use high quality JPEG encoding
            ////bm.Save(ostrm, IMAGEFORMAT);	// generate a jpeg   TODO: get png to work with pdf
            //appbox.Drawing.Imaging.ImageCodecInfo[] info;
            //info = ImageCodecInfo.GetImageEncoders();
            //EncoderParameters encoderParameters;
            //encoderParameters = new EncoderParameters(1);
            //// 20022008 AJM GJL - Using centralised image quality
            //encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, ImageQualityManager.CustomImageQuality);
            //appbox.Drawing.Imaging.ImageCodecInfo codec = null;
            //for(int i = 0; i < info.Length; i++) {
            //	if(info[i].FormatDescription == "JPEG") {
            //		codec = info[i];
            //		break;
            //	}
            //}
            //bm.Save(ostrm, codec, encoderParameters);

            //ip.Image(new Image(rpt.ReportDefinition, this, xNode), row, null, ostrm);
            //ostrm.Close();
            //        }
            //        catch (Exception ex)
            //        {
            //            rpt.rl.LogError(8, string.Format("Exception in CustomReportItem handling.\n{0}\n{1}", ex.Message, ex.StackTrace));
            //        }
            //        finally
            //        {
            //            if (cri != null)
            //                cri.Dispose();
            //        }
            //        return;
        }

        override internal void RunPage(Pages pgs, Row row)
        {
            Report rpt = pgs.Report;
            if (IsHidden(rpt, row))
                return;

            SetPagePositionBegin(pgs);

            // Build the Chart bitmap, along with data regions
            ICustomReportItem cri = null;
            try
            {
                cri = RdlEngineConfig.CreateCustomReportItem(Type);
                SetProperties(pgs.Report, row, cri);

                int width = WidthCalc(rpt, pgs.G) -
                    (Style == null ? 0 :
                        (Style.EvalPaddingLeftPx(rpt, row) + Style.EvalPaddingRightPx(rpt, row)));
                int height = RSize.PixelsFromPoints(HeightOrOwnerHeight) -
                    (Style == null ? 0 :
                        (Style.EvalPaddingTopPx(rpt, row) + Style.EvalPaddingBottomPx(rpt, row)));
                var skbmp = cri.DrawImage(width, height);
                var bm = new Bitmap(skbmp);

                PageImage pi = new PageImage(bm, width, height);   // Create an image
                pi.Sizing = ImageSizingEnum.Clip;
                //RunPageRegionBegin(pgs);

                SetPagePositionAndStyle(rpt, pi, row);

                if (pgs.CurrentPage.YOffset + pi.Y + pi.H >= pgs.BottomOfPage && !pgs.CurrentPage.IsEmpty())
                {   // force page break if it doesn't fit on the page
                    pgs.NextOrNew();
                    pgs.CurrentPage.YOffset = OwnerReport.TopOfPage;
                    if (YParents != null)
                        pi.Y = 0;
                }

                pgs.CurrentPage.AddObject(pi);    // Put image onto the current page

                // RunPageRegionEnd(pgs);
                // I don't know why we need move offset after draw. If we do it, barcode if it first in list all text shifted.
                // If it broken something, write to Gankov
                /*  if (!this.PageBreakAtEnd && !IsTableOrMatrixCell(rpt))
                    {
                        float newY = pi.Y + pi.H;
                        pgs.CurrentPage.YOffset += newY;	// bump the y location
                    } */
                SetPagePositionEnd(pgs, pi.Y + pi.H);
            }
            catch (Exception ex)
            {
                rpt.rl.LogError(8, $"Exception in CustomReportItem handling: {ex.Message}");
            }
            finally
            {
                if (cri != null)
                    cri.Dispose();
            }
        }

        private void SetProperties(Report rpt, Row row, ICustomReportItem cri)
        {
            if (_Properties == null || _Properties.Count == 0)  // Any properties specified?
                return;

            Dictionary<string, object> dict =
                new Dictionary<string, object>(_Properties.Count);
            foreach (CustomProperty cp in _Properties)
            {
                string name = cp.Name.EvaluateString(rpt, row);
                object val = cp.Value.Evaluate(rpt, row);
                try { dict.Add(name, val); }
                catch
                {
                    rpt.rl.LogError(4, string.Format("Property {0} has already been set.  New value {1} ignored.", name, val));
                }
            }
            cri.SetProperties(dict);
        }

        private List<CustomProperty> CustomProperties(XmlNode xNode)
        {
            if (!xNode.HasChildNodes)
                return null;

            List<CustomProperty> cps = new List<CustomProperty>(xNode.ChildNodes.Count);
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                switch (xNodeLoop.Name)
                {
                    case "CustomProperty":
                        CustomProperty cp = CustomProperty(xNodeLoop);
                        if (cp != null)
                            cps.Add(cp);
                        break;
                    default:
                        OwnerReport.rl.LogError(4, "Unknown CustomProperties element " + xNodeLoop.Name + " ignored.");
                        break;
                }
            }
            return cps;
        }

        private CustomProperty CustomProperty(XmlNode xNode)
        {
            Expression name = null;
            Expression val = null;
            CustomProperty cp = null;
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                switch (xNodeLoop.Name)
                {
                    case "Name":
                        name = new Expression(OwnerReport, this, xNodeLoop, ExpressionType.String);
                        break;
                    case "Value":
                        val = new Expression(OwnerReport, this, xNodeLoop, ExpressionType.Variant);
                        break;
                    default:
                        OwnerReport.rl.LogError(4, "Unknown CustomProperty element " + xNodeLoop.Name + " ignored.");
                        break;
                }
            }
            if (name == null || val == null)
                OwnerReport.rl.LogError(8, "CustomProperty requires the Name and Value element.");
            else
            {
                cp = new CustomProperty(name, val);
            }
            return cp;
        }
    }

    sealed class CustomProperty
    {
        /// <summary>
        /// name of the property
        /// </summary>
        internal Expression Name { get; }

        /// <summary>
        /// value of the property
        /// </summary>
        internal Expression Value { get; }

        internal CustomProperty(Expression name, Expression val)
        {
            Name = name;
            Value = val;
        }
    }

}
