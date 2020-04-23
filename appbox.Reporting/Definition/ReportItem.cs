using System;
using System.Xml;
using appbox.Drawing;
using System.Collections.Generic;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// Base class of all display items in a report.  e.g. Textbox, Matrix, Table, ...
    ///</summary>
    [Serializable]
    internal class ReportItem : ReportLink, IComparable
    {
        #region ====Fields & Properties====
        /// <summary>
        /// Name of the report item
        /// </summary>
        internal Name Name { get; set; }

        /// <summary>
        /// Style information for the element
        /// </summary>
        internal Style Style { get; set; }

        /// <summary>
        /// An action (e.g. a hyperlink) associated with the ReportItem
        /// </summary>
        internal Action Action { get; set; }

        /// <summary>
        /// The distance of the item from the top of the containing object.
        /// Defaults to 0 if omitted.
        /// </summary>
        internal RSize Top { get; set; }

        /// <summary>
        /// The distance of the item from the left of the containing object.
        /// Defaults to 0 if omitted.
        /// </summary>
        internal RSize Left { get; set; }

        /// <summary>
        /// Height of the item. Negative sizes allowed only for lines (The height/width gives the
        /// offset of the endpoint of the line from the start point).
        /// Defaults to the height of the containing object minus Top if omitted.
        /// </summary>
        internal RSize Height { get; set; }

        // routine returns the height; If not specified go up the owner chain
        //   to find an appropriate containing object
        internal float HeightOrOwnerHeight
        {
            get
            {
                if (Height != null)
                    return Height.Points;

                float yloc = this.Top == null ? 0 : this.Top.Points;

                for (ReportLink rl = this.Parent; rl != null; rl = rl.Parent)
                {
                    if (rl is ReportItem)
                    {
                        ReportItem ri = rl as ReportItem;
                        if (ri.Height != null)
                            return ri.Height.Points - yloc;
                        continue;
                    }
                    if (rl is PageHeader)
                    {
                        PageHeader ph = rl as PageHeader;
                        if (ph.Height != null)
                            return ph.Height.Points - yloc;
                        continue;
                    }
                    if (rl is PageFooter)
                    {
                        PageFooter pf = rl as PageFooter;
                        if (pf.Height != null)
                            return pf.Height.Points - yloc;
                        continue;
                    }
                    if (rl is TableRow)
                    {
                        TableRow tr = rl as TableRow;
                        if (tr.Height != null)
                            return tr.Height.Points - yloc;
                        continue;
                    }
                    if (rl is MatrixRow)
                    {
                        MatrixRow mr = rl as MatrixRow;
                        if (mr.Height != null)
                            return mr.Height.Points - yloc;
                        continue;
                    }
                    if (rl is Body)
                    {
                        Body b = rl as Body;
                        if (b.Height != null)
                            return b.Height.Points - yloc;
                        continue;
                    }
                }
                return OwnerReport.PageHeight.Points;
            }
        }

        /// <summary>
        /// Width of the item. Negative sizes allowed only for lines.
        /// Defaults to the width of the containing object minus Left if omitted.
        /// </summary>
        internal RSize Width { get; set; }

        /// <summary>
        /// Drawing order of the report item within the containing object.
        /// Items with lower indices are drawn first (appearing behind items with
        /// higher indices). Items with equal indices have an unspecified order.
        /// Default: 0 Min: 0 Max: 2147483647
        /// </summary>
        internal int ZIndex { get; set; }

        /// <summary>
        /// Indicates if the item should be hidden.
        /// </summary>
        internal Visibility Visibility { get; set; }

        /// <summary>
        /// (string) A textual label for the report item. Used for
        /// such things as including TITLE and ALT attributes in HTML reports.
        /// </summary>
        internal Expression ToolTip { get; set; }

        /// <summary>
        /// A label to identify an instance of a report item
        /// (Variant) within the client UI (to provide a user-friendly label for searching)
        /// Hierarchical listing of report item and group
        /// labels within the UI (the Document Map)
        /// should reflect the object containment
        /// hierarchy in the report definition. Peer items
        /// should be listed in left-to-right top-to-bottom order.
        /// If the expression returns null, no item is added
        /// to the Document Map. Not used for report items in the page header or footer.
        /// </summary>
        internal Expression Label { get; set; }

        /// <summary>
        /// The name of a report item contained directly
        /// within this report item that is the target
        /// location for the Document Map label (if any).
        /// Ignored if Label is not present. Used only for Rectangle.
        /// </summary>
        internal string LinkToChild { get; set; }

        /// <summary>
        /// (string)A bookmark that can be linked to via a Bookmark action
        /// </summary>
        internal Expression Bookmark { get; set; }

        /// <summary>
        /// TableCell- if part of a Table
        /// </summary>
        internal TableCell TC { get; private set; }

        /// <summary>
        /// The name of a data region that this report item
        /// should be repeated with if that data region spans multiple pages.
        /// The data region must be in the same ReportItems collection as this ReportItem
        /// (Since data regions are not allowed in page headers/footers, this means RepeatWith will
        /// be unusable in page headers/footers).
        /// Not allowed if this report item is a data region, subreport or rectangle that contains a
        /// data region or subreport.
        /// </summary>
        internal string RepeatWith { get; set; }

        /// <summary>
        /// Custom information to be handed to a report output component.
        /// </summary>
        internal Custom Custom { get; set; }

        /// <summary>
        /// calculated: when calculating the y position these are the items above it
        /// </summary>
        internal List<ReportItem> YParents { get; private set; }

        private string _DataElementName;
        /// <summary>
        /// The name to use for the data element/attribute for this report item.
        /// Default: Name of the report item
        /// </summary>
        internal string DataElementName
        {
            get
            {
                if (_DataElementName != null)
                    return _DataElementName;
                else if (Name != null)
                    return Name.Nm;
                else
                    return null;
            }
            set { _DataElementName = value; }
        }

        private DataElementOutputEnum _DataElementOutput;
        /// <summary>
        /// should item appear in data rendering?
        /// </summary>
        virtual internal DataElementOutputEnum DataElementOutput
        {
            get
            {
                if (_DataElementOutput == DataElementOutputEnum.Auto)
                {
                    if (this is Textbox)
                    {
                        Textbox tb = this as Textbox;
                        if (tb.Value.IsConstant())
                            return DataElementOutputEnum.NoOutput;
                        else
                            return DataElementOutputEnum.Output;
                    }
                    if (this is Rectangle)
                        return DataElementOutputEnum.ContentsOnly;

                    return DataElementOutputEnum.Output;
                }
                else
                    return _DataElementOutput;
            }
            set { _DataElementOutput = value; }
        }

        private bool _InMatrix;     // true if reportitem is in a matrix

        internal bool IsInBody => Parent.Parent is Body;
        #endregion

        #region ====Ctor====
        internal ReportItem(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            Name = null;
            Style = null;
            Action = null;
            Top = null;
            Left = null;
            Height = null;
            Width = null;
            ZIndex = 0;
            Visibility = null;
            ToolTip = null;
            Label = null;
            LinkToChild = null;
            Bookmark = null;
            RepeatWith = null;
            Custom = null;
            _DataElementName = null;
            _DataElementOutput = DataElementOutputEnum.Auto;
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
        }
        #endregion

        internal bool ReportItemElement(XmlNode xNodeLoop)
        {
            switch (xNodeLoop.Name)
            {
                case "Style":
                    Style = new Style(OwnerReport, this, xNodeLoop);
                    break;
                case "Action":
                    Action = new Action(OwnerReport, this, xNodeLoop);
                    break;
                case "Top":
                    Top = new RSize(OwnerReport, xNodeLoop);
                    break;
                case "Left":
                    Left = new RSize(OwnerReport, xNodeLoop);
                    break;
                case "Height":
                    Height = new RSize(OwnerReport, xNodeLoop);
                    break;
                case "Width":
                    Width = new RSize(OwnerReport, xNodeLoop);
                    break;
                case "ZIndex":
                    ZIndex = XmlUtil.Integer(xNodeLoop.InnerText);
                    break;
                case "Visibility":
                    Visibility = new Visibility(OwnerReport, this, xNodeLoop);
                    break;
                case "ToolTip":
                    ToolTip = new Expression(OwnerReport, this, xNodeLoop, ExpressionType.String);
                    break;
                case "Label":
                    Label = new Expression(OwnerReport, this, xNodeLoop, ExpressionType.Variant);
                    break;
                case "LinkToChild":
                    LinkToChild = xNodeLoop.InnerText;
                    break;
                case "Bookmark":
                    Bookmark = new Expression(OwnerReport, this, xNodeLoop, ExpressionType.String);
                    break;
                case "RepeatWith":
                    RepeatWith = xNodeLoop.InnerText;
                    break;
                case "Custom":
                    Custom = new Custom(OwnerReport, this, xNodeLoop);
                    break;
                case "DataElementName":
                    _DataElementName = xNodeLoop.InnerText;
                    break;
                case "DataElementOutput":
                    _DataElementOutput = RDL.DataElementOutput.GetStyle(xNodeLoop.InnerText, OwnerReport.rl);
                    break;
                case "rd:DefaultName":
                    break;      // MS tag: we don't use but don't want to generate a warning
                default:
                    return false;   // Not a report item element
            }
            return true;
        }

        // Handle parsing of function in final pass
        override internal void FinalPass()
        {
            if (Style != null)
                Style.FinalPass();
            if (Action != null)
                Action.FinalPass();
            if (Visibility != null)
                Visibility.FinalPass();
            if (ToolTip != null)
                ToolTip.FinalPass();
            if (Label != null)
                Label.FinalPass();
            if (Bookmark != null)
                Bookmark.FinalPass();
            if (Custom != null)
                Custom.FinalPass();

            if (Parent.Parent is TableCell) // This is part of a table
            {
                TC = Parent.Parent as TableCell;
            }
            else
            {
                TC = null;
            }

            // Determine if ReportItem is defined inside of a Matrix
            _InMatrix = false;
            for (ReportLink rl = this.Parent; rl != null; rl = rl.Parent)
            {
                if (rl is Matrix)
                {
                    _InMatrix = true;
                    break;
                }
                if (rl is Table || rl is List || rl is Chart)
                    break;
            }

            return;
        }

        internal void PositioningFinalPass(int i, List<ReportItem> items)
        {
            if (items.Count == 1 || i == 0)     // Nothing to do if only one item in list or 1st item in list
                return;

            int x = this.Left == null ? 0 : this.Left.Size;
            int w = PositioningWidth(this);
            int right = x + w;
            int y = (this.Top == null ? 0 : this.Top.Size);
            if (this is Line)
            {   // normalize the width
                if (w < 0)
                {
                    x -= w;
                    w = -w;
                }
            }

            this.YParents = new List<ReportItem>();
            int maxParents = 100;               // heuristic to limit size of parents; otherwise processing in
                                                //   extreme cases can blow up
            for (int ti = i - 1; ti >= 0 && maxParents > 0; ti--)
            {
                ReportItem ri = items[ti];

                int xw = ri.Left == null ? 0 : ri.Left.Size;
                int w2 = PositioningWidth(ri);
                if (ri is Line)
                {   // normalize the width
                    if (w2 < 0)
                    {
                        xw -= w2;
                        w2 = -w2;
                    }
                }
                if (ri.Height == null || ri.Top == null) // if position/height not specified don't use to reposition
                    continue;
                if (y < ri.Top.Size + ri.Height.Size)
                    continue;
                YParents.Add(ri);		// X coordinate overlap
                maxParents--;
                if (xw <= x && xw + w2 >= x + w &&       // if item above completely covers the report item then it will be pushed down first
                    maxParents > 30)                      //   and we haven't already set the maxParents.   
                    maxParents = 30;                        //   just add a few more if necessary 
            }
            //foreach (ReportItem ri in items)
            //{
            //    if (ri == this)
            //        break;

            //    int xw = ri.Left == null ? 0 : ri.Left.Size;
            //    int w2 = PositioningWidth(ri);
            //    if (ri is Line)
            //    {   // normalize the width
            //        if (w2 < 0)
            //        {
            //            xw -= w2;
            //            w2 = -w2;
            //        }
            //    }
            //    //if (xw > right || x > xw + w2)                    // this allows items to be repositioned only based on what's above them
            //    //    continue;
            //    if (ri.Height == null || ri.Top == null)          // if position/height not specified don't use to reposition
            //        continue;
            //    if (y < ri.Top.Size + ri.Height.Size)
            //        continue;
            //    _YParents.Add(ri);		// X coordinate overlap
            //}

            // Reduce the overhead
            if (this.YParents.Count == 0)
                this.YParents = null;
            else
                this.YParents.TrimExcess();

            return;
        }

        int PositioningWidth(ReportItem ri)
        {
            int w;
            if (ri.Width == null)
            {
                if (ri is Table)
                {
                    Table t = ri as Table;
                    w = t.WidthInUnits;
                }
                else
                    w = int.MaxValue / 2;   // MaxValue/2 is just meant to be a large number (but won't overflow when adding in the x)
            }
            else
                w = ri.Width.Size;

            return w;
        }

        internal virtual void Run(IPresent ip, Row row)
        {
            return;
        }

        internal virtual void RunPage(Pages pgs, Row row)
        {
            return;
        }

        internal bool IsTableOrMatrixCell(Report rpt)
        {
            WorkClass wc = GetWC(rpt);
            return (TC != null || wc.MC != null || this._InMatrix);
        }

        internal float LeftCalc(Report rpt)
        {
            WorkClass wc = GetWC(rpt);
            if (TC != null || wc.MC != null || Left == null)
                return 0;
            else
                return Left.Points;
        }

        internal float GetOffsetCalc(Report rpt)
        {
            WorkClass wc = GetWC(rpt);
            float x;
            if (this.TC != null)
            {   // must be part of a table
                Table t = TC.OwnerTable;
                int colindex = TC.ColIndex;

                TableColumn tc;
                tc = (TableColumn)(t.TableColumns.Items[colindex]);
                x = tc.GetXPosition(rpt);
            }
            else if (wc.MC != null)
            {   // must be part of a matrix
                x = wc.MC.XPosition;
            }
            else
            {
                ReportItems ris = this.Parent as ReportItems;
                x = ris.GetXOffset(rpt);
            }

            return x;
        }

        internal bool IsHidden(Report rpt, Row r)
        {
            if (this.Visibility == null)
                return false;
            return Visibility.IsHidden(rpt, r);
        }

        internal void SetPageLeft(Report rpt)
        {
            if (this.TC != null)
            {   // must be part of a table
                Table t = TC.OwnerTable;
                int colindex = TC.ColIndex;
                TableColumn tc = (TableColumn)(t.TableColumns.Items[colindex]);
                Left = new RSize(OwnerReport, tc.GetXPosition(rpt).ToString() + "pt");
            }
            else if (Left == null)
                Left = new RSize(OwnerReport, "0pt");
        }

        internal void SetPagePositionAndStyle(Report rpt, PageItem pi, Row row)
        {
            WorkClass wc = GetWC(rpt);
            pi.X = GetOffsetCalc(rpt) + LeftCalc(rpt);
            if (this.TC != null)
            {   // must be part of a table
                Table t = TC.OwnerTable;
                int colindex = TC.ColIndex;

                // Calculate width: add up all columns within the column span
                float width = 0;
                TableColumn tc;
                for (int ci = colindex; ci < colindex + TC.ColSpan; ci++)
                {
                    tc = (TableColumn)(t.TableColumns.Items[ci]);
                    width += tc.Width.Points;
                }
                pi.W = width;
                pi.Y = 0;

                TableRow tr = (TableRow)(TC.Parent.Parent);
                pi.H = tr.HeightCalc(rpt);  // this is a cached item; note tr.HeightOfRow must already be called on row
            }
            else if (wc.MC != null)
            {   // must be part of a matrix
                pi.W = wc.MC.Width;
                pi.Y = 0;
                pi.H = wc.MC.Height;
            }
            else if (pi is PageLine)
            {   // don't really handle if line is part of table???  TODO
                PageLine pl = (PageLine)pi;
                if (Top != null)
                    pl.Y = this.Gap(rpt);		 //  y will get adjusted when pageitem added to page
                float y2 = pl.Y;
                if (Height != null)
                    y2 += Height.Points;
                pl.Y2 = y2;
                pl.X2 = pl.X;
                if (Width != null)
                    pl.X2 += Width.Points;
            }
            else
            {	// not part of a table or matrix
                if (Top != null)
                    pi.Y = this.Gap(rpt);		 //  y will get adjusted when pageitem added to page
                if (Height != null)
                    pi.H = Height.Points;
                else
                    pi.H = this.HeightOrOwnerHeight;
                if (Width != null)
                    pi.W = Width.Points;
                else
                    pi.W = this.WidthOrOwnerWidth(rpt);
            }
            if (Style != null)
                pi.SI = Style.GetStyleInfo(rpt, row);
            else
                pi.SI = new StyleInfo();	// this will just default everything

            pi.ZIndex = this.ZIndex;        // retain the zindex of the object

            // Catch any action needed
            if (this.Action != null)
            {
                pi.BookmarkLink = Action.BookmarkLinkValue(rpt, row);
                pi.HyperLink = Action.HyperLinkValue(rpt, row);
            }

            if (this.Bookmark != null)
                pi.Bookmark = Bookmark.EvaluateString(rpt, row);

            if (this.ToolTip != null)
                pi.Tooltip = ToolTip.EvaluateString(rpt, row);
        }

        internal MatrixCellEntry GetMC(Report rpt)
        {
            WorkClass wc = GetWC(rpt);
            return wc.MC;
        }

        internal void SetMC(Report rpt, MatrixCellEntry mce)
        {
            WorkClass wc = GetWC(rpt);
            wc.MC = mce;
        }

        internal float WidthOrOwnerWidth(Report rpt)
        {
            if (Width != null)
                return Width.Points;
            float xloc = this.LeftCalc(rpt);

            for (ReportLink rl = this.Parent; rl != null; rl = rl.Parent)
            {
                if (rl is ReportItem)
                {
                    ReportItem ri = rl as ReportItem;
                    if (ri.Width != null)
                        return ri.Width.Points - xloc;
                    continue;
                }
                if (rl is PageHeader ||
                    rl is PageFooter ||
                    rl is Body)
                {
                    return OwnerReport.Width.Points - xloc;
                }
            }

            return OwnerReport.Width.Points - xloc;
        }

        internal int WidthCalc(Report rpt, Graphics g)
        {
            WorkClass wc = GetWC(rpt);
            int width;
            if (TC != null)
            {   // must be part of a table
                Table t = TC.OwnerTable;
                int colindex = TC.ColIndex;

                // Calculate width: add up all columns within the column span
                width = 0;
                TableColumn tc;
                for (int ci = colindex; ci < colindex + TC.ColSpan; ci++)
                {
                    tc = t.TableColumns.Items[ci];
                    width += g == null ? tc.Width.PixelsX : tc.Width.ToPixels((decimal)g.DpiX);
                }
            }
            else if (wc.MC != null)
            {   // must be part of a matrix
                width = g == null ? RSize.PixelsFromPoints(wc.MC.Width)
                    : RSize.PixelsFromPoints(wc.MC.Width, g.DpiX);
            }
            else
            {   // not part of a table or matrix
                if (Width != null)
                    width = g == null ? Width.PixelsX : Width.ToPixels((decimal)g.DpiX);
                else
                    width = g == null ? RSize.PixelsFromPoints(WidthOrOwnerWidth(rpt))
                        : RSize.PixelsFromPoints(WidthOrOwnerWidth(rpt), g.DpiX);
            }
            return width;
        }

        internal Page RunPageNew(Pages pgs, Page p)
        {
            if (p.IsEmpty())            // if the page is empty it won't help to create another one
                return p;

            // Do we need a new page or have should we fill out more body columns
            Body b = OwnerReport.Body;
            int ccol = b.IncrCurrentColumn(pgs.Report); // bump to next column

            float top = OwnerReport.TopOfPage;  // calc top of page

            if (ccol < b.Columns)
            {       // Stay on same page but move to new column
                p.XOffset =
                    ((OwnerReport.Width.Points + b.ColumnSpacing.Points) * ccol);
                p.YOffset = top;
                p.SetEmpty();           // consider this page empty
            }
            else
            {       // Go to new page
                b.SetCurrentColumn(pgs.Report, 0);
                pgs.NextOrNew();
                p = pgs.CurrentPage;
                p.YOffset = top;
                p.XOffset = 0;
            }

            return p;
        }

        /// <summary>
        /// Updates the current page and location based on the ReportItems 
        /// that are above it in the report.
        /// </summary>
        /// <param name="pgs"></param>
        internal void SetPagePositionBegin(Pages pgs)
        {
            // Update the current page
            if (this.YParents != null)
            {
                ReportItem saveri = GetReportItemAbove(pgs.Report);
                if (saveri != null)
                {
                    WorkClass wc = saveri.GetWC(pgs.Report);
                    pgs.CurrentPage = wc.CurrentPage;
                    pgs.CurrentPage.YOffset = wc.BottomPosition;
                }
            }
            else if (this.Parent.Parent is PageHeader)
            {
                pgs.CurrentPage.YOffset = OwnerReport.TopMargin.Points;

            }
            else if (this.Parent.Parent is PageFooter)
            {
                pgs.CurrentPage.YOffset = OwnerReport.PageHeight.Points
                    - OwnerReport.BottomMargin.Points
                    - OwnerReport.PageFooter.Height.Points;
            }
            else if (!(this.Parent.Parent is Body))
            {   // if not body then we don't need to do anything
            }
            else if (this.OwnerReport.Subreport != null)
            {
                //				pgs.CurrentPage = this.OwnerReport.Subreport.FirstPage;
                //				pgs.CurrentPage.YOffset = top;
            }
            else
            {
                pgs.CurrentPage = pgs.FirstPage;    // if nothing above it (in body) then it goes on first page
                pgs.CurrentPage.YOffset = OwnerReport.TopOfPage;
            }

            return;
        }

        internal void SetPagePositionEnd(Pages pgs, float pos)
        {
            if (TC != null || _InMatrix)           // don't mess with page if part of a table or in a matrix
                return;
            WorkClass wc = GetWC(pgs.Report);
            wc.CurrentPage = pgs.CurrentPage;
            wc.BottomPosition = pos;
        }

        /// <summary>
        /// Calculates the runtime y position of the object based on the height of objects 
        /// above it vertically.
        /// </summary>
        internal float Gap(Report rpt)
        {
            float top = Top == null ? 0 : Top.Points;
            ReportItem saveri = GetReportItemAbove(rpt);
            if (saveri == null)
                return top;

            float gap = top;
            float s_top = saveri.Top == null ? 0 : saveri.Top.Points;
            float s_h = saveri.Height == null ? 0 : saveri.Height.Points;

            gap -= saveri.Top.Points;
            if (top < s_top + s_h)          // do we have an overlap;
                gap = top - (s_top + s_h);    // yes; force overlap even when moving report item down
            else
                gap -= saveri.Height.Points;  // no; move report item down just the gap between the items  

            return gap;
        }

        /// <summary>
        /// Calculates the runtime y position of the object based on the height of objects 
        /// above it vertically.
        /// </summary>
        internal float RelativeY(Report rpt)
        {
            float top = Top == null ? 0 : Top.Points;
            ReportItem saveri = GetReportItemAbove(rpt);
            if (saveri == null)
                return top;

            float gap = top;
            if (saveri.Top != null)
                gap -= saveri.Top.Points;
            if (saveri.Height != null)
                gap -= saveri.Height.Points;

            return gap;
        }

        private ReportItem GetReportItemAbove(Report rpt)
        {
            if (this.YParents == null)
                return null;

            float maxy = float.MinValue;
            ReportItem saveri = null;
            int pgno = 0;

            foreach (ReportItem ri in this.YParents)
            {
                WorkClass wc = ri.GetWC(rpt);
                if (wc.BottomPosition.CompareTo(float.NaN) == 0 ||
                    wc.CurrentPage == null ||
                    pgno > wc.CurrentPage.PageNumber)
                    continue;
                if (maxy < wc.BottomPosition || pgno < wc.CurrentPage.PageNumber)
                {
                    pgno = wc.CurrentPage.PageNumber;
                    maxy = wc.BottomPosition;
                    saveri = ri;
                }
            }
            return saveri;
        }

        internal string ToolTipValue(Report rpt, Row r)
        {
            if (ToolTip == null)
                return null;

            return ToolTip.EvaluateString(rpt, r);
        }

        internal string BookmarkValue(Report rpt, Row r)
        {
            if (Bookmark == null)
                return null;

            return Bookmark.EvaluateString(rpt, r);
        }

        private WorkClass GetWC(Report rpt)
        {
            if (rpt == null)
                return new WorkClass();

            WorkClass wc = rpt.Cache.Get(this, "riwc") as WorkClass;
            if (wc == null)
            {
                wc = new WorkClass();
                rpt.Cache.Add(this, "riwc", wc);
            }
            return wc;
        }

        internal virtual void RemoveWC(Report rpt)
        {
            rpt.Cache.Remove(this, "riwc");
        }

        class WorkClass
        {
            internal MatrixCellEntry MC;    // matrix cell entry
            internal float BottomPosition;  // used when calculating position of objects below this one.
                                            // this must be initialized by the inheriting class.
            internal Page CurrentPage;      // the page this reportitem was last put on; 
            internal WorkClass()
            {
                MC = null;
                BottomPosition = float.NaN;
                CurrentPage = null;
            }
        }

        #region ====IComparable Members====
        // Sort report items based on top down, left to right
        public int CompareTo(object obj)
        {
            ReportItem ri = obj as ReportItem;

            int t1 = this.Top == null ? 0 : this.Top.Size;
            int t2 = ri.Top == null ? 0 : ri.Top.Size;

            int rc = t1 - t2;
            if (rc != 0)
                return rc;

            int l1 = this.Left == null ? 0 : this.Left.Size;
            int l2 = ri.Left == null ? 0 : ri.Left.Size;

            return l1 - l2;
        }
        #endregion
    }
}
