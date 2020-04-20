using System;
using System.Xml;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

namespace appbox.Reporting.RDL
{
    /// <summary>
    /// Delegate used to ask for a Data Source Reference password used to decrypt the file.
    /// </summary>
    public delegate string NeedPassword();

    ///<summary>
    /// Main Report definition; this is the top of the tree that contains the complete
    /// definition of a instance of a report.
    ///</summary>
    [Serializable]
    public class ReportDefn
    {
        internal int _ObjectCount = 0;  // master object counter
        internal ReportLog rl;  // report log

        private Name _Name;             // Name of the report
        internal string Name
        {
            get { return _Name?.Nm; }
            set { _Name = new Name(value); }
        }

        internal NeedPassword GetDataSourceReferencePassword = null;

        /// <summary>
        /// Description of the report
        /// </summary>
        internal string Description { get; set; }

        /// <summary>
        /// Author of the report
        /// </summary>
        internal string Author { get; set; }

        /// <summary>
        /// Rate at which the report page automatically refreshes, in seconds.  Must be nonnegative.
        /// </summary>
        internal int AutoRefresh { get; set; }

        /// <summary>
        /// Parameters for the report
        /// </summary>
        internal ReportParameters ReportParameters { get; set; }

        /// <summary>
        /// Describes the data that is displayed as part of the report
        /// </summary>
        internal DataSourcesDefn DataSourcesDefn { get; }

        /// <summary>
        /// Describes the data that is displayed as part of the report
        /// </summary>
        internal DataSetsDefn DataSetsDefn { get; }

        /// <summary>
        /// The header that is output at the top of each page of the report.
        /// </summary>
        internal PageHeader PageHeader { get; set; }

        /// <summary>
        /// Describes how the body of the report is structured
        /// </summary>
        internal Body Body { get; set; }

        /// <summary>
        /// The footer that is output at the bottom of each page of the report.
        /// </summary>
        internal PageFooter PageFooter { get; set; }

        /// <summary>
        /// Custom information to be handed to the report engine
        /// </summary>
        internal Custom Customer { get; set; }

        RSize _Width;           // Width of the report
        RSize _PageHeight;      // Default height for the report.  Default is 11 in.
        RSize _PageWidth;       // Default width for the report. Default is 8.5 in.
        RSize _LeftMargin;      // Width of the left margin. Default: 0 in
        RSize _RightMargin;     // Width of the right margin. Default: 0 in
        RSize _TopMargin;       // Width of the top margin. Default: 0 in
        RSize _BottomMargin;    // Width of the bottom margin. Default: 0 in

        internal RSize Width
        {
            get
            {
                if (_Width == null)         // Shouldn't be need since technically Width is required (I let it slip)	
                    _Width = PageWidth;     // Not specified; assume page width

                return _Width;
            }
            set { _Width = value; }
        }

        internal RSize PageHeight
        {
            get
            {
                if (Subreport != null)
                    return Subreport.OwnerReport.PageHeight;

                if (_PageHeight == null)            // default height is 11 inches
                    _PageHeight = new RSize(this, "11 in");
                return _PageHeight;
            }
            set { _PageHeight = value; }
        }

        internal float PageHeightPoints
        {
            get
            {
                return PageHeight.Points;
            }
        }

        internal RSize PageWidth
        {
            get
            {
                if (Subreport != null)
                    return Subreport.OwnerReport.PageWidth;

                if (_PageWidth == null)             // default width is 8.5 inches
                    _PageWidth = new RSize(this, "8.5 in");

                return _PageWidth;
            }
            set { _PageWidth = value; }
        }

        internal float PageWidthPoints => PageWidth.Points;

        internal RSize LeftMargin
        {
            get
            {
                if (Subreport != null)
                {
                    if (Subreport.Left == null)
                        Subreport.Left = new RSize(this, "0 in");
                    return Subreport.Left + Subreport.OwnerReport.LeftMargin;
                }

                if (_LeftMargin == null)
                    _LeftMargin = new RSize(this, "0 in");
                return _LeftMargin;
            }
            set { _LeftMargin = value; }
        }

