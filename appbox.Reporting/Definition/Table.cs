using System;
using System.Xml;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using appbox.Reporting.Resources;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// Table definition and processing.  Inherits from DataRegion which inherits from ReportItem.
    ///</summary>
    [Serializable]
    internal class Table : DataRegion
    {
        internal bool IsGrid { get; }

        /// <summary>
        /// The columns in the table
        /// </summary>
        internal TableColumns TableColumns { get; set; }

        internal int ColumnCount => TableColumns.Items.Count;

        /// <summary>
        /// The table header rows
        /// </summary>
        internal Header Header { get; set; }

        /// <summary>
        /// The groups (group expressions/headers/footers) for the table
        /// </summary>
        internal TableGroups TableGroups { get; set; }

        /// <summary>
        /// The details rows for the table
        ///	The table must have at least one of: Details, Header or Footer
        /// </summary>
        internal Details Details { get; set; }

        internal int DetailsCount => Details == null ? 0 : Details.TableRows.Items.Count;

        /// <summary>
        /// The table footer rows
        /// </summary>
        internal Footer Footer { get; set; }

        /// <summary>
        /// Indicates the table should expand to
        ///	fill the page, pushing items below it to the bottom of the page.
        /// </summary>
        internal bool FillPage { get; set; }

        /// <summary>
        /// The name to use for the data element for instances of this group. Ignored if
        /// there is a grouping defined for the details. Default: "Details"
        /// </summary>
        internal string DetailDataElementName { get; set; }

        /// <summary>
        /// The name to use for the data element for the collection of all instances of this group.
        /// Default: "Details_Collection"
        /// </summary>
        internal string DetailDataCollectionName { get; set; }

        /// <summary>
        /// Indicates whether the details should appear in a data rendering.
        /// </summary>
        internal DataElementOutputEnum DetailDataElementOutput { get; set; }

        internal Table(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p, xNode)
        {
            TableColumns = null;
            Header = null;
            TableGroups = null;
            Details = null;
            Footer = null;
            FillPage = true;
            DetailDataElementName = "Details";
            DetailDataCollectionName = "Details_Collection";
            DetailDataElementOutput = DataElementOutputEnum.Output;
            IsGrid = xNode.Name != "Table";                 // a grid is a restricted table to no data behind it

            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                switch (xNodeLoop.Name)
                {
                    case "TableColumns":
                        TableColumns = new TableColumns(r, this, xNodeLoop);
                        break;
                    case "Header":
                        Header = new Header(r, this, xNodeLoop);
                        break;
                    case "TableGroups":
                        TableGroups = new TableGroups(r, this, xNodeLoop);
                        break;
                    case "Details":
                        Details = new Details(r, this, xNodeLoop);
                        break;
                    case "Footer":
                        Footer = new Footer(r, this, xNodeLoop);
                        break;
                    case "FillPage":
                        FillPage = XmlUtil.Boolean(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    case "DetailDataElementName":
                        DetailDataElementName = xNodeLoop.InnerText;
                        break;
                    case "DetailDataCollectionName":
                        DetailDataCollectionName = xNodeLoop.InnerText;
                        break;
                    case "DetailDataElementOutput":
                        DetailDataElementOutput = RDL.DataElementOutput.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
                        break;
                    default:
                        if (DataRegionElement(xNodeLoop))   // try at DataRegion level
                            break;
                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, "Unknown " + xNode.Name + " element '" + xNodeLoop.Name + "' ignored.");
                        break;
                }
            }
            DataRegionFinish();         // Tidy up the DataRegion
            if (TableColumns == null)
            {
                OwnerReport.rl.LogError(8, "TableColumns element must be specified for a " + xNode.Name + ".");
                return;
            }

            // Verify Grid restrictions
            if (IsGrid)
            {
                if (TableGroups != null)
                    OwnerReport.rl.LogError(8, "TableGroups not allowed in Grid element '" + xNode.Name + "'.");
            }

            if (OwnerReport.rl.MaxSeverity < 8)
                VerifyCC();         // Verify column count
        }

        private void VerifyCC()
        {
            int colCount = TableColumns.Items.Count;
            if (Header != null)
                VerifyCCTableRows("Table Header", Header.TableRows, colCount);

            if (Footer != null)
                VerifyCCTableRows("Table Footer", Footer.TableRows, colCount);

            if (Details != null)
                VerifyCCTableRows("Table Details", Details.TableRows, colCount);

            if (TableGroups != null)
            {
                foreach (TableGroup tg in TableGroups.Items)
                {
                    if (tg.Header != null)
                        VerifyCCTableRows("TableGroup Header", tg.Header.TableRows, colCount);

                    if (tg.Footer != null)
                        VerifyCCTableRows("TableGroup Footer", tg.Footer.TableRows, colCount);
                }
            }
        }

        private void VerifyCCTableRows(string label, TableRows trs, int colCount)
        {
            foreach (TableRow tr in trs.Items)
            {
                int cols = 0;
                foreach (TableCell tc in tr.TableCells.Items)
                {
                    cols += tc.ColSpan;
                }
                if (cols != colCount)
                    OwnerReport.rl.LogError(8, String.Format("{0} must have the same number of columns as TableColumns.", label));
            }
            return;
        }

        override internal void FinalPass()
        {
            base.FinalPass();
            float totalHeight = 0;

            if (TableColumns != null)
                TableColumns.FinalPass();
            if (Header != null)
            {
                Header.FinalPass();
                totalHeight += Header.TableRows.DefnHeight();
            }
            if (TableGroups != null)
            {
                TableGroups.FinalPass();
                totalHeight += TableGroups.DefnHeight();
            }
            if (Details != null)
            {
                Details.FinalPass();
                totalHeight += Details.TableRows.DefnHeight();
            }
            if (Footer != null)
            {
                Footer.FinalPass();
                totalHeight += Footer.TableRows.DefnHeight();
            }

            if (Height == null)
            {   // Calculate a height based on the sum of the TableRows
                Height = new RSize(OwnerReport,
                    string.Format(NumberFormatInfo.InvariantInfo, "{0:0.00}pt", totalHeight));
            }
            return;
        }

        override internal void Run(IPresent ip, Row row)
        {
            Report r = ip.Report();
            TableWorkClass wc = GetValue(r);

            if (IsGrid)
                wc.Data = Rows.CreateOneRow(r);
            else
                wc.Data = GetFilteredData(r, row);

            if (!AnyRows(ip, wc.Data))      // if no rows return
                return;                 //   nothing left to do

            RunPrep(r, row, wc);

            if (!ip.TableStart(this, row))
                return;                     // render doesn't want to continue

            if (TableColumns != null)
                TableColumns.Run(ip, row);

            // Header
            if (Header != null)
            {
                ip.TableHeaderStart(Header, row);
                Row frow = wc.Data.Data.Count > 0 ? wc.Data.Data[0] : null;
                Header.Run(ip, frow);
                ip.TableHeaderEnd(Header, row);
            }

            // Body
            ip.TableBodyStart(this, row);
            if (wc.RecursiveGroup != null)
                RunRecursiveGroups(ip, wc);
            else
                RunGroups(ip, wc.Groups, wc);
            ip.TableBodyEnd(this, row);

            // Footer
            if (Footer != null)
            {
                ip.TableFooterStart(Footer, row);
                Row lrow = wc.Data.Data.Count > 0 ? wc.Data.Data[wc.Data.Data.Count - 1] : null;
                Footer.Run(ip, lrow);
                ip.TableFooterEnd(Footer, row);
            }

            ip.TableEnd(this, row);
            RemoveValue(r);
        }

        override internal void RunPage(Pages pgs, Row row)
        {
            Report r = pgs.Report;
            if (IsHidden(r, row))
                return;

            TableWorkClass wc = GetValue(r);
            if (IsGrid)
                wc.Data = Rows.CreateOneRow(r);
            else
                wc.Data = GetFilteredData(r, row);

            SetPagePositionBegin(pgs);

            if (!AnyRowsPage(pgs, wc.Data))     // if no rows return
                return;                     //   nothing left to do

            RunPrep(r, row, wc);

            RunPageRegionBegin(pgs);

            Page p = pgs.CurrentPage;
            p.YOffset += RelativeY(r);

            // Calculate the xpositions of the columns
            TableColumns.CalculateXPositions(r, GetOffsetCalc(r) + LeftCalc(r), row);

            RunPageHeader(pgs, wc.Data.Data[0], true, null);

            if (wc.RecursiveGroup != null)
            {
                RunRecursiveGroupsPage(pgs, wc);
            }
            else
            {
                float groupHeight = 0;
                if (wc.Data.Data.Count > 0 && Footer != null)
                    groupHeight = Footer.HeightOfRows(pgs, wc.Data.Data[0]);
                RunGroupsPage(pgs, wc, wc.Groups, wc.Data.Data.Count - 1, groupHeight);
            }

            // Footer
            if (row == null)
            {
                // Row == null means the inital/last page
                Row lRow = wc.Data.Data.Count > 0 ? wc.Data.Data[wc.Data.Data.Count - 1] : null;
                RunPageFooter(pgs, lRow, true);
            }

            // dst, use the code above
            //if (_Footer != null)
            //{
            //    Row lrow = wc.Data.Data.Count > 0?  wc.Data.Data[wc.Data.Data.Count-1]: null;
            //    p = pgs.CurrentPage;
            //    // make sure the footer fits on the page
            //    if (p.YOffset + _Footer.HeightOfRows(pgs, lrow) > pgs.BottomOfPage)
            //    {
            //        p = RunPageNew(pgs, p);
            //        RunPageHeader(pgs, row, false, null);
            //    }
            //    _Footer.RunPage(pgs, lrow);
            //}

            RunPageRegionEnd(pgs);

            SetPagePositionEnd(pgs, pgs.CurrentPage.YOffset);
            RemoveValue(r);
        }

        internal void RunPageFooter(Pages pgs, Row row, bool bLast)
        {
            if (Footer != null && (Footer.RepeatOnNewPage || bLast))
            {
                Page p = pgs.CurrentPage;
                if (p.YOffset + Footer.HeightOfRows(pgs, row) > pgs.BottomOfPage)
                {
                    p = RunPageNew(pgs, p);
                    RunPageHeader(pgs, row, false, null);
                }
                Footer.RunPage(pgs, row);
            }
        }

        internal float GetPageFooterHeight(Pages pgs, Row row)
        {
            // Calculate height of footer on every page
            float FooterHeight = 0;

            if (Footer != null && Footer.RepeatOnNewPage)
                FooterHeight += Footer.HeightOfRows(pgs, row);

            // Need add calculation for group footer which show on every page
            return FooterHeight;
        }

        internal void RunPageHeader(Pages pgs, Row frow, bool bFirst, TableGroup stoptg)
        {
            // Do the table headers
            bool isEmpty = pgs.CurrentPage.IsEmpty();

            if (Header != null && (Header.RepeatOnNewPage || bFirst))
            {
                Header.RunPage(pgs, frow);
                if (isEmpty)
                    pgs.CurrentPage.SetEmpty();     // Consider this empty of data
            }

            if (bFirst)                         // the very first time we'll output the header (and not consider page empty)
                return;

            if (TableGroups == null)
            {
                pgs.CurrentPage.SetEmpty();     // Consider this empty of data
                return;
            }

            // Do the group headers
            foreach (TableGroup tg in TableGroups.Items)
            {
                if (stoptg != null && tg == stoptg)
                    break;
                if (tg.Header != null)
                {
                    if (tg.Header.RepeatOnNewPage)
                    {
                        tg.Header.RunPage(pgs, frow);
                    }
                }
            }

            pgs.CurrentPage.SetEmpty();     // Consider this empty of data

            return;
        }

        void RunPrep(Report rpt, Row row, TableWorkClass wc)
        {
            GroupEntry[] currentGroups;

            // We have some data
            if (TableGroups != null ||
                (Details != null &&
                 (Details.Sorting != null ||
                 Details.Grouping != null)))       // fix up the data
            {
                List<Row> saveData = wc.Data.Data;
                Grouping gr;
                Sorting srt;
                if (Details == null)
                {
                    gr = null; srt = null;
                }
                else
                {
                    gr = Details.Grouping;
                    srt = Details.Sorting;
                }

                wc.Data = new Rows(rpt, TableGroups, gr, srt);
                wc.Data.Data = saveData;
                wc.Data.Sort();
                PrepGroups(rpt, wc);
            }

            // If we haven't formed any groups then form one with all rows
            if (wc.Groups == null)
            {
                wc.Groups = new List<GroupEntry>();
                GroupEntry ge = new GroupEntry(null, null, 0);
                if (wc.Data.Data != null)       // Do we have any data?
                    ge.EndRow = wc.Data.Data.Count - 1; // yes
                else
                    ge.EndRow = -1;                 // no
                wc.Groups.Add(ge);          // top group
                currentGroups = new GroupEntry[1];
            }
            else if (TableGroups != null)
            {
                int count = TableGroups.Items.Count;
                if (Details != null && Details.Grouping != null)
                    count++;

                currentGroups = new GroupEntry[count];
            }
            else
                currentGroups = new GroupEntry[1];

            wc.Data.CurrentGroups = currentGroups;

            SortGroups(rpt, wc.Groups, wc);
        }

        private void SortGroups(Report rpt, List<GroupEntry> groupEntries, TableWorkClass wc)
        {
            GroupEntry fge = (GroupEntry)(groupEntries[0]);
            if (fge.Sort != null)
            {
                GroupEntryCompare gec = new GroupEntryCompare(rpt, wc);
                RunGroupsSetGroups(rpt, wc, groupEntries);
                groupEntries.Sort(gec);
            }

            // drill down into nested groups
            foreach (GroupEntry ge in groupEntries)
            {
                if (ge.NestedGroup.Count > 0)
                    SortGroups(rpt, ge.NestedGroup, wc);
            }
        }

        private void PrepGroups(Report rpt, TableWorkClass wc)
        {
            wc.RecursiveGroup = null;
            if (TableGroups == null)
            {   // no tablegroups; check to ensure details is grouped
                if (Details == null || Details.Grouping == null)
                    return;         // no groups to prepare
            }

            int i = 0;
            // 1) Build array of all GroupExpression objects
            List<GroupExpression> gea = new List<GroupExpression>();
            //    count the number of groups
            int countG = 0;
            if (TableGroups != null)
                countG = TableGroups.Items.Count;

            Grouping dg = null;
            Sorting ds = null;
            if (Details != null && Details.Grouping != null)
            {
                dg = Details.Grouping;
                ds = Details.Sorting;
                countG++;
            }
            GroupEntry[] currentGroups = new GroupEntry[countG++];
            if (TableGroups != null)
            {   // add in the groups for the tablegroup
                foreach (TableGroup tg in TableGroups.Items)
                {
                    if (tg.Grouping.ParentGroup != null)
                        wc.RecursiveGroup = tg.Grouping;
                    tg.Grouping.SetIndex(rpt, i);       // set the index of this group (so we can find the GroupEntry)
                    currentGroups[i++] = new GroupEntry(tg.Grouping, tg.Sorting, 0);
                    foreach (GroupExpression ge in tg.Grouping.GroupExpressions.Items)
                    {
                        gea.Add(ge);
                    }
                }
            }
            if (dg != null)
            {   // add in the groups for the details grouping
                if (dg.ParentGroup != null)
                    wc.RecursiveGroup = dg;
                dg.SetIndex(rpt, i);        // set the index of this group (so we can find the GroupEntry)
                currentGroups[i++] = new GroupEntry(dg, ds, 0);
                foreach (GroupExpression ge in dg.GroupExpressions.Items)
                {
                    gea.Add(ge);
                }
            }

            if (wc.RecursiveGroup != null)
            {
                if (gea.Count != 1)     // Limitiation of implementation
                    throw new Exception(Strings.Table_Error_RecursiveGroupsMustOnlyGroupDefinition);

                PrepRecursiveGroup(rpt, wc);    // only one group and it's recursive: optimization 
                return;
            }

            // Save the typecodes, and grouping by groupexpression; for later use
            TypeCode[] tcs = new TypeCode[gea.Count];
            Grouping[] grp = new Grouping[gea.Count];
            i = 0;
            foreach (GroupExpression ge in gea)
            {
                grp[i] = (Grouping)(ge.Parent.Parent);          // remember the group
                tcs[i++] = ge.Expression.GetTypeCode(); // remember type of expression
            }

            // 2) Loop thru the data, then loop thru the GroupExpression list
            wc.Groups = new List<GroupEntry>();
            object[] savValues = null;
            object[] grpValues = null;
            int rowCurrent = 0;

            foreach (Row row in wc.Data.Data)
            {
                // Get the values for all the group expressions
                if (grpValues == null)
                    grpValues = new object[gea.Count];

                i = 0;
                foreach (GroupExpression ge in gea)
                {
                    if (((Grouping)(ge.Parent.Parent)).ParentGroup == null)
                        grpValues[i++] = ge.Expression.Evaluate(rpt, row);
                    else
                        grpValues[i++] = null;  // Want all the parentGroup to evaluate equal
                }

                // For first row we just primed the pump; action starts on next row
                if (rowCurrent == 0)            // always start new group on first row
                {
                    rowCurrent++;
                    savValues = grpValues;
                    grpValues = null;
                    continue;
                }

                // compare the values; if change then we have a group break
                for (i = 0; i < savValues.Length; i++)
                {
                    if (Filter.ApplyCompare(tcs[i], savValues[i], grpValues[i]) != 0)
                    {
                        // start a new group; and force a break on every subgroup
                        GroupEntry saveGe = null;
                        for (int j = grp[i].GetIndex(rpt); j < currentGroups.Length; j++)
                        {
                            currentGroups[j].EndRow = rowCurrent - 1;
                            if (j == 0)
                                wc.Groups.Add(currentGroups[j]);        // top group
                            else if (saveGe == null)
                                currentGroups[j - 1].NestedGroup.Add(currentGroups[j]);
                            else
                                saveGe.NestedGroup.Add(currentGroups[j]);

                            saveGe = currentGroups[j];  // retain this GroupEntry
                            currentGroups[j] = new GroupEntry(currentGroups[j].Group, currentGroups[j].Sort, rowCurrent);
                        }
                        savValues = grpValues;
                        grpValues = null;
                        break;      // break out of the value comparison loop
                    }
                }
                rowCurrent++;
            }

            // End of all rows force break on end of rows
            for (i = 0; i < currentGroups.Length; i++)
            {
                currentGroups[i].EndRow = rowCurrent - 1;
                if (i == 0)
                    wc.Groups.Add(currentGroups[i]);        // top group
                else
                    currentGroups[i - 1].NestedGroup.Add(currentGroups[i]);
            }

            return;
        }

        private void PrepRecursiveGroup(Report rpt, TableWorkClass wc)
        {
            // Prepare for processing recursive group
            Grouping g = wc.RecursiveGroup;
            IExpr parentExpr = g.ParentGroup;
            GroupExpression gexpr = g.GroupExpressions.Items[0] as GroupExpression;
            IExpr groupExpr = gexpr.Expression;
            TypeCode tc = groupExpr.GetTypeCode();
            List<Row> odata = new List<Row>(wc.Data.Data);			// this is the old data that we'll recreate using the recursive hierarchy
            List<Row> newrows = new List<Row>(odata.Count);

            // For now we assume on a single top of tree (and it must sort first as null)
            //   spec is incomplete: should have ability to specify starting value of tree
            // TODO: pull all of the rows that start with null
            newrows.Add(odata[0]);                  // add the starting row
            odata.RemoveAt(0);                      //   remove olddata

            // we need to build the group entry stack
            // Build the initial one
            wc.Groups = new List<GroupEntry>();
            GroupEntry ge = new GroupEntry(null, null, 0);
            ge.EndRow = odata.Count - 1;
            wc.Groups.Add(ge);				// top group

            List<GroupEntry> ges = new List<GroupEntry>();
            ges.Add(ge);

            // loop thru the rows and find their children
            //   we place the children right after the parent
            //   this reorders the rows in the form of the hierarchy
            Row r;
            RecursiveCompare rc = new RecursiveCompare(rpt, parentExpr, tc, groupExpr);
            for (int iRow = 0; iRow < newrows.Count; iRow++)    // go thru the temp rows
            {
                r = newrows[iRow];

                r.GroupEntry = ge = new GroupEntry(g, null, iRow);  // TODO: sort for this group??
                r.GroupEntry.EndRow = iRow;

                // pull out all the rows that match this value
                int iMainRow = odata.BinarySearch(r, rc);
                if (iMainRow < 0)
                {
                    for (int i = 0; i <= r.Level + 1 && i < ges.Count; i++)
                    {
                        ge = ges[i] as GroupEntry;
                        Row rr = newrows[ge.StartRow];  // start row has the base level of group	
                        if (rr.Level < r.Level)
                            ge.EndRow = iRow;
                    }
                    continue;
                }

                // look backward for starting row; 
                //   in case of duplicates, BinarySearch can land on any of the rows
                object cmpvalue = groupExpr.Evaluate(rpt, r);

                int sRow = iMainRow - 1;
                while (sRow >= 0)
                {
                    object v = parentExpr.Evaluate(rpt, odata[sRow]);
                    if (Filter.ApplyCompare(tc, cmpvalue, v) != 0)
                        break;
                    sRow--;
                }
                sRow++;     // adjust; since we went just prior it
                            // look forward for ending row
                int eRow = iMainRow + 1;
                while (eRow < odata.Count)
                {
                    object v = parentExpr.Evaluate(rpt, odata[eRow]);
                    if (Filter.ApplyCompare(tc, cmpvalue, v) != 0)
                        break;
                    eRow++;
                }
                // Build a group entry for this
                GroupEntry ge2 = ges[r.Level] as GroupEntry;
                ge2.NestedGroup.Add(ge);
                if (r.Level + 1 >= ges.Count)   // ensure we have room in the array (based on level)
                    ges.Add(ge);                        // add to the array
                else
                {
                    ges[r.Level + 1] = ge;              // put this in the array
                }

                // add all of them in; want the same order for these rows.
                int offset = 1;
                for (int tRow = sRow; tRow < eRow; tRow++)
                {
                    Row tr = odata[tRow];
                    tr.Level = r.Level + 1;
                    newrows.Insert(iRow + offset, tr);
                    offset++;
                }
                // remove from old data
                odata.RemoveRange(sRow, eRow - sRow);
            }

            // update the groupentries for the very last row
            int lastrow = newrows.Count - 1;
            r = newrows[lastrow];
            for (int i = 0; i < r.Level + 1 && i < ges.Count; i++)
            {
                ge = ges[i] as GroupEntry;
                ge.EndRow = lastrow;
            }

            wc.Data.Data = newrows;     // we've completely replaced the rows
            return;
        }

        private void RunGroups(IPresent ip, List<GroupEntry> groupEntries, TableWorkClass wc)
        {
            Report rpt = ip.Report();
            GroupEntry fge = groupEntries[0];
            if (fge.Group != null)
                ip.GroupingStart(fge.Group);

            foreach (GroupEntry ge in groupEntries)
            {
                // set the group entry value
                int index;
                if (ge.Group != null)   // groups?
                {
                    ip.GroupingInstanceStart(ge.Group);
                    ge.Group.ResetHideDuplicates(rpt);  // reset duplicate checking
                    index = ge.Group.GetIndex(rpt); // yes
                }
                else                    // no; must be main dataset
                    index = 0;
                wc.Data.CurrentGroups[index] = ge;

                if (ge.NestedGroup.Count > 0)
                    RunGroupsSetGroups(rpt, wc, ge.NestedGroup);

                // Handle the group header
                if (ge.Group != null && ge.Group.Parent != null)
                {
                    TableGroup tg = ge.Group.Parent as TableGroup;
                    if (tg != null && tg.Header != null)
                    {
                        // Calculate the number of table rows below this group; header, footer, details count
                        if (ge.NestedGroup.Count > 0)
                            wc.GroupNestCount = RunGroupsCount(ge.NestedGroup, 0);
                        else
                            wc.GroupNestCount = (ge.EndRow - ge.StartRow + 1) * DetailsCount;
                        tg.Header.Run(ip, wc.Data.Data[ge.StartRow]);
                        wc.GroupNestCount = 0;
                    }
                }
                // Handle the nested groups if any
                if (ge.NestedGroup.Count > 0)
                    RunGroups(ip, ge.NestedGroup, wc);
                // If no nested groups then handle the detail rows for the group
                else if (Details != null)
                {
                    if (ge.Group != null &&
                        ge.Group.Parent as TableGroup == null)
                    {   // Group defined on table; means that Detail rows only put out once per group
                        Details.Run(ip, wc.Data, ge.StartRow, ge.StartRow);
                    }
                    else
                        Details.Run(ip, wc.Data, ge.StartRow, ge.EndRow);
                }

                // Do the group footer
                if (ge.Group != null)
                {
                    if (ge.Group.Parent != null)
                    {
                        TableGroup tg = ge.Group.Parent as TableGroup;  // detail groups will result in null
                        if (tg != null && tg.Footer != null)
                            tg.Footer.Run(ip, wc.Data.Data[ge.EndRow]);
                    }
                    ip.GroupingInstanceEnd(ge.Group);
                }
            }
            if (fge.Group != null)
                ip.GroupingEnd(fge.Group);
        }

        private void RunGroupsPage(Pages pgs, TableWorkClass wc, List<GroupEntry> groupEntries, int endRow, float groupHeight)
        {
            Report rpt = pgs.Report;
            foreach (GroupEntry ge in groupEntries)
            {
                // set the group entry value
                int index;
                if (ge.Group != null)   // groups?
                {
                    ge.Group.ResetHideDuplicates(rpt);  // reset duplicate checking
                    index = ge.Group.GetIndex(rpt); // yes
                }
                else                    // no; must be main dataset
                    index = 0;
                wc.Data.CurrentGroups[index] = ge;

                if (ge.NestedGroup.Count > 0)
                    RunGroupsSetGroups(rpt, wc, ge.NestedGroup);

                // Handle the group header
                if (ge.Group != null)
                {
                    TableGroup tg = ge.Group.Parent as TableGroup;
                    if (ge.Group.PageBreakAtStart && !pgs.CurrentPage.IsEmpty())
                    {
                        RunPageNew(pgs, pgs.CurrentPage);
                        RunPageHeader(pgs, wc.Data.Data[ge.StartRow], false, tg);
                    }

                    if (tg != null && tg.Header != null)
                    {
                        // Calculate the number of table rows below this group; header, footer, details count
                        if (ge.NestedGroup.Count > 0)
                            wc.GroupNestCount = RunGroupsCount(ge.NestedGroup, 0);
                        else
                            wc.GroupNestCount = (ge.EndRow - ge.StartRow + 1) * DetailsCount;

                        tg.Header.RunPage(pgs, wc.Data.Data[ge.StartRow]);
                        wc.GroupNestCount = 0;
                    }
                }
                float footerHeight = RunGroupsFooterHeight(pgs, wc, ge);


                if (ge.EndRow == endRow && (Footer != null ? !Footer.RepeatOnNewPage : true))
                {
                    footerHeight += groupHeight;
                }

                // Handle the nested groups if any
                if (ge.NestedGroup.Count > 0)
                    RunGroupsPage(pgs, wc, ge.NestedGroup, ge.EndRow, footerHeight);
                // If no nested groups then handle the detail rows for the group
                else if (Details != null)
                {
                    if (ge.Group != null &&
                        ge.Group.Parent as TableGroup == null)
                    {   // Group defined on table; means that Detail rows only put out once per group
                        Details.RunPage(pgs, wc.Data, ge.StartRow, ge.StartRow, footerHeight);
                    }
                    else
                    {
                        Details.RunPage(pgs, wc.Data, ge.StartRow, ge.EndRow, footerHeight);
                    }
                }
                else    // When no details we need to figure out whether any more fits on a page
                {
                    Page p = pgs.CurrentPage;
                    if (p.YOffset + footerHeight > pgs.BottomOfPage) //	Do we need new page to fit footer?
                    {
                        p = RunPageNew(pgs, p);
                        RunPageHeader(pgs, wc.Data.Data[ge.EndRow], false, null);
                    }
                }

                // Do the group footer
                if (ge.Group != null && ge.Group.Parent != null)
                {
                    TableGroup tg = ge.Group.Parent as TableGroup;  // detail groups will result in null
                    if (tg != null && tg.Footer != null)
                        tg.Footer.RunPage(pgs, wc.Data.Data[ge.EndRow]);

                    if (ge.Group.PageBreakAtEnd && !pgs.CurrentPage.IsEmpty())
                    {
                        RunPageNew(pgs, pgs.CurrentPage);
                        RunPageHeader(pgs, wc.Data.Data[ge.StartRow], false, tg);
                    }
                }

            }
        }

        private float RunGroupsFooterHeight(Pages pgs, TableWorkClass wc, GroupEntry ge)
        {
            Grouping g = ge.Group;
            if (g == null)
                return 0;

            TableGroup tg = g.Parent as TableGroup;     // detail groups will result in null
            if (tg == null || tg.Footer == null)
                return 0;

            return tg.Footer.HeightOfRows(pgs, wc.Data.Data[ge.EndRow]);
        }

        private int RunGroupsCount(List<GroupEntry> groupEntries, int count)
        {
            foreach (GroupEntry ge in groupEntries)
            {
                Grouping g = ge.Group;
                if (g != null)
                {
                    TableGroup tg = g.Parent as TableGroup;
                    if (tg != null)
                        count += (tg.HeaderCount + tg.FooterCount);
                }
                if (ge.NestedGroup.Count > 0)
                    count = RunGroupsCount(ge.NestedGroup, count);
                else
                    count += (ge.EndRow - ge.StartRow + 1) * DetailsCount;
            }
            return count;
        }

        internal void RunGroupsSetGroups(Report rpt, TableWorkClass wc, List<GroupEntry> groupEntries)
        {
            // set the group entry value
            GroupEntry ge = groupEntries[0];
            wc.Data.CurrentGroups[ge.Group.GetIndex(rpt)] = ge;

            if (ge.NestedGroup.Count > 0)
                RunGroupsSetGroups(rpt, wc, ge.NestedGroup);
        }

        private void RunRecursiveGroups(IPresent ip, TableWorkClass wc)
        {
            List<Row> rows = wc.Data.Data;
            Row r;

            // Get any header/footer information for use in loop
            Header header = null;
            Footer footer = null;
            TableGroup tg = wc.RecursiveGroup.Parent as TableGroup;
            if (tg != null)
            {
                header = tg.Header;
                footer = tg.Footer;
            }

            bool bHeader = true;
            for (int iRow = 0; iRow < rows.Count; iRow++)
            {
                r = rows[iRow];
                wc.Data.CurrentGroups[0] = r.GroupEntry;
                wc.GroupNestCount = r.GroupEntry.EndRow - r.GroupEntry.StartRow;
                if (bHeader)
                {
                    bHeader = false;
                    if (header != null)
                        header.Run(ip, r);
                }

                if (Details != null)
                {
                    Details.Run(ip, wc.Data, iRow, iRow);
                }

                // determine need for group headers and/or footers
                if (iRow < rows.Count - 1)
                {
                    Row r2 = rows[iRow + 1];
                    if (r.Level > r2.Level)
                    {
                        if (footer != null)
                            footer.Run(ip, r);
                    }
                    else if (r.Level < r2.Level)
                        bHeader = true;
                }
            }
            if (footer != null)
                footer.Run(ip, rows[rows.Count - 1] as Row);
        }

        private void RunRecursiveGroupsPage(Pages pgs, TableWorkClass wc)
        {
            List<Row> rows = wc.Data.Data;
            Row r;

            // Get any header/footer information for use in loop
            Header header = null;
            Footer footer = null;
            TableGroup tg = wc.RecursiveGroup.Parent as TableGroup;
            if (tg != null)
            {
                header = tg.Header;
                footer = tg.Footer;
            }

            bool bHeader = true;
            for (int iRow = 0; iRow < rows.Count; iRow++)
            {
                r = rows[iRow];
                wc.Data.CurrentGroups[0] = r.GroupEntry;
                wc.GroupNestCount = r.GroupEntry.EndRow - r.GroupEntry.StartRow;
                if (bHeader)
                {
                    bHeader = false;
                    if (header != null)
                    {
                        Page p = pgs.CurrentPage;           // this can change after running a row
                        float height = p.YOffset + header.HeightOfRows(pgs, r);
                        if (height > pgs.BottomOfPage)
                        {
                            p = RunPageNew(pgs, p);
                            RunPageHeader(pgs, r, false, null);
                            if (!header.RepeatOnNewPage)
                                header.RunPage(pgs, r);
                        }
                        else
                            header.RunPage(pgs, r);
                    }
                }

                // determine need for group headers and/or footers
                bool bFooter = false;
                float footerHeight = 0;

                if (iRow < rows.Count - 1)
                {
                    Row r2 = rows[iRow + 1];
                    if (r.Level > r2.Level)
                    {
                        if (footer != null)
                        {
                            bFooter = true;
                            footerHeight = footer.HeightOfRows(pgs, r);
                        }
                    }
                    else if (r.Level < r2.Level)
                        bHeader = true;
                }

                if (Details != null)
                {
                    Details.RunPage(pgs, wc.Data, iRow, iRow, footerHeight);
                }

                // and output the footer if needed
                if (bFooter)
                    footer.RunPage(pgs, r);
            }
            if (footer != null)
                footer.RunPage(pgs, rows[rows.Count - 1] as Row);
        }

        internal int GetGroupNestCount(Report rpt)
        {
            TableWorkClass wc = GetValue(rpt);
            return wc.GroupNestCount;
        }

        internal int WidthInPixels(Report rpt, Row row)
        {
            // Calculate this based on the sum of TableColumns
            int width = 0;
            foreach (TableColumn tc in this.TableColumns.Items)
            {
                if (tc.Visibility == null || !tc.Visibility.Hidden.EvaluateBoolean(rpt, row))
                    width += tc.Width.PixelsX;
            }
            return width;
        }

        internal int WidthInUnits
        {
            get
            {

                // Calculate this based on the sum of TableColumns
                int width = 0;
                foreach (TableColumn tc in this.TableColumns.Items)
                {
                    width += tc.Width.Size;
                }
                return width;
            }
        }

        private TableWorkClass GetValue(Report rpt)
        {
            TableWorkClass wc = rpt.Cache.Get(this, "wc") as TableWorkClass;
            if (wc == null)
            {
                wc = new TableWorkClass();
                rpt.Cache.Add(this, "wc", wc);
            }
            return wc;
        }

        private void RemoveValue(Report rpt)
        {
            rpt.Cache.Remove(this, "wc");
        }

    }

    class TableWorkClass
    {
        internal Rows Data; // Runtime data; either original query if no groups
                            // or sorting or a copied version that is grouped/sorted
        internal List<GroupEntry> Groups;       // Runtime groups; array of GroupEntry
        internal int GroupNestCount;    // Runtime: calculated on fly for # of table rows that are part of a group
                                        //    used to handle toggling of a group 
        internal Grouping RecursiveGroup;   // Runtime: set with a recursive; currently on support a single recursive group
        internal TableWorkClass()
        {
            Data = null;
            Groups = null;
            GroupNestCount = 0;
            RecursiveGroup = null;
        }
    }

    class RecursiveCompare : IComparer<Row>
    {
        readonly Report rpt;
        readonly TypeCode _Type;
        readonly IExpr parentExpr;
        readonly IExpr groupExpr;

        internal RecursiveCompare(Report r, IExpr pExpr, TypeCode tc, IExpr gExpr)
        {
            rpt = r;
            _Type = tc;
            parentExpr = pExpr;
            groupExpr = gExpr;
        }

        #region ====IComparer Members====
        public int Compare(Row x, Row y)
        {
            object xv = parentExpr.Evaluate(rpt, x);
            object yv = groupExpr.Evaluate(rpt, y);

            return -Filter.ApplyCompare(_Type, yv, xv);
        }
        #endregion
    }

    class GroupEntryCompare : IComparer<GroupEntry>
    {
        readonly Report rpt;
        readonly TableWorkClass wc;

        internal GroupEntryCompare(Report r, TableWorkClass w)
        {
            rpt = r;
            wc = w;
        }

        #region ====IComparer<GroupEntry> Members====
        public int Compare(GroupEntry x1, GroupEntry y1)
        {
            Debug.Assert(x1 != null && y1 != null && x1.Sort != null, "Illegal arguments to GroupEntryCompare",
                "Compare requires object to be of type GroupEntry and that sort be defined.");

            int rc = 0;
            foreach (SortBy sb in x1.Sort.Items)
            {
                TypeCode tc = sb.SortExpression.Type;

                int index = x1.Group.GetIndex(rpt);
                wc.Data.CurrentGroups[index] = x1;
                object o1 = sb.SortExpression.Evaluate(rpt, wc.Data.Data[x1.StartRow]);
                index = y1.Group.GetIndex(rpt);
                wc.Data.CurrentGroups[index] = y1;
                object o2 = sb.SortExpression.Evaluate(rpt, wc.Data.Data[y1.StartRow]);

                rc = Filter.ApplyCompare(tc, o1, o2);
                if (rc != 0)
                {
                    if (sb.Direction == SortDirectionEnum.Descending)
                        rc = -rc;
                    break;
                }
            }
            return rc;
        }
        #endregion
    }

}
