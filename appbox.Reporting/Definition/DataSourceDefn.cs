using System;
using System.Xml;
using System.Data;
using System.IO;
using appbox.Reporting.Resources;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// Information about the data source (e.g. a database connection string).
    ///</summary>
    [Serializable]
    internal class DataSourceDefn : ReportLink
    {
        /// <summary>
        /// The name of the data source Must be unique within the report
        /// </summary>
        internal Name Name { get; set; }

        /// <summary>
        /// Indicates the data sets that use this data
        /// source should be executed in a single transaction.
        /// </summary>
        internal bool Transaction { get; set; }

        /// <summary>
        /// Information about how to connect to the data source
        /// </summary>
        internal ConnectionProperties ConnectionProperties { get; set; }

        /// <summary>
        /// The full path (e.g. �/salesreports/salesdatabase�) or relative path
        /// (e.g. �salesdatabase�) to a data source reference.
        /// Relative paths start in the same location as the report.		
        /// </summary>
        internal string DataSourceReference { get; set; }

        [NonSerialized] IDbConnection _ParseConnection; // while parsing we sometimes need to connect

        internal DataSourceDefn(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            Name = null;
            Transaction = false;
            ConnectionProperties = null;
            DataSourceReference = null;
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
                    case "Transaction":
                        Transaction = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    case "ConnectionProperties":
                        ConnectionProperties = new ConnectionProperties(r, this, xNodeLoop);
                        break;
                    case "DataSourceReference":
                        DataSourceReference = xNodeLoop.InnerText;
                        break;
                    default:
                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, "Unknown DataSource element '" + xNodeLoop.Name + "' ignored.");
                        break;
                }
            }
            if (Name == null)
                OwnerReport.rl.LogError(8, "DataSource Name is required but not specified.");
            else if (ConnectionProperties == null && DataSourceReference == null)
                OwnerReport.rl.LogError(8, string.Format("Either ConnectionProperties or DataSourceReference must be specified for DataSource {0}.", this.Name.Nm));
            else if (ConnectionProperties != null && DataSourceReference != null)
                OwnerReport.rl.LogError(8, string.Format("Either ConnectionProperties or DataSourceReference must be specified for DataSource {0} but not both.", this.Name.Nm));
        }

        override internal void FinalPass()
        {
            if (ConnectionProperties != null)
                ConnectionProperties.FinalPass();

            ConnectDataSource(null);
        }

        internal bool IsConnected(Report rpt)
        {
            return GetConnection(rpt) == null ? false : true;
        }

        internal bool AreSameDataSource(DataSourceDefn dsd)
        {
            if (this.DataSourceReference != null &&
                this.DataSourceReference == dsd.DataSourceReference)
                return true;        // datasource references are the same

            if (this.ConnectionProperties == null ||
                dsd.ConnectionProperties == null)
                return false;

            ConnectionProperties cp1 = this.ConnectionProperties;
            ConnectionProperties cp2 = dsd.ConnectionProperties;
            return (cp1.DataProvider == cp2.DataProvider &&
                cp1.ConnectstringValue == cp2.ConnectstringValue &&
                cp1.IntegratedSecurity == cp2.IntegratedSecurity);
        }

        internal bool ConnectDataSource(Report rpt)
        {
            IDbConnection cn = GetConnection(rpt);
            if (cn != null)
            {
                return true;
            }

            if (DataSourceReference != null)
            {
                ConnectDataSourceReference(rpt);	// this will create a _ConnectionProperties
            }

            if (ConnectionProperties == null ||
                ConnectionProperties.ConnectstringValue == null)
            {
                return false;
            }

            bool rc = false;
            try
            {
                cn = RdlEngineConfig.GetConnection(ConnectionProperties.DataProvider,
                    ConnectionProperties.Connectstring(rpt));
                if (cn != null)
                {
                    cn.Open();
                    rc = true;
                }
            }
            catch (Exception e)
            {
                string err = string.Format("DataSource '{0}'.\r\n{1}", Name,
                    e.InnerException == null ? e.Message : e.InnerException.Message);
                if (rpt == null)
                    OwnerReport.rl.LogError(4, err);    // error occurred during parse phase
                else
                    rpt.rl.LogError(4, err);
                if (cn != null)
                {
                    cn.Close();
                    cn = null;
                }
            }

            if (cn != null)
                SetSysConnection(rpt, cn);
            else
            {
                string err = string.Format("Unable to connect to datasource '{0}'.", Name.Nm);
                if (rpt == null)
                    OwnerReport.rl.LogError(4, err);    // error occurred during parse phase
                else
                    rpt.rl.LogError(4, err);
            }
            return rc;
        }

        void ConnectDataSourceReference(Report rpt)
        {
            if (ConnectionProperties != null)
                return;

            try
            {
                string file;
                string folder = rpt == null ? OwnerReport.ParseFolder : rpt.Folder;
                if (folder == null)
                {   // didn't specify folder; check to see if we have a fully formed name 
                    if (!DataSourceReference.EndsWith(".dsr", StringComparison.InvariantCultureIgnoreCase))
                        file = DataSourceReference + ".dsr";
                    else
                        file = DataSourceReference;
                }
                else if (DataSourceReference[0] != Path.DirectorySeparatorChar)
                    file = folder + Path.DirectorySeparatorChar + DataSourceReference + ".dsr";
                else
                    file = folder + DataSourceReference + ".dsr";

                string pswd = OwnerReport.GetDataSourceReferencePassword == null ?
                                    null : OwnerReport.GetDataSourceReferencePassword();
                if (pswd == null)
                    throw new Exception(Strings.DataSourceDefn_Error_NoPasswordForDSR);

                string xml = RDL.DataSourceReference.Retrieve(file, pswd);
                XmlDocument xDoc = new XmlDocument();
                xDoc.LoadXml(xml);
                XmlNode xNodeLoop = xDoc.FirstChild;

                ConnectionProperties = new ConnectionProperties(OwnerReport, this, xNodeLoop);
                ConnectionProperties.FinalPass();
            }
            catch (Exception e)
            {
                OwnerReport.rl.LogError(4, e.Message);
                ConnectionProperties = null;
            }
            return;
        }

        internal bool IsUserConnection(Report rpt)
        {
            if (rpt == null)
                return false;

            object uc = rpt.Cache.Get(this, "UserConnection");
            return uc == null ? false : true;
        }

        internal void SetUserConnection(Report rpt, IDbConnection cn)
        {
            if (cn == null)
                rpt.Cache.Remove(this, "UserConnection");
            else
                rpt.Cache.AddReplace(this, "UserConnection", cn);
        }

        private void SetSysConnection(Report rpt, IDbConnection cn)
        {
            if (rpt == null)
                _ParseConnection = cn;
            else if (cn == null)
                rpt.Cache.Remove(this, "SysConnection");
            else
                rpt.Cache.Add(this, "SysConnection", cn);
        }

        internal IDbConnection GetConnection(Report rpt)
        {
            IDbConnection cn;

            if (rpt == null)
            {
                return _ParseConnection;
            }

            cn = rpt.Cache.Get(this, "UserConnection") as IDbConnection;
            if (cn == null)
            {
                cn = rpt.Cache.Get(this, "SysConnection") as IDbConnection;
            }
            return cn;
        }

        internal void CleanUp(Report rpt)
        {
            if (IsUserConnection(rpt))
                return;
            IDbConnection cn = GetConnection(rpt);
            if (cn == null)
                return;

            try
            {
                cn.Close();
                // cn.Dispose();		// not good for connection pooling
            }
            catch (Exception ex)
            {   // report the error but keep going
                if (rpt != null)
                    rpt.rl.LogError(4, string.Format("Error closing connection. {0}", ex.Message));
                else
                    this.OwnerReport.rl.LogError(4, string.Format("Error closing connection. {0}", ex.Message));
            }
            SetSysConnection(rpt, null);    // get rid of connection from cache
            return;
        }

        internal IDbConnection SqlConnect(Report rpt)
        {
            return GetConnection(rpt);
        }

    }
}