        internal RSize RightMargin
        {
            get
            {
                if (Subreport != null)
                    return Subreport.OwnerReport.RightMargin;

                if (_RightMargin == null)
                    _RightMargin = new RSize(this, "0 in");
                return _RightMargin;
            }
            set { _RightMargin = value; }
        }

        internal RSize TopMargin
        {
            get
            {
                if (Subreport != null)
                    return Subreport.OwnerReport.TopMargin;

                if (_TopMargin == null)
                    _TopMargin = new RSize(this, "0 in");
                return _TopMargin;
            }
            set { _TopMargin = value; }
        }

        internal float TopOfPage
        {
            get
            {
                if (Subreport != null)
                    return Subreport.OwnerReport.TopOfPage;

                float y = TopMargin.Points;
                if (this.PageHeader != null)
                    y += PageHeader.Height.Points;
                return y;
            }
        }

        internal RSize BottomMargin
        {
            get
            {
                if (Subreport != null)
                    return Subreport.OwnerReport.BottomMargin;

                if (_BottomMargin == null)
                    _BottomMargin = new RSize(this, "0 in");
                return _BottomMargin;
            }
            set { _BottomMargin = value; }
        }

        internal float BottomOfPage     // this is the y coordinate just above the page footer
        {
            get
            {
                if (Subreport != null)
                    return Subreport.OwnerReport.BottomOfPage;

                // calc size of bottom margin + footer
                float y = BottomMargin.Points;
                if (PageFooter != null)
                    y += PageFooter.Height.Points;

                // now get the absolute coordinate
                y = PageHeight.Points - y;
                return y;
            }
        }

        /// <summary>
        /// Images embedded within the report
        /// </summary>
        internal EmbeddedImages EmbeddedImages { get; set; }

        /// <summary>
        /// The primary language of the text. Default is server language.
        /// </summary>
        internal Expression Language { get; set; }

        /// <summary>
        /// The <Code> element support; ie VB functions
        /// </summary>
        internal Code Code { get; }

        /// <summary>
        /// Code modules to make available to the report for use in expressions.
        /// </summary>
        internal CodeModules CodeModules { get; set; }

        /// <summary>
        /// Classes 0-1 Element Classes to instantiate during report initialization
        /// </summary>
        internal Classes Classes { get; set; }

        /// <summary>
        /// The location to a transformation to apply to a report data rendering.
        /// This can be a full folder path (e.g. �/xsl/xfrm.xsl�), relative path (e.g. �xfrm.xsl�).
        /// </summary>
        internal string DataTransform { get; set; }

        /// <summary>
        /// The schema or namespace to use for a report data rendering.
        /// </summary>
        internal string DataSchema { get; set; }

        private string _DataElementName;
        /// <summary>
        /// Name of a top level element that
        /// </summary>
        internal string DataElementName
        {
            get { return _DataElementName ?? "Report"; }
            set { _DataElementName = value; }
        }

        /// <summary>
        /// Indicates whether textboxes should render as elements or attributes.
        /// </summary>
        internal DataElementStyleEnum DataElementStyle { get; set; }

        /// <summary>
        /// null if top level report; otherwise the subreport that loaded the report
        /// </summary>
        internal Subreport Subreport { get; set; }

        /// <summary>
        /// true if report contains a subreport
        /// </summary>
        internal bool ContainsSubreport { get; set; }

        int _DynamicNames = 0;      // used for creating names on the fly during parsing

        /// <summary>
        /// contains all function that implement ICacheData
        /// </summary>
        internal List<ICacheData> DataCache { get; }

        /// <summary>
        /// contains global and user properties
        /// </summary>
        internal IDictionary LUGlobals { get; private set; }

        /// <summary>
        /// contains global and user properties
        /// </summary>
        internal IDictionary LUUser { get; private set; }

        /// <summary>
        /// all TextBoxes in the report
        /// </summary>
        internal IDictionary LUReportItems { get; }

