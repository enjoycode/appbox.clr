using System;
using System.Xml;
using System.Collections.Generic;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// DataRegion base class definition and processing.
    /// Warning if you inherit from DataRegion look at Expression.cs first.
    ///</summary>
    [Serializable]
    internal class DataRegion : ReportItem
    {
        /// <summary>
        /// Indicates the entire data region (all repeated sections) should be kept
        /// together on one page if possible.
        /// </summary>
        internal bool KeepTogether { get; set; }

        /// <summary>
        /// (string) Message to display in the DataRegion
        /// (instead of the region layout) when no rows of data are available.
        /// Note: Style information on the data region applies to this text
        /// </summary>
        internal Expression NoRows { get; set; }

        /// <summary>
        /// Indicates which data set to use for this data region.
        /// Mandatory for top level DataRegions
        /// (not contained within another DataRegion) if there is not exactly
        /// one data set in the report. If there is exactly one data set in the report, the
        /// data region uses that data set. (Note: If there are zero data sets in the
        /// report, data regions can not be used, as there is no valid DataSetName to
        /// use) Ignored for DataRegions that are not top level.
        /// </summary>
        internal string DataSetName { get; set; }

        /// <summary>
        /// resolved data set name
        /// </summary>
        internal DataSetDefn DataSetDefn { get; set; }

        /// <summary>
        /// Indicates the report should page break at the start of the data region.
        /// </summary>
        internal bool PageBreakAtStart { get; set; }

        /// <summary>
        /// Indicates the report should page break at the end of the data region.
        /// </summary>
        internal bool PageBreakAtEnd { get; set; }

        /// <summary>
        /// Filters to apply to each row of data in the data region.
        /// </summary>
        internal Filters Filters { get; set; }

        DataRegion _ParentDataRegion;   // when DataRegions are nested; the nested regions have the parent set 

        internal DataRegion(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p, xNode)
        {
            KeepTogether = false;
            NoRows = null;
            DataSetName = null;
            DataSetDefn = null;
            PageBreakAtStart = false;
            PageBreakAtEnd = false;
            Filters = null;
        }

        internal bool DataRegionElement(XmlNode xNodeLoop)
        {
            switch (xNodeLoop.Name)
            {
                case "KeepTogether":
                    KeepTogether = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
                    break;
                case "NoRows":
                    NoRows = new Expression(OwnerReport, this, xNodeLoop, ExpressionType.String);
                    break;
                case "DataSetName":
                    DataSetName = xNodeLoop.InnerText;
                    break;
                case "PageBreakAtStart":
                    PageBreakAtStart = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
                    break;
                case "PageBreakAtEnd":
                    PageBreakAtEnd = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
                    break;
                case "Filters":
                    Filters = new Filters(OwnerReport, this, xNodeLoop);
                    break;
                default:    // Will get many that are handled by the specific
                            //  type of data region: ie  list,chart,matrix,table
                    if (ReportItemElement(xNodeLoop))   // try at ReportItem level
                        break;
                    return false;
            }
            return true;
        }

        // Handle parsing of function in final pass
        override internal void FinalPass()
        {
            base.FinalPass();

            if (this is Table)
            {   // Grids don't have any data responsibilities
                Table t = this as Table;
                if (t.IsGrid)
                    return;
            }

            // DataRegions aren't allowed in PageHeader or PageFooter; 
            if (this.InPageHeaderOrFooter())
                OwnerReport.rl.LogError(8, String.Format("The DataRegion '{0}' is not allowed in a PageHeader or PageFooter", this.Name == null ? "unknown" : Name.Nm));

            ResolveNestedDataRegions();

            if (_ParentDataRegion != null)      // when nested we use the dataset of the parent
            {
                DataSetDefn = _ParentDataRegion.DataSetDefn;
            }
            else if (DataSetName != null)
            {
                if (OwnerReport.DataSetsDefn != null)
                    DataSetDefn = (DataSetDefn)OwnerReport.DataSetsDefn.Items[DataSetName];
                if (DataSetDefn == null)
                {
                    OwnerReport.rl.LogError(8, String.Format("DataSetName '{0}' not specified in DataSets list.", DataSetName));
                }
            }
            else
            {       // No name but maybe we can default to a single Dataset
                if (DataSetDefn == null && OwnerReport.DataSetsDefn != null &&
                    OwnerReport.DataSetsDefn.Items.Count == 1)
                {
                    foreach (DataSetDefn d in OwnerReport.DataSetsDefn.Items.Values)
                    {
                        DataSetDefn = d;
                        break;  // since there is only 1 this will obtain it
                    }
                }
                if (DataSetDefn == null)
                    OwnerReport.rl.LogError(8, string.Format("{0} must specify a DataSetName.", this.Name == null ? "DataRegions" : this.Name.Nm));
            }

            if (NoRows != null)
                NoRows.FinalPass();
            if (Filters != null)
                Filters.FinalPass();

            return;
        }

        void ResolveNestedDataRegions()
        {
            ReportLink rl = Parent;
            while (rl != null)
            {
                if (rl is DataRegion)
                {
                    _ParentDataRegion = rl as DataRegion;
                    break;
                }
                rl = rl.Parent;
            }
            return;
        }

        override internal void Run(IPresent ip, Row row)
        {
            base.Run(ip, row);
        }

        internal void RunPageRegionBegin(Pages pgs)
        {
            if (TC == null && PageBreakAtStart && !pgs.CurrentPage.IsEmpty())
            {   // force page break at beginning of dataregion
                pgs.NextOrNew();
                pgs.CurrentPage.YOffset = OwnerReport.TopOfPage;
            }
        }

        internal void RunPageRegionEnd(Pages pgs)
        {
            if (TC == null && PageBreakAtEnd && !pgs.CurrentPage.IsEmpty())
            {   // force page break at beginning of dataregion
                pgs.NextOrNew();
                pgs.CurrentPage.YOffset = OwnerReport.TopOfPage;
            }
        }

        internal bool AnyRows(IPresent ip, Rows data)
        {
            if (data == null || data.Data == null || data.Data.Count <= 0)
            {
                string msg;
                if (NoRows != null)
                    msg = NoRows.EvaluateString(ip.Report(), null);
                else
                    msg = null;
                ip.DataRegionNoRows(this, msg);
                return false;
            }

            return true;
        }

        internal bool AnyRowsPage(Pages pgs, Rows data)
        {
            if (data != null && data.Data != null &&
                data.Data.Count > 0)
                return true;

            string msg;
            if (NoRows != null)
                msg = this.NoRows.EvaluateString(pgs.Report, null);
            else
                msg = null;

            if (msg == null)
                return false;

            // OK we have a message we need to put out
            RunPageRegionBegin(pgs);                // still perform page break if needed

            PageText pt = new PageText(msg);
            SetPagePositionAndStyle(pgs.Report, pt, null);

            if (pt.SI.BackgroundImage != null)
                pt.SI.BackgroundImage.H = pt.H;     //   and in the background image

            pgs.CurrentPage.AddObject(pt);

            RunPageRegionEnd(pgs);					// perform end page break if needed

            SetPagePositionEnd(pgs, pt.Y + pt.H);

            return false;
        }

        internal Rows GetFilteredData(Report rpt, Row row)
        {
            try
            {
                Rows data;
                if (Filters == null)
                {
                    if (_ParentDataRegion == null)
                    {
                        data = DataSetDefn.Query.GetMyData(rpt);
                        return data == null ? null : new Rows(rpt, data);   // We need to copy in case DataSet is shared by multiple DataRegions
                    }
                    else
                        return GetNestedData(rpt, row);
                }

                if (_ParentDataRegion == null)
                {
                    data = DataSetDefn.Query.GetMyData(rpt);
                    if (data != null)
                        data = new Rows(rpt, data);
                }
                else
                    data = GetNestedData(rpt, row);

                if (data == null)
                    return null;

                List<Row> ar = new List<Row>();
                foreach (Row r in data.Data)
                {
                    if (Filters.Apply(rpt, r))
                        ar.Add(r);
                }
                ar.TrimExcess();
                data.Data = ar;
                Filters.ApplyFinalFilters(rpt, data, true);

                // Adjust the rowcount
                int rCount = 0;
                foreach (Row r in ar)
                {
                    r.RowNumber = rCount++;
                }
                return data;
            }
            catch (Exception e)
            {
                OwnerReport.rl.LogError(8, e.Message);
                return null;
            }
        }

        Rows GetNestedData(Report rpt, Row row)
        {
            if (row == null)
                return null;

            ReportLink rl = this.Parent;
            while (rl != null)
            {
                if (rl is TableGroup || rl is List || rl is MatrixCell)
                    break;
                rl = rl.Parent;
            }
            if (rl == null)
                return null;            // should have been caught as an error

            Grouping g = null;
            if (rl is TableGroup)
            {
                TableGroup tg = rl as TableGroup;
                g = tg.Grouping;
            }
            else if (rl is List)
            {
                List l = rl as List;
                g = l.Grouping;
            }
            else if (rl is MatrixCell)
            {
                MatrixCellEntry mce = this.GetMC(rpt);
                return new Rows(rpt, mce.Data);
            }
            if (g == null)
                return null;

            GroupEntry ge = row.R.CurrentGroups[g.GetIndex(rpt)];

            return new Rows(rpt, row.R, ge.StartRow, ge.EndRow, null);
        }

        internal void DataRegionFinish()
        {
            // All dataregion names need to be saved!
            if (this.Name != null)
            {
                try
                {
                    OwnerReport.LUAggrScope.Add(this.Name.Nm, this);        // add to referenceable regions
                }
                catch // wish duplicate had its own exception
                {
                    OwnerReport.rl.LogError(8, "Duplicate name '" + this.Name.Nm + "'.");
                }
            }
            return;
        }

    }
}
