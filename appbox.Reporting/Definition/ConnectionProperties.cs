using System;
using System.Xml;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// Information about how to connect to the data source.
    ///</summary>
    [Serializable]
    internal class ConnectionProperties : ReportLink
    {
        /// <summary>
        /// The type of the data source. This will determine
        // the syntax of the Connectstring and
        // CommandText. Supported types are SQL, OLEDB, ODBC, Oracle
        /// </summary>
        internal string DataProvider { get; set; }

        private readonly Expression _ConnectString;
        /// <summary>
        /// The connection string for the data source
        /// </summary>
        internal string ConnectstringValue => _ConnectString?.Source;

        /// <summary>
        /// Indicates that this data source should connected to using integrated security
        /// </summary>
        internal bool IntegratedSecurity { get; set; }

        /// <summary>
        /// The prompt displayed to the user when
        /// prompting for database credentials for this data source.
        /// </summary>
        internal string Prompt { get; set; }

        internal ConnectionProperties(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            DataProvider = null;
            _ConnectString = null;
            IntegratedSecurity = false;
            Prompt = null;

            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                switch (xNodeLoop.Name)
                {
                    case "DataProvider":
                        DataProvider = xNodeLoop.InnerText;
                        break;
                    case "ConnectString":
                        _ConnectString = String.IsNullOrWhiteSpace(r.OverwriteConnectionString)
                            ? new Expression(r, this, xNodeLoop, ExpressionType.String)
                            : new Expression(r, this, r.OverwriteConnectionString, ExpressionType.String);
                        break;
                    case "IntegratedSecurity":
                        IntegratedSecurity = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    case "Prompt":
                        Prompt = xNodeLoop.InnerText;
                        break;
                    default:
                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, "Unknown ConnectionProperties element '" + xNodeLoop.Name + "' ignored.");
                        break;
                }
            }
            if (DataProvider == null)
                OwnerReport.rl.LogError(8, "ConnectionProperties DataProvider is required.");
            if (_ConnectString == null)
                OwnerReport.rl.LogError(8, "ConnectionProperties ConnectString is required.");
        }

        override internal void FinalPass()
        {
            if (_ConnectString != null)
                _ConnectString.FinalPass();
            return;
        }

        internal string Connectstring(Report rpt)
        {
            return _ConnectString.EvaluateString(rpt, null);
        }

    }
}