        /// <summary>
        /// for dynamic names
        /// </summary>
        internal IDictionary LUDynamicNames { get; }

        /// <summary>
        /// Datasets, Dataregions, grouping names
        /// </summary>
        internal IDictionary LUAggrScope { get; }

        /// <summary>
        /// Embedded images
        /// </summary>
        internal IDictionary LUEmbeddedImages { get; }

        /// <summary>
        /// temporary folder for looking up things during parse/finalpass
        /// </summary>
        internal string ParseFolder { get; }

        /// <summary>
        /// used for parsing of expressions; DONT USE AT RUNTIME
        /// </summary>
        internal Type CodeType { get; private set; }

        /// <summary>
        /// To overwrite ConnectionString
        /// </summary>
        internal string OverwriteConnectionString { get; set; }

        /// <summary>
        /// Overwrite ConnectionString in subreport too
        /// </summary>
        internal bool OverwriteInSubreport { get; set; }

        internal IDictionary LUReportParameters
        {
            get
            {
                return ReportParameters != null &&
                    ReportParameters.Items != null
                    ? ReportParameters.Items
                    : null;
            }
        }

        internal int ErrorMaxSeverity => rl == null ? 0 : rl.MaxSeverity;

        internal IList ErrorItems => rl?.ErrorItems;

        /// <summary>
        /// EBN 31/03/2014
        /// Cross object
        /// </summary>
        public CrossDelegate SubReportGetContent { get; set; } = new CrossDelegate();

