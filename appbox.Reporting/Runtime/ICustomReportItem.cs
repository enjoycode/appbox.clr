using System;
using System.Collections.Generic;
using System.Xml;
using SkiaSharp;

namespace appbox.Reporting.RDL
{
    /// <summary>
    /// ICustomReportItem defines the protocol for implementing a CustomReportItem
    /// </summary>
    public interface ICustomReportItem : IDisposable
    {
        /// <summary>
        /// Does CustomReportItem require DataRegions
        /// </summary>
        bool IsDataRegion();

        /// <summary>
        /// Draw the imagep; do SetParameters first
        /// </summary>
        SKBitmap DrawImage(int width, int height); //TODO:暂使用SKBitmap

        ///// <summary>
        ///// Design time: Draw the designer image in the passed bitmap;
        ///// </summary>
        //void DrawDesignerImage(ref Bitmap bm);
        /// <summary>
        /// Set the runtime properties
        /// </summary>
        void SetProperties(IDictionary<string, object> parameters);

        /// <summary>
        /// Design time: return class representing properties
        /// </summary>
        object GetPropertiesInstance(XmlNode node);

        /// <summary>
        /// Design time: given class representing properties set the XML custom properties
        /// </summary>
        void SetPropertiesInstance(XmlNode node, object inst);

        /// <summary>
        /// Design time: return string with <CustomReportItem> ... </CustomReportItem> syntax for the insert
        /// </summary>
        string GetCustomReportItemXml();
    }

}
