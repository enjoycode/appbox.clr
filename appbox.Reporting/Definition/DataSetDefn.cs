using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Data;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// Information about a set of data that will be retrieved when report data is requested.
    ///</summary>
    [Serializable]
    internal class DataSetDefn : ReportLink
    {
        /// <summary>
        /// Name of the data set
        // Cannot be the same name as any data region or grouping
        /// </summary>
        internal Name Name { get; set; }

        /// <summary>
        /// The fields in the data set
        /// </summary>
        internal Fields Fields { get; set; }

        /// <summary>
        /// Information about the data source, including connection information, query,
        /// etc. required to get the data from the data source.
        /// </summary>
        internal Query Query { get; set; }

        private readonly string _XmlRowData; // User specified data; instead of invoking query we use inline XML data
                                             //   This is particularlly useful for testing and reporting bugs when
                                             //   you don't have access to the datasource.

        private readonly string _XmlRowFile; //   - File should be loaded for user data; if not found use XmlRowData

        /// <summary>
        /// Indicates if the data is case sensitive; true/false/auto
        /// if auto; should query data provider; Default false if data provider doesn't support.
        /// </summary>
        internal TrueFalseAutoEnum CaseSensitivity { get; set; }

        /// <summary>
        /// The locale to use for the collation sequence for sorting data.
        ///  See Microsoft SQL Server collation codes (http://msdn.microsoft.com/library/enus/tsqlref/ts_ca-co_2e95.asp).
        /// If no Collation is specified, the application should attempt to derive the collation setting by
        /// querying the data provider.
        /// Defaults to the application�s locale settings if
        /// the data provider does not support that method
        /// or returns an unsupported or invalid value
        /// </summary>
        internal string Collation { get; set; }

        /// <summary>
        /// Indicates whether the data is accent sensitive True | False | Auto (Default)
        /// If Auto is specified, the application should attempt to derive the accent sensitivity setting
        /// by querying the data provider.
        /// Defaults to False if the data provider does not support that method.
        /// </summary>
        internal TrueFalseAutoEnum AccentSensitivity { get; set; }

        /// <summary>
        /// Indicates if the data is kanatype sensitive True | False | Auto (Default)
        /// If Auto is specified, the Application should  to derive the kanatype sensitivity
        /// setting by querying the data provider.
        /// Defaults to False if the data provider does not support that method.
        /// </summary>
        internal TrueFalseAutoEnum KanatypeSensitivity { get; set; }

        /// <summary>
        /// Indicates if the data is width sensitive True | False | Auto (Default)
        /// If Auto is specified, the Application should attempt to derive the width sensitivity setting by
        /// querying the data provider.
        /// Defaults to False if the data provider does not support that method.
        /// </summary>
        internal TrueFalseAutoEnum WidthSensitivity { get; set; }

        /// <summary>
        /// Filters to apply to each row of data in the data set
        /// </summary>
        internal Filters Filters { get; set; }

        private List<Textbox> _HideDuplicates;  // holds any textboxes that use this as a hideduplicate scope

        internal DataSetDefn(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            Name = null;
            Fields = null;
            Query = null;
            CaseSensitivity = TrueFalseAutoEnum.True;
            Collation = null;
            AccentSensitivity = TrueFalseAutoEnum.False;
            KanatypeSensitivity = TrueFalseAutoEnum.False;
            WidthSensitivity = TrueFalseAutoEnum.False;
            Filters = null;
            _HideDuplicates = null;
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
                    case "Fields":
                        Fields = new Fields(r, this, xNodeLoop);
                        break;
                    case "Query":
                        Query = new Query(r, this, xNodeLoop);
                        break;
                    case "Rows":    // Extension !!!!!!!!!!!!!!!!!!!!!!!
                        _XmlRowData = "<?xml version='1.0' encoding='UTF-8'?><Rows>" + xNodeLoop.InnerXml + "</Rows>";
                        foreach (XmlAttribute xA in xNodeLoop.Attributes)
                        {
                            if (xA.Name == "File")
                                _XmlRowFile = xA.Value;
                        }
                        break;
                    case "CaseSensitivity":
                        CaseSensitivity = TrueFalseAuto.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    case "Collation":
                        Collation = xNodeLoop.InnerText;
                        break;
                    case "AccentSensitivity":
                        AccentSensitivity = TrueFalseAuto.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    case "KanatypeSensitivity":
                        KanatypeSensitivity = TrueFalseAuto.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    case "WidthSensitivity":
                        WidthSensitivity = TrueFalseAuto.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    case "Filters":
                        Filters = new Filters(r, this, xNodeLoop);
                        break;
                    default:
                        OwnerReport.rl.LogError(4, "Unknown DataSet element '" + xNodeLoop.Name + "' ignored.");
                        break;
                }
            }
            if (this.Name != null)
                OwnerReport.LUAggrScope.Add(this.Name.Nm, this);        // add to referenceable TextBoxes
            else
                OwnerReport.rl.LogError(4, "Name attribute must be specified in a DataSet.");

            if (Query == null)
                OwnerReport.rl.LogError(8, "Query element must be specified in a DataSet.");

        }

        override internal void FinalPass()
        {
            if (Query != null)          // query must be resolved before fields
                Query.FinalPass();
            if (Fields != null)
                Fields.FinalPass();
            if (Filters != null)
                Filters.FinalPass();
            return;
        }

        internal void AddHideDuplicates(Textbox tb)
        {
            if (_HideDuplicates == null)
                _HideDuplicates = new List<Textbox>();
            _HideDuplicates.Add(tb);
        }

        internal bool GetData(Report rpt)
        {
            ResetHideDuplicates(rpt);

            bool bRows = false;
            if (_XmlRowData != null)
            {       // Override the query and provide data from XML
                string xdata = GetDataFile(rpt, _XmlRowFile);
                if (xdata == null)
                {
                    xdata = _XmlRowData;					// didn't find any data
                }

                bRows = Query.GetData(rpt, xdata, Fields, Filters);    // get the data (and apply the filters
                return bRows;
            }

            if (Query == null)
            {
                return bRows;
            }

            bRows = Query.GetData(rpt, this.Fields, Filters);	// get the data (and apply the filters
            return bRows;
        }

        private string GetDataFile(Report rpt, string file)
        {
            if (file == null)		// no file no data
            {
                return null;
            }

            StreamReader fs = null;
            string d = null;
            string fullpath;
            string folder = rpt.Folder;
            if (folder == null || folder.Length == 0)
            {
                fullpath = file;
            }
            else
            {
                fullpath = folder + Path.DirectorySeparatorChar + file;
            }

            try
            {
                fs = new StreamReader(fullpath);
                d = fs.ReadToEnd();
            }
            catch (FileNotFoundException fe)
            {
                rpt.rl.LogError(4, string.Format("XML data file {0} not found.\n{1}", fullpath, fe.StackTrace));
                d = null;
            }
            catch (Exception ge)
            {
                rpt.rl.LogError(4, string.Format("XML data file error {0}\n{1}\n{2}", fullpath, ge.Message, ge.StackTrace));
                d = null;
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }
            return d;
        }

        internal void SetData(Report rpt, IDataReader dr)
        {
            Query.SetData(rpt, dr, Fields, Filters);       // get the data (and apply the filters
        }

        internal void SetData(Report rpt, DataTable dt)
        {
            Query.SetData(rpt, dt, Fields, Filters);
        }

        internal void SetData(Report rpt, XmlDocument xmlDoc)
        {
            Query.SetData(rpt, xmlDoc, Fields, Filters);
        }

        internal void SetData(Report rpt, IEnumerable ie)
        {
            Query.SetData(rpt, ie, Fields, Filters);
        }

        internal void ResetHideDuplicates(Report rpt)
        {
            if (_HideDuplicates == null)
            {
                return;
            }

            foreach (Textbox tb in _HideDuplicates)
            {
                tb.ResetPrevious(rpt);
            }
        }

    }
}