        // Constructor
        internal ReportDefn(XmlNode xNode, ReportLog replog, string folder,
            NeedPassword getpswd, int objcount, CrossDelegate crossdel,
            string overwriteConnectionString, bool overwriteInSubreport)        // report has no parents
        {
            rl = replog;                // used for error reporting
            _ObjectCount = objcount;    // starting number for objects in this report; 0 other than for subreports
            GetDataSourceReferencePassword = getpswd;
            ParseFolder = folder;
            Description = null;
            Author = null;
            AutoRefresh = -1;
            DataSourcesDefn = null;
            DataSetsDefn = null;
            Body = null;
            _Width = null;
            PageHeader = null;
            PageFooter = null;
            _PageHeight = null;
            _PageWidth = null;
            _LeftMargin = null;
            _RightMargin = null;
            _TopMargin = null;
            _BottomMargin = null;
            EmbeddedImages = null;
            Language = null;
            CodeModules = null;
            Code = null;
            Classes = null;
            DataTransform = null;
            DataSchema = null;
            _DataElementName = null;
            DataElementStyle = DataElementStyleEnum.AttributeNormal;
            LUReportItems = new Hashtable();       // to hold all the textBoxes
            LUAggrScope = new ListDictionary();    // to hold all dataset, dataregion, grouping names
            LUEmbeddedImages = new ListDictionary();   // probably not very many
            LUDynamicNames = new Hashtable();
            DataCache = new List<ICacheData>();

            // EBN 30/03/2014
            SubReportGetContent = crossdel;

            OverwriteConnectionString = overwriteConnectionString;
            OverwriteInSubreport = overwriteInSubreport;

            // Run thru the attributes
            foreach (XmlAttribute xAttr in xNode.Attributes)
            {
                switch (xAttr.Name)
                {
                    case "Name":
                        _Name = new Name(xAttr.Value);
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
                    case "Description":
                        Description = xNodeLoop.InnerText;
                        break;
                    case "Author":
                        Author = xNodeLoop.InnerText;
                        break;
                    case "AutoRefresh":
                        AutoRefresh = XmlUtil.Integer(xNodeLoop.InnerText);
                        break;
                    case "DataSources":
                        DataSourcesDefn = new DataSourcesDefn(this, null, xNodeLoop);
                        break;
                    case "DataSets":
                        DataSetsDefn = new DataSetsDefn(this, null, xNodeLoop);
                        break;
                    case "Body":
                        Body = new Body(this, null, xNodeLoop);
                        break;
                    case "ReportParameters":
                        ReportParameters = new ReportParameters(this, null, xNodeLoop);
                        break;
                    case "Width":
                        _Width = new RSize(this, xNodeLoop);
                        break;
                    case "PageHeader":
                        PageHeader = new PageHeader(this, null, xNodeLoop);
                        break;
                    case "PageFooter":
                        PageFooter = new PageFooter(this, null, xNodeLoop);
                        break;
                    case "PageHeight":
                        _PageHeight = new RSize(this, xNodeLoop);
                        break;
                    case "PageWidth":
                        _PageWidth = new RSize(this, xNodeLoop);
                        break;
                    case "LeftMargin":
                        _LeftMargin = new RSize(this, xNodeLoop);
                        break;
                    case "RightMargin":
                        _RightMargin = new RSize(this, xNodeLoop);
                        break;
                    case "TopMargin":
                        _TopMargin = new RSize(this, xNodeLoop);
                        break;
                    case "BottomMargin":
                        _BottomMargin = new RSize(this, xNodeLoop);
                        break;
                    case "EmbeddedImages":
                        EmbeddedImages = new EmbeddedImages(this, null, xNodeLoop);
                        break;
                    case "Language":
                        Language = new Expression(this, null, xNodeLoop, ExpressionType.String);
                        break;
                    case "Code":
                        Code = new Code(this, null, xNodeLoop);
                        break;
                    case "CodeModules":
                        CodeModules = new CodeModules(this, null, xNodeLoop);
                        break;
                    case "Classes":
                        Classes = new Classes(this, null, xNodeLoop);
                        break;
                    case "DataTransform":
                        DataTransform = xNodeLoop.InnerText;
                        break;
                    case "DataSchema":
                        DataSchema = xNodeLoop.InnerText;
                        break;
                    case "DataElementName":
                        _DataElementName = xNodeLoop.InnerText;
                        break;
                    case "DataElementStyle":
                        DataElementStyle = RDL.DataElementStyle.GetStyle(xNodeLoop.InnerText, this.rl);
                        break;
                    default:
                        // don't know this element - log it
                        rl.LogError(4, "Unknown Report element '" + xNodeLoop.Name + "' ignored.");
                        break;
                }
            }

            if (Body == null)
                rl.LogError(8, "Body not specified for report.");

            if (_Width == null)
                rl.LogError(4, "Width not specified for report.  Assuming page width.");

            if (rl.MaxSeverity <= 4)    // don't do final pass if already have serious errors
            {
                FinalPass(folder);  // call final parser pass for expression resolution
            }

            // Cleanup any dangling resources
            if (DataSourcesDefn != null)
                DataSourcesDefn.CleanUp(null);
        }

        //
        void FinalPass(string folder)
        {
            // Now do some addition validation and final preparation

            // Create the Globals and User lookup dictionaries
            LUGlobals = new ListDictionary();  // if entries grow beyond 10; make hashtable
            LUGlobals.Add("PageNumber", new FunctionPageNumber());
            LUGlobals.Add("TotalPages", new FunctionTotalPages());
            LUGlobals.Add("ExecutionTime", new FunctionExecutionTime());
            LUGlobals.Add("ReportFolder", new FunctionReportFolder());
            LUGlobals.Add("ReportName", new FunctionReportName());
            LUUser = new ListDictionary();     // if entries grow beyond 10; make hashtable
            LUUser.Add("UserID", new FunctionUserID());
            LUUser.Add("Language", new FunctionUserLanguage());
            if (CodeModules != null)
            {
                CodeModules.FinalPass();
                CodeModules.LoadModules();
            }
            if (Classes != null)
            {
                Classes.FinalPass();
                // _Classes.Load();
            }
            if (Code != null)
            {
                Code.FinalPass();
                CodeType = Code.CodeType();
            }

            if (ReportParameters != null)      // report parameters might be used in data source connection strings
                ReportParameters.FinalPass();
            if (DataSourcesDefn != null)
                DataSourcesDefn.FinalPass();
            if (DataSetsDefn != null)
                DataSetsDefn.FinalPass();
            Body.FinalPass();
            if (PageHeader != null)
                PageHeader.FinalPass();
            if (PageFooter != null)
                PageFooter.FinalPass();
            if (EmbeddedImages != null)
                EmbeddedImages.FinalPass();
            if (Language != null)
                Language.FinalPass();

            DataCache.TrimExcess();    // reduce size of array of expressions that cache data
            return;
        }

