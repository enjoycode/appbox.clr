using System;
using System.Collections.Generic;
using System.Xml;

namespace appbox.Reporting
{
    public abstract class BarcodeBase : RDL.ICustomReportItem
    {
        private string _code = "";

        protected abstract ZXing.BarcodeFormat BarcodeFormat { get; }

        #region ICustomReportItem Members
        public bool IsDataRegion() => false;

        public SkiaSharp.SKBitmap DrawImage(int width, int height)
        {
            return DrawImage(width, height, _code);
        }

        public SkiaSharp.SKBitmap DrawImage(int width, int height, string code)
        {
            var writer = new ZXing.SkiaSharp.BarcodeWriter(); //TODO:优化单例ZXing.BarCodeRender
            writer.Format = BarcodeFormat;
            writer.Options.Height = height;
            writer.Options.Width = width;
            return writer.Write(code);
        }

        public void SetProperties(IDictionary<string, object> props)
        {
            try
            {
                _code = props["Code"].ToString();
            }
            catch (KeyNotFoundException)
            {
                throw new Exception("Code property must be specified");
            }
        }

        public object GetPropertiesInstance(XmlNode iNode)
        {
            BarCodeProperties bcp = new BarCodeProperties(this, iNode);
            foreach (XmlNode n in iNode.ChildNodes)
            {
                if (n.Name != "CustomProperty")
                    continue;
                string pname = XmlHelpers.GetNamedElementValue(n, "Name", "");
                switch (pname)
                {
                    case "Code":
                        bcp.SetBarCode(XmlHelpers.GetNamedElementValue(n, "Value", ""));
                        break;
                    default:
                        break;
                }
            }

            return bcp;
        }

        public void SetPropertiesInstance(XmlNode node, object inst)
        {
            node.RemoveAll();       // Get rid of all properties

            BarCodeProperties bcp = inst as BarCodeProperties;
            if (bcp == null)
                return;


            XmlHelpers.CreateChild(node, "Code", bcp.Code);
        }


        /// <summary>
        /// Design time call: return string with <CustomReportItem> ... </CustomReportItem> syntax for 
        /// the insert.  The string contains a variable {0} which will be substituted with the
        /// configuration name.  This allows the name to be completely controlled by
        /// the configuration file.
        /// </summary>
        /// <returns></returns>
        public string GetCustomReportItemXml()
        {
            return "<CustomReportItem><Type>{0}</Type>" +
                "<Height>200pt</Height><Width>100pt</Width>" +
                "<CustomProperties>" +
                "<CustomProperty>" +
                "<Name>Code</Name>" +
                "<Value>Enter Your Value</Value>" +
                "</CustomProperty>" +
                "</CustomProperties>" +
                "</CustomReportItem>";
        }

        #endregion

        #region IDisposable Members
        public void Dispose() { }
        #endregion

        /// <summary>
        /// BarCodeProperties- All properties are type string to allow for definition of
        /// a runtime expression.
        /// </summary>
        public sealed class BarCodeProperties
        {
            private string _code;
            private readonly BarcodeBase _bc;
            private readonly XmlNode _node;

            internal BarCodeProperties(BarcodeBase bc, XmlNode node)
            {
                _bc = bc;
                _node = node;
            }

            internal void SetBarCode(string ns)
            {
                _code = ns;
            }

            /// <summary>
            /// The text string to be encoded as a Code.
            /// </summary>
            public string Code
            {
                get { return _code; }
                set { _code = value; _bc.SetPropertiesInstance(_node, this); }
            }
        }

    }

    public sealed class BarCode128: BarcodeBase
    {
        protected override ZXing.BarcodeFormat BarcodeFormat => ZXing.BarcodeFormat.CODE_128;
    }

    public sealed class BarCode39 : BarcodeBase
    {
        protected override ZXing.BarcodeFormat BarcodeFormat => ZXing.BarcodeFormat.CODE_39;
    }

    public sealed class QrCode : BarcodeBase
    {
        protected override ZXing.BarcodeFormat BarcodeFormat => ZXing.BarcodeFormat.QR_CODE;
    }
}
