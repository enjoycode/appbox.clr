using System;
using System.Collections.Generic;
using appbox.Drawing;
using System.Xml;

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
        /// Draw the image in the passed bitmap; do SetParameters first
        /// </summary>
        void DrawImage(ref Bitmap bm);
        /// <summary>
        /// Design time: Draw the designer image in the passed bitmap;
        /// </summary>
        void DrawDesignerImage(ref Bitmap bm);
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