        internal int GetObjectNumber()
        {
            _ObjectCount++;
            return _ObjectCount;
        }

        internal void SetObjectNumber(int oc)
        {
            _ObjectCount = oc;
        }

        // Obtain the data for the report
        internal bool RunGetData(Report rpt, IDictionary parms)
        {
            bool bRows = false;
            // Step 1- set the parameter values for the runtime
            if (parms != null && ReportParameters != null)
            {
                ReportParameters.SetRuntimeValues(rpt, parms);	// set the parameters
            }

            // Step 2- prep the datasources (ie connect and execute the queries)
            if (this.DataSourcesDefn != null)
            {
                DataSourcesDefn.ConnectDataSources(rpt);
            }

            // Step 3- obtain the data; applying filters
            if (DataSetsDefn != null)
            {
                ResetCachedData(rpt);
                bRows = DataSetsDefn.GetData(rpt);
            }

            // Step 4- cleanup any DB connections
            if (DataSourcesDefn != null)
            {
                if (!this.ContainsSubreport)
                {
                    DataSourcesDefn.CleanUp(rpt);	// no subreports means that nothing will use this transaction
                }
            }

            return bRows;
        }

        internal string CreateDynamicName(object ro)
        {
            _DynamicNames++;                    // increment the name generator
            string name = "o" + _DynamicNames.ToString();
            LUDynamicNames.Add(name, ro);
            return name;
        }

        private void ResetCachedData(Report rpt)
        {
            foreach (ICacheData icd in this.DataCache)
            {
                icd.ClearCache(rpt);
            }
        }

        internal void Run(IPresent ip)
        {
            if (Subreport == null)
            {   // do true intialization
                ip.Start();
            }

            if (ip.IsPagingNeeded())
            {
                RunPage(ip);
            }
            else
            {
                if (PageHeader != null && !(ip is RenderXml))
                    PageHeader.Run(ip, null);
                Body.Run(ip, null);
                if (PageFooter != null && !(ip is RenderXml))
                    PageFooter.Run(ip, null);
            }

            if (Subreport == null)
                ip.End();

            if (DataSourcesDefn != null)
                DataSourcesDefn.CleanUp(ip.Report());  // datasets may not have been cleaned up
        }

        internal void RunPage(IPresent ip)
        {
            Pages pgs = new Pages(ip.Report());
            try
            {
                Page p = new Page(1);               // kick it off with a new page
                pgs.AddPage(p);

                // Create all the pages
                Body.RunPage(pgs);

                if (pgs.LastPage.IsEmpty() && pgs.PageCount > 1)    // get rid of extraneous pages which
                    pgs.RemoveLastPage();           //   can be caused by region page break at end

                // Now create the headers and footers for all the pages (as needed)
                if (PageHeader != null)
                    PageHeader.RunPage(pgs);
                if (PageFooter != null)
                    PageFooter.RunPage(pgs);

                pgs.SortPageItems();             // Handle ZIndex ordering of pages

                ip.RunPages(pgs);
            }
            finally
            {
                pgs.CleanUp();      // always want to make sure we clean this up since 
                if (DataSourcesDefn != null)
                    DataSourcesDefn.CleanUp(pgs.Report);   // ensure datasets are cleaned up
            }

            return;
        }

        internal string EvalLanguage(Report rpt, Row r)
        {
            if (Language == null)
            {
                CultureInfo ci = CultureInfo.CurrentCulture;
                return ci.Name;
            }

            return Language.EvaluateString(rpt, r);
        }

        internal void ErrorReset()
        {
            if (rl == null)
                return;
            rl.Reset();
            return;
        }
    }
}
