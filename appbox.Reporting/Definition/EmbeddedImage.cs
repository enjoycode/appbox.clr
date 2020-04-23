using System;
using System.Xml;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// The defintion of an embedded images, including the actual image data and type.
    ///</summary>
    [Serializable]
    internal class EmbeddedImage : ReportLink
    {
        /// <summary>
        /// Name of the image.
        /// </summary>
        internal Name Name { get; set; }

        /// <summary>
        /// The MIMEType for the image. Valid values are:
        /// image/bmp, image/jpeg, image/gif, image/png, image/xpng.
        /// </summary>
        internal string MIMEType { get; set; }

        /// <summary>
        /// Base-64 encoded image data.
        /// </summary>
        internal string ImageData { get; set; }

        internal EmbeddedImage(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            Name = null;
            MIMEType = null;
            ImageData = null;
            // Run thru the attributes
            foreach (XmlAttribute xAttr in xNode.Attributes)
            {
                switch (xAttr.Name)
                {
                    case "Name":
                        Name = new Name(xAttr.Value);
                        break;
                }
            }
            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                switch (xNodeLoop.Name)
                {
                    case "MIMEType":
                        MIMEType = xNodeLoop.InnerText;
                        break;
                    case "ImageData":
                        ImageData = xNodeLoop.InnerText;
                        break;
                    default:
                        OwnerReport.rl.LogError(4, $"Unknown Report element '{xNodeLoop.Name}' ignored.");
                        break;
                }
            }

            if (Name == null)
            {
                OwnerReport.rl.LogError(8, "EmbeddedImage Name is required but not specified.");
            }
            else
            {
                try
                {
                    OwnerReport.LUEmbeddedImages.Add(Name.Nm, this);       // add to referenceable embedded images
                }
                catch       // Duplicate name
                {
                    OwnerReport.rl.LogError(4, $"Duplicate EmbeddedImage  name '{Name.Nm}' ignored.");
                }
            }
            if (MIMEType == null)
                OwnerReport.rl.LogError(8, "EmbeddedImage MIMEType is required but not specified for "
                    + (Name == null ? "'name not specified'" : Name.Nm));

            if (ImageData == null)
                OwnerReport.rl.LogError(8, "EmbeddedImage ImageData is required but not specified for "
                    + (Name == null ? "'name not specified'" : Name.Nm));
        }

        override internal void FinalPass()
        {
            return;
        }

    }
}
