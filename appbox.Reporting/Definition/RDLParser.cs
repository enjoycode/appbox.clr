using System;
using System.Xml;
using appbox.Reporting.Resources;

namespace appbox.Reporting.RDL
{
    /// <summary>
    ///	The RDLParser class takes an XML representation (either string or DOM) of a
    ///	RDL file and compiles a Report.
    /// </summary>
    public class RDLParser
    {
        private XmlDocument _RdlDocument;
        /// <summary>
        /// the RDL XML syntax
        /// </summary>
        internal XmlDocument RdlDocument
        {
            get { return _RdlDocument; }
            set
            {
                // With a new document existing report is not valid
                _RdlDocument = value;
                bPassed = false;
                _Report = null;
            }
        }

        bool bPassed = false;       // has Report passed definition
        Report _Report = null;  // The report; complete if bPassed true

        /// <summary>
        /// Get the compiled report.
        /// Only return a report if it has been fully constructed
        /// </summary>
        public Report Report => bPassed ? _Report : null;

        /// <summary>
        /// For shared data sources, the DataSourceReferencePassword is the user phrase
        /// used to decrypt the report.
        /// </summary>
        public NeedPassword DataSourceReferencePassword { get; set; }

        /// <summary>
        /// Folder is the location of the report.
        /// </summary>
        public string Folder { get; set; }

        /// <summary>
        /// ConnectionString to overwrite
        /// </summary>
        public string OverwriteConnectionString { get; set; }

        /// <summary>
        /// overwrite ConnectionString in subreport
        /// </summary>
        public bool OverwriteInSubreport { get; set; }

        /// <summary>
        /// EBN 31/03/2014
        /// Cross-Object parameters
        /// The SubReportGetContent delegate handles a callback to get the content of a subreport from another source (server, memory, database, ...)
        /// </summary>
        /// <param name="SubReportName"></param>
        /// <returns></returns>
        public CrossDelegate OnSubReportGetContent = new CrossDelegate();

        /// <summary>
        /// RDLParser takes in an RDL XML file and creates the
        /// definition that will be used at runtime.  It validates
        /// that the syntax is correct according to the specification.
        /// </summary>
        public RDLParser(string xml)
        {
            try
            {
                _RdlDocument = new XmlDocument();
                _RdlDocument.PreserveWhitespace = false;
                _RdlDocument.LoadXml(xml);
            }
            catch (XmlException ex)
            {
                throw new ParserException(Strings.RDLParser_ErrorP_XMLFailed + ex.Message);
            }
        }

        /// <summary>
        /// RDLParser takes in an RDL XmlDocument and creates the
        /// definition that will be used at runtime.  It validates
        /// that the syntax is correct according to the specification.
        /// </summary>		
        public RDLParser(XmlDocument xml) // preparsed XML
        {
            _RdlDocument = xml;
        }

        /// <summary>
        /// Returns a parsed RPL report instance.
        /// </summary>
        /// <returns>A Report instance.</returns>
        public Report Parse()
        {
            return Parse(0);
        }

        internal Report Parse(int oc)
        {
            if (_RdlDocument == null)   // no document?
                return null;            // nothing to do
            else if (bPassed)           // If I've already parsed it
                return _Report;         // then return existing Report
                                        //  Need to create a report.
            XmlNode xNode;
            xNode = _RdlDocument.LastChild;
            if (xNode == null || xNode.Name != "Report")
            {
                throw new ParserException(Strings.RDLParser_ErrorP__NoReport);
            }

            ReportLog rl = new ReportLog();     // create a report log

            ReportDefn rd = new ReportDefn(xNode, rl, Folder, DataSourceReferencePassword,
                oc, OnSubReportGetContent, OverwriteConnectionString, OverwriteInSubreport);
            _Report = new Report(rd);

            bPassed = true;

            return _Report;
        }

    }
}
