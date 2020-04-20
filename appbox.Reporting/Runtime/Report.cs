using System;
using System.Xml;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// Main Report definition; this is the top of the tree that contains the complete
    /// definition of a instance of a report.
    ///</summary>
    public class Report
    {
        /// <summary>
        /// private definitions
        /// </summary>
        public ReportDefn ReportDefinition { get; private set; }

        private DataSources _DataSources;
        private DataSets _DataSets;
        private ICollection _UserParameters;    // User parameters

        public DataSources DataSources
        {
            get
            {
                if (ReportDefinition.DataSourcesDefn == null)
                    return null;
                if (_DataSources == null)
                    _DataSources = new DataSources(this, ReportDefinition.DataSourcesDefn);
                return _DataSources;
            }
        }

        public DataSets DataSets
        {
            get
            {
                if (ReportDefinition.DataSetsDefn == null)
                    return null;
                if (_DataSets == null)
                    _DataSets = new DataSets(this, ReportDefinition.DataSetsDefn);

                return _DataSets;
            }
        }

        /// <summary>
        /// User provided parameters to the report.  IEnumerable is a list of UserReportParameter.
        /// </summary>
        public ICollection UserReportParameters
        {
            get
            {
                if (_UserParameters != null)    // only create this once
                    return _UserParameters;     //  since it can be expensive to build

                if (ReportDefinition.ReportParameters == null || ReportDefinition.ReportParameters.Count <= 0)
                {
                    List<UserReportParameter> parms = new List<UserReportParameter>(1);
                    _UserParameters = parms;
                }
                else
                {
                    List<UserReportParameter> parms = new List<UserReportParameter>(ReportDefinition.ReportParameters.Count);
                    foreach (ReportParameter p in ReportDefinition.ReportParameters)
                    {
                        UserReportParameter urp = new UserReportParameter(this, p);
                        parms.Add(urp);
                    }
                    parms.TrimExcess();
                    _UserParameters = parms;
                }
                return _UserParameters;
            }
        }

        private int _RuntimeName = 0;       // used for the generation of unique runtime names
        private readonly IDictionary _LURuntimeName;     // Runtime names
        internal ReportLog rl;  // report log

        internal RCache Cache { get; private set; }

        // Some report runtime variables
        private string _Folder;         // folder name
        /// <summary>
        /// Get/Set the folder containing the report.
        /// </summary>
        public string Folder
        {
            get { return _Folder ?? ReportDefinition.ParseFolder; }
            set { _Folder = value; }
        }

        /// <summary>
        /// Get/Set the report name.  Usually this is the file name of the report sans extension.
        /// </summary>
        public string Name { get; set; }

        public string Description => ReportDefinition.Description;

        public string Author => ReportDefinition.Author;

        /// <summary>
        /// after rendering ASPHTML; this is separate
        /// </summary>
        public string CSS { get; private set; }

        /// <summary>
        /// after rendering ASPHTML; this is separate
        /// </summary>
        public string JavaScript { get; private set; }

        /// <summary>
        /// Instance of the class generated for the Code element
        /// </summary>
        internal object CodeInstance { get; }

        /// <summary>
        /// needed for page header/footer references
        /// </summary>
        internal Page CurrentPage { get; set; }

        private string _UserID;         // UserID of client executing the report
        private string _ClientLanguage; // Language code of the client executing the report.

        internal int PageNumber = 1;            // current page number
        internal int TotalPages = 1;            // total number of pages in report
        internal DateTime ExecutionTime;    // start time of report execution

        /// <summary>
        /// Returns the height of the page in points.
        /// </summary>
        public float PageHeightPoints => ReportDefinition.PageHeight.Points;

        /// <summary>
        /// Returns the width of the page in points.
        /// </summary>
        public float PageWidthPoints => ReportDefinition.PageWidthPoints;

        /// <summary>
        /// Returns the left margin size in points.
        /// </summary>
        public float LeftMarginPoints => ReportDefinition.LeftMargin.Points;

        /// <summary>
        /// Returns the right margin size in points.
        /// </summary>
        public float RightMarginPoints => ReportDefinition.RightMargin.Points;

        /// <summary>
        /// Returns the top margin size in points.
        /// </summary>
        public float TopMarginPoints => ReportDefinition.TopMargin.Points;

        /// <summary>
        /// Returns the bottom margin size in points.
        /// </summary>
        public float BottomMarginPoints => ReportDefinition.BottomMargin.Points;

        /// <summary>
        /// Returns the maximum severity of any error.  4 or less indicating report continues running.
        /// </summary>
        public int ErrorMaxSeverity => rl == null ? 0 : rl.MaxSeverity;

        /// <summary>
        /// List of errors encountered so far.
        /// </summary>
        public IList ErrorItems => rl?.ErrorItems;

        public NeedPassword GetDataSourceReferencePassword
        {
            get { return ReportDefinition.GetDataSourceReferencePassword; }
            set { ReportDefinition.GetDataSourceReferencePassword = value; }
        }

        /// <summary>
        /// Get/Set the UserID, that is the running user.
        /// </summary>
        public string UserID
        {
            get { return _UserID ?? Environment.UserName; }
            set { _UserID = value; }
        }

        /// <summary>
        /// Get/Set the three letter ISO language of the client of the report.
        /// </summary>
        public string ClientLanguage
        {
            get
            {
                if (ReportDefinition.Language != null)
                    return ReportDefinition.Language.EvaluateString(this, null);

                if (_ClientLanguage != null)
                    return _ClientLanguage;

                return CultureInfo.CurrentCulture.ThreeLetterISOLanguageName;
            }
            set { _ClientLanguage = value; }
        }

        /// <summary>
        /// When running subreport with merge transactions this is parent report connections
        /// </summary>
        internal DataSourcesDefn ParentConnections { get; set; }

        internal bool IsSubreportDataRetrievalDefined => SubreportDataRetrieval != null;

        /// <summary>
        /// Construct a runtime Report object using the compiled report definition.
        /// </summary>
        /// <param name="r"></param>
        public Report(ReportDefn r)
        {
            ReportDefinition = r;
            Cache = new RCache();
            rl = new ReportLog(r.rl);
            Name = r.Name;
            _UserParameters = null;
            _LURuntimeName = new ListDictionary();  // shouldn't be very many of these
            if (r.Code != null)
                CodeInstance = r.Code.Load(this);
            if (r.Classes != null)
                r.Classes.Load(this);
        }

        // Event for Subreport data retrieval
        /// <summary>
        /// Event invoked just prior to obtaining data for the subreport.  Setting DataSet 
        /// and DataConnection information during this event affects only this instance
        /// of the Subreport.
        /// </summary>
        public event EventHandler<SubreportDataRetrievalEventArgs> SubreportDataRetrieval;
        protected virtual void OnSubreportDataRetrieval(SubreportDataRetrievalEventArgs e)
        {
            SubreportDataRetrieval?.Invoke(this, e);
        }

        internal void SubreportDataRetrievalTriggerEvent()
        {
            if (SubreportDataRetrieval != null)
            {
                OnSubreportDataRetrieval(new SubreportDataRetrievalEventArgs(this));
            }

        }

        internal Rows GetPageExpressionRows(string exprname)
        {
            if (CurrentPage == null)
                return null;

            return CurrentPage.GetPageExpressionRows(exprname);
        }

        /// <summary>
        /// Read all the DataSets in the report
        /// </summary>
        /// <param name="parms"></param>
        public bool RunGetData(IDictionary parms)
        {
            ExecutionTime = DateTime.Now;
            bool bRows = ReportDefinition.RunGetData(this, parms);
            return bRows;
        }

        /// <summary>
        /// Renders the report using the requested presentation type.
        /// </summary>
        /// <param name="sg">IStreamGen for generating result stream</param>
        /// <param name="type">Presentation type: HTML, XML, PDF, or ASP compatible HTML</param>
        public void RunRender(IStreamGen sg, OutputPresentationType type)
        {
            RunRender(sg, type, "");
        }

        /// <summary>
        /// Renders the report using the requested presentation type.
        /// </summary>
        /// <param name="sg">IStreamGen for generating result stream</param>
        /// <param name="type">Presentation type: HTML, XML, PDF, MHT, or ASP compatible HTML</param>
        /// <param name="prefix">For HTML puts prefix allowing unique name generation</param>
        public void RunRender(IStreamGen sg, OutputPresentationType type, string prefix)
        {
            if (sg == null)
                throw new ArgumentException("IStreamGen argument cannot be null.", "sg");
            PageNumber = 1;     // reset page numbers
            TotalPages = 1;
            IPresent ip;
            MemoryStreamGen msg;
            switch (type)
            {
                case OutputPresentationType.PDF:
                    ip = new RenderPdf(this, sg);
                    ReportDefinition.Run(ip);
                    break;
                case OutputPresentationType.XML:
                    if (ReportDefinition.DataTransform != null && ReportDefinition.DataTransform.Length > 0)
                    {
                        msg = new MemoryStreamGen();
                        ip = new RenderXml(this, msg);
                        ReportDefinition.Run(ip);
                        RunRenderXmlTransform(sg, msg);
                    }
                    else
                    {
                        ip = new RenderXml(this, sg);
                        ReportDefinition.Run(ip);
                    }
                    break;
                case OutputPresentationType.MHTML:
                    RunRenderMht(sg);
                    break;
                case OutputPresentationType.CSV:
                    ip = new RenderCsv(this, sg);
                    ReportDefinition.Run(ip);
                    break;
                case OutputPresentationType.RTF:
                    ip = new RenderRtf(this, sg);
                    ReportDefinition.Run(ip);
                    break;
                case OutputPresentationType.ExcelTableOnly:
                    ip = new RenderExcel(this, sg);
                    ReportDefinition.Run(ip);
                    break;
                case OutputPresentationType.Excel2007:
                    throw new NotImplementedException();
                //ip = new RenderExcel2007(this, sg);
                //_Report.Run(ip);
                //break;
                case OutputPresentationType.ASPHTML:
                case OutputPresentationType.HTML:
                default:
                    RenderHtml rh;
                    ip = rh = new RenderHtml(this, sg);
                    rh.Asp = (type == OutputPresentationType.ASPHTML);
                    rh.Prefix = prefix;
                    ReportDefinition.Run(ip);
                    // Retain the CSS and JavaScript
                    if (rh != null)
                    {
                        CSS = rh.CSS;
                        JavaScript = rh.JavaScript;
                    }
                    break;
            }

            sg.CloseMainStream();
            Cache = new RCache();
            return;
        }

        private void RunRenderMht(IStreamGen sg)
        {
            OneFileStreamGen temp = null;
            FileStream fs = null;
            try
            {
                string tempHtmlReportFileName = Path.ChangeExtension(Path.GetTempFileName(), "htm");
                temp = new OneFileStreamGen(tempHtmlReportFileName, true);
                RunRender(temp, OutputPresentationType.HTML);
                temp.CloseMainStream();

                // Create the mht file (into a temporary file position)
                MhtBuilder mhtConverter = new MhtBuilder();
                string fileName = Path.ChangeExtension(Path.GetTempFileName(), "mht");
                mhtConverter.SavePageArchive(fileName, "file://" + tempHtmlReportFileName);

                // clean up the temporary files
                foreach (string tempFileName in temp.FileList)
                {
                    try
                    {
                        File.Delete(tempFileName);
                    }
                    catch { }
                }

                // Copy the mht file to the requested stream
                Stream os = sg.GetStream();
                fs = File.OpenRead(fileName);
                byte[] ba = new byte[4096];
                int rb = 0;
                while ((rb = fs.Read(ba, 0, ba.Length)) > 0)
                {
                    os.Write(ba, 0, rb);
                }

            }
            catch (Exception ex)
            {
                rl.LogError(8, "Error converting HTML to MHTML " + ex.Message +
                                    Environment.NewLine + ex.StackTrace);
            }
            finally
            {
                if (temp != null)
                    temp.CloseMainStream();
                if (fs != null)
                    fs.Close();
                Cache = new RCache();
            }
        }

        /// <summary>
        /// RunRenderPdf will render a Pdf given the page structure
        /// </summary>
        /// <param name="sg"></param>
        /// <param name="pgs"></param>
        public void RunRenderPdf(IStreamGen sg, Pages pgs)
        {
            throw new NotImplementedException();
            //PageNumber = 1;     // reset page numbers
            //TotalPages = 1;

            //IPresent ip = new RenderPdf(this, sg);
            //try
            //{
            //    ip.Start();
            //    ip.RunPages(pgs);
            //    ip.End();
            //}
            //finally
            //{
            //    pgs.CleanUp();		// always want to make sure we cleanup to reduce resource usage
            //    _Cache = new RCache();
            //}

            //return;
        }

        /// <summary>
        /// RunRenderTif will render a TIF given the page structure
        /// </summary>
        /// <param name="sg"></param>
        /// <param name="pgs"></param>
        public void RunRenderTif(IStreamGen sg, Pages pgs, bool bColor)
        {
            throw new NotImplementedException();
            //PageNumber = 1;		// reset page numbers
            //TotalPages = 1;

            //RenderTif ip = new RenderTif(this, sg);
            //ip.RenderColor = bColor;
            //try
            //{
            //    ip.Start();
            //    ip.RunPages(pgs);
            //    ip.End();
            //}
            //finally
            //{
            //    pgs.CleanUp();		// always want to make sure we cleanup to reduce resource usage
            //    _Cache = new RCache();
            //}

            //return;
        }

        private void RunRenderXmlTransform(IStreamGen sg, MemoryStreamGen msg)
        {
            try
            {
                string file;
                if (ReportDefinition.DataTransform[0] != Path.DirectorySeparatorChar)
                    file = this.Folder + Path.DirectorySeparatorChar + ReportDefinition.DataTransform;
                else
                    file = this.Folder + ReportDefinition.DataTransform;
                XmlUtil.XslTrans(file, msg.GetText(), sg.GetStream());
            }
            catch (Exception ex)
            {
                rl.LogError(8, "Error processing DataTransform " + ex.Message + "\r\n" + ex.StackTrace);
            }
            finally
            {
                msg.Dispose();
            }
            return;
        }

        /// <summary>
        /// Build the Pages for this report.
        /// </summary>
        /// <returns></returns>
        public Pages BuildPages()
        {
            PageNumber = 1;     // reset page numbers
            TotalPages = 1;

            Pages pgs = new Pages(this);
            pgs.PageHeight = ReportDefinition.PageHeight.Points;
            pgs.PageWidth = ReportDefinition.PageWidth.Points;
            try
            {
                Page p = new Page(1);               // kick it off with a new page
                pgs.AddPage(p);

                // Create all the pages
                ReportDefinition.Body.RunPage(pgs);

                if (pgs.LastPage.IsEmpty() && pgs.PageCount > 1) // get rid of extraneous pages which
                    pgs.RemoveLastPage();           //   can be caused by region page break at end

                // Now create the headers and footers for all the pages (as needed)
                if (ReportDefinition.PageHeader != null)
                    ReportDefinition.PageHeader.RunPage(pgs);
                if (ReportDefinition.PageFooter != null)
                    ReportDefinition.PageFooter.RunPage(pgs);
                // clear out any runtime clutter
                foreach (Page pg in pgs)
                    pg.ResetPageExpressions();

                pgs.SortPageItems();             // Handle ZIndex ordering of pages
            }
            catch (Exception e)
            {
                rl.LogError(8, "Exception running report\r\n" + e.Message + "\r\n" + e.StackTrace);
            }
            finally
            {
                pgs.CleanUp();		// always want to make sure we clean this up since 
                Cache = new RCache();
            }

            return pgs;
        }

        internal void SetReportDefinition(ReportDefn r)
        {
            ReportDefinition = r;
            _UserParameters = null;     // force recalculation of user parameters
            _DataSets = null;           // force reload of datasets
        }

        internal string CreateRuntimeName(object ro)
        {
            _RuntimeName++;                 // increment the name generator
            string name = "o" + _RuntimeName.ToString();
            _LURuntimeName.Add(name, ro);
            return name;
        }

        /// <summary>
        /// Clear all errors generated up to now.
        /// </summary>
        public void ErrorReset()
        {
            if (rl == null)
                return;
            rl.Reset();
        }

    }

    internal class RCache
    {
        Hashtable _RunCache;

        internal RCache()
        {
            _RunCache = new Hashtable();
        }

        internal void Add(ReportLink rl, string name, object o)
        {
            _RunCache.Add(GetKey(rl, name), o);
        }

        internal void AddReplace(ReportLink rl, string name, object o)
        {
            string key = GetKey(rl, name);
            _RunCache.Remove(key);
            _RunCache.Add(key, o);
        }

        internal object Get(ReportLink rl, string name)
        {
            return _RunCache[GetKey(rl, name)];
        }

        internal void Remove(ReportLink rl, string name)
        {
            _RunCache.Remove(GetKey(rl, name));
        }

        internal void Add(ReportDefn rd, string name, object o)
        {
            _RunCache.Add(GetKey(rd, name), o);
        }

        internal void AddReplace(ReportDefn rd, string name, object o)
        {
            string key = GetKey(rd, name);
            _RunCache.Remove(key);
            _RunCache.Add(key, o);
        }

        internal object Get(ReportDefn rd, string name)
        {
            return _RunCache[GetKey(rd, name)];
        }

        internal void Remove(ReportDefn rd, string name)
        {
            _RunCache.Remove(GetKey(rd, name));
        }

        internal void Add(string key, object o)
        {
            _RunCache.Add(key, o);
        }

        internal void AddReplace(string key, object o)
        {
            _RunCache.Remove(key);
            _RunCache.Add(key, o);
        }

        internal object Get(string key)
        {
            return _RunCache[key];
        }

        internal void Remove(string key)
        {
            _RunCache.Remove(key);
        }

        internal object Get(int i, string name)
        {
            return _RunCache[GetKey(i, name)];
        }

        internal void Remove(int i, string name)
        {
            _RunCache.Remove(GetKey(i, name));
        }

        string GetKey(ReportLink rl, string name)
        {
            return GetKey(rl.ObjectNumber, name);
        }

        string GetKey(ReportDefn rd, string name)
        {
            if (rd.Subreport == null)	// top level report use 0 
            {
                return GetKey(0, name);
            }
            else						// Use the subreports object number
            {
                return GetKey(rd.Subreport.ObjectNumber, name);
            }
        }

        string GetKey(int onum, string name)
        {
            return name + onum.ToString();
        }
    }

    // holder objects for value types
    internal class ODateTime
    {
        internal DateTime dt;

        internal ODateTime(DateTime adt)
        {
            dt = adt;
        }
    }

    internal class ODecimal
    {
        internal decimal d;

        internal ODecimal(decimal ad)
        {
            d = ad;
        }
    }

    internal class ODouble
    {
        internal double d;

        internal ODouble(double ad)
        {
            d = ad;
        }
    }

    internal class OFloat
    {
        internal float f;

        internal OFloat(float af)
        {
            f = af;
        }
    }

    internal class OInt
    {
        internal int i;

        internal OInt(int ai)
        {
            i = ai;
        }
    }

    public class SubreportDataRetrievalEventArgs : EventArgs
    {
        public readonly Report Report;

        public SubreportDataRetrievalEventArgs(Report r)
        {
            Report = r;
        }
    }
}
