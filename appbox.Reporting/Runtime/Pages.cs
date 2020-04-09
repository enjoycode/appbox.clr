using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using appbox.Drawing;
using appbox.Reporting.Resources;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// Represents all the pages of a report.  Needed when you need
    /// render based on pages.  e.g. PDF
    ///</summary>
    public class Pages : IEnumerable
    {
        private Bitmap _bm;                     // bitmap to build graphics object 
        private Graphics _g;                    // graphics object
        private readonly List<Page> _pages;     // array of pages
        private Page _currentPage;              // the current page; 1st page if null

        /// <summary>
        /// the bottom of the page
        /// </summary>
        public float BottomOfPage { get; set; }

        public Page CurrentPage
        {
            get
            {
                if (_currentPage != null)
                    return _currentPage;

                if (_pages.Count >= 1)
                {
                    _currentPage = _pages[0];
                    return _currentPage;
                }

                return null;
            }
            set
            {
                _currentPage = value;
#if DEBUG
                if (value == null)
                    return;
                foreach (Page p in _pages)
                {
                    if (p == value)
                        return;
                }
                throw new Exception(Strings.Pages_Error_CurrentPageMustInList);
#endif
            }
        }

        public Page FirstPage => _pages.Count <= 0 ? null : _pages[0];

        public Page LastPage => _pages.Count <= 0 ? null : _pages[_pages.Count - 1];

        /// <summary>
        /// default height for all pages
        /// </summary>
        public float PageHeight { get; set; }

        /// <summary>
        /// default width for all pages
        /// </summary>
        public float PageWidth { get; set; }

        public Graphics G
        {
            get
            {
                if (_g == null)
                {
                    _bm = new Bitmap(10, 10);   // create a small bitmap to base our graphics
                    _g = Graphics.FromImage(_bm);
                }
                return _g;
            }
        }

        public int PageCount => _pages.Count;

        public Pages(Report r)
        {
            Report = r;
            _pages = new List<Page>();  // array of Page objects

            _bm = new Bitmap(10, 10);   // create a small bitmap to base our graphics
            _g = Graphics.FromImage(_bm);
        }

        /// <summary>
        /// owner report
        /// </summary>
        internal Report Report { get; }

        public Page this[int index]
        {
            get { return _pages[index]; }
        }

        public int Count
        {
            get { return _pages.Count; }
        }

        public void AddPage(Page p)
        {
            _pages.Add(p);
            _currentPage = p;
        }

        public void NextOrNew()
        {
            if (_currentPage == this.LastPage)
                AddPage(new Page(PageCount + 1));
            else
            {
                _currentPage = _pages[_currentPage.PageNumber];
                _currentPage.SetEmpty();
            }
            //Allows using PageNumber in report body.
            //Important! This feature is NOT included in RDL specification!
            //PageNumber will be wrong if element using it will cause carry to next page after render.
            Report.PageNumber = _currentPage.PageNumber;
        }

        /// <summary>
        /// CleanUp should be called after every render to reduce resource utilization.
        /// </summary>
        public void CleanUp()
        {
            if (_g != null)
            {
                _g.Dispose();
                _g = null;
            }
            if (_bm != null)
            {
                _bm.Dispose();
                _bm = null;
            }
        }

        public void SortPageItems()
        {
            foreach (Page p in this)
            {
                p.SortPageItems();
            }
        }

        public void RemoveLastPage()
        {
            Page lp = LastPage;

            if (lp == null)             // if no last page nothing to do
                return;

            _pages.RemoveAt(_pages.Count - 1);  // remove the page

            if (this.CurrentPage == lp) // reset the current if necessary
            {
                if (_pages.Count <= 0)
                    CurrentPage = null;
                else
                    CurrentPage = _pages[_pages.Count - 1];
            }

            return;
        }

        #region IEnumerable Members
        public IEnumerator GetEnumerator()      // just loop thru the pages
        {
            return _pages.GetEnumerator();
        }
        #endregion
    }

    public class Page : IEnumerable
    {
        public int PageNumber { get; }

        private readonly List<PageItem> _items; // array of items on the page

        public int Count => _items.Count;

        /// <summary>
        /// current x offset; margin, body taken into account?
        /// </summary>
        public float XOffset { get; set; }
        /// <summary>
        /// current y offset; top margin, page header, other details, ... 
        /// </summary>
        public float YOffset { get; set; }

        int _emptyItems;				// # of items which constitute empty
        bool _needSort;                 // need sort
        int _lastZIndex;                // last ZIndex
        Dictionary<string, Rows> _PageExprReferences;    // needed to save page header/footer expressions

        public Page(int page)
        {
            PageNumber = page;
            _items = new List<PageItem>();
            _emptyItems = 0;
            _needSort = false;
        }

        public PageItem this[int index]
        {
            get { return _items[index]; }
        }

        public void InsertObject(PageItem pi)
        {
            AddObjectInternal(pi);
            _items.Insert(0, pi);
        }

        public void AddObject(PageItem pi)
        {
            AddObjectInternal(pi);
            _items.Add(pi);
        }

        private void AddObjectInternal(PageItem pi)
        {
            pi.Page = this;
            pi.ItemNumber = _items.Count;
            if (_items.Count == 0)
                _lastZIndex = pi.ZIndex;
            else if (_lastZIndex != pi.ZIndex)
                _needSort = true;

            // adjust the page item locations
            pi.X += XOffset;
            pi.Y += YOffset;
            if (pi is PageLine)
            {
                PageLine pl = pi as PageLine;
                pl.X2 += XOffset;
                pl.Y2 += YOffset;
            }
            else if (pi is PagePolygon)
            {
                PagePolygon pp = pi as PagePolygon;
                for (int i = 0; i < pp.Points.Length; i++)
                {
                    pp.Points[i].X += XOffset;
                    pp.Points[i].Y += YOffset;
                }
            }
            else if (pi is PageCurve)
            {
                PageCurve pc = pi as PageCurve;
                for (int i = 0; i < pc.Points.Length; i++)
                {
                    pc.Points[i].X += XOffset;
                    pc.Points[i].Y += YOffset;
                }
            }
        }

        public bool IsEmpty() => _items.Count > _emptyItems ? false : true;

        public void SortPageItems()
        {
            if (!_needSort)
                return;
            _items.Sort();
        }

        public void ResetEmpty() => _emptyItems = 0;

        public void SetEmpty() => _emptyItems = _items.Count;

        internal void AddPageExpressionRow(Report rpt, string exprname, Row r)
        {
            if (exprname == null || r == null)
                return;

            if (_PageExprReferences == null)
                _PageExprReferences = new Dictionary<string, Rows>();

            Rows rows = null;
            _PageExprReferences.TryGetValue(exprname, out rows);
            if (rows == null)
            {
                rows = new Rows(rpt);
                rows.Data = new List<Row>();
                _PageExprReferences.Add(exprname, rows);
            }
            Row row = new Row(rows, r); // have to make a new copy
            row.RowNumber = rows.Data.Count;
            rows.Data.Add(row);         // add row to rows
            return;
        }

        internal Rows GetPageExpressionRows(string exprname)
        {
            if (_PageExprReferences == null)
                return null;

            Rows rows = null;
            _PageExprReferences.TryGetValue(exprname, out rows);
            return rows;
        }

        internal void ResetPageExpressions()
        {
            _PageExprReferences = null;    // clear it out; not needed once page header/footer are processed
        }

        #region IEnumerable Members
        public IEnumerator GetEnumerator()      // just loop thru the pages
        {
            return _items.GetEnumerator();
        }
        #endregion
    }

    public class PageItem : ICloneable, IComparable
    {
        /// <summary>
        /// parent page
        /// </summary>
        public Page Page { get; set; }

        /// <summary>
        /// allow selection of this item
        /// </summary>
        public bool AllowSelect { get; set; } = true;

        /// <summary>
        /// x coordinate
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// y coordinate
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// zindex; items will be sorted by this
        /// </summary>
        public int ZIndex { get; set; }

        /// <summary>
        /// original number of item
        /// </summary>
        public int ItemNumber { get; set; }

        /// <summary>
        /// height  --- line redefines as Y2
        /// </summary>
        public float H { get; set; }

        /// <summary>
        /// width   --- line redefines as X2
        /// </summary>
        public float W { get; set; }

        /// <summary>
        /// a hyperlink the object should link to
        /// </summary>
        public string HyperLink { get; set; }

        /// <summary>
        /// a hyperlink within the report object should link to
        /// </summary>
        public string BookmarkLink { get; set; }

        /// <summary>
        /// bookmark text for this pageItem
        /// </summary>
        public string Bookmark { get; set; }

        /// <summary>
        /// a message to display when user hovers with mouse
        /// </summary>
        public string Tooltip { get; set; }

        /// <summary>
        /// all the style information evaluated
        /// </summary>
        public StyleInfo SI { get; set; }

        #region ICloneable Members
        public object Clone()
        {
            return this.MemberwiseClone();
        }
        #endregion

        #region IComparable Members
        // Sort items based on zindex, then on order items were added to array
        public int CompareTo(object obj)
        {
            PageItem pi = obj as PageItem;

            int rc = this.ZIndex - pi.ZIndex;
            if (rc == 0)
                rc = this.ItemNumber - pi.ItemNumber;
            return rc;
        }
        #endregion
    }

    public class PageImage : PageItem, ICloneable
    {
        public PageImage(ImageFormat im, byte[] image, int w, int h)
        {
            Debug.Assert(im == ImageFormat.Jpeg || im == ImageFormat.Png || im == ImageFormat.Gif || im == ImageFormat.Wmf,
                            "PageImage only supports Jpeg, Gif and Png and WMF image formats (Thanks HYNE!).");
            ImgFormat = im;
            ImageData = image;
            SamplesW = w;
            SamplesH = h;
            Repeat = ImageRepeat.NoRepeat;
            Sizing = ImageSizingEnum.AutoSize;
        }

        public byte[] ImageData { get; }

        /// <summary>
        /// type of image; png, jpeg are supported
        /// </summary>
        public ImageFormat ImgFormat { get; }

        /// <summary>
        /// name of object if constant image
        /// </summary>
        public string Name { get; set; }

        public ImageRepeat Repeat { get; set; }

        public ImageSizingEnum Sizing { get; set; }

        public int SamplesW { get; }

        public int SamplesH { get; }

        #region ICloneable Members
        new public object Clone()
        {
            return MemberwiseClone();
        }
        #endregion
    }

    public enum ImageRepeat
    {
        Repeat,         // repeat image in both x and y directions
        NoRepeat,       // don't repeat
        RepeatX,        // repeat image in x direction
        RepeatY         // repeat image in y direction
    }

    public class PageEllipse : PageItem, ICloneable
    {
        public PageEllipse()
        {
        }

        #region ICloneable Members
        new public object Clone()
        {
            return this.MemberwiseClone();
        }
        #endregion
    }

    public class PageLine : PageItem, ICloneable
    {
        public PageLine()
        {
        }

        public float X2
        {
            get { return W; }
            set { W = value; }
        }

        public float Y2
        {
            get { return H; }
            set { H = value; }
        }
        #region ICloneable Members
        new public object Clone()
        {
            return this.MemberwiseClone();
        }
        #endregion
    }

    public class PageCurve : PageItem, ICloneable
    {
        public PageCurve()
        {
        }

        public PointF[] Points { get; set; }

        public int Offset { get; set; }

        public float Tension { get; set; }

        #region ICloneable Members
        new public object Clone()
        {
            return this.MemberwiseClone();
        }
        #endregion
    }

    public class PagePolygon : PageItem, ICloneable
    {
        public PointF[] Points { get; set; }

        public PagePolygon()
        {
        }
    }

    public class PagePie : PageItem, ICloneable
    {
        public PagePie() { }
        public float StartAngle { get; set; }
        public float SweepAngle { get; set; }

        #region ICloneable Members
        new public object Clone()
        {
            return this.MemberwiseClone();
        }
        #endregion
    }

    public class PageRectangle : PageItem, ICloneable
    {
        public PageRectangle()
        {
        }
        #region ICloneable Members
        new public object Clone()
        {
            return this.MemberwiseClone();
        }
        #endregion
    }

    public class PageText : PageItem, ICloneable
    {
        public PageText(string t)
        {
            Text = t;
            Descent = 0;
            CanGrow = false;
        }

        public PageTextHtml HtmlParent { get; set; } = null;

        public string Text { get; set; }

        /// <summary>
        /// in some cases the Font descent will be recorded; 0 otherwise
        /// </summary>
        public float Descent { get; set; }

        /// <summary>
        /// on drawing disallow clipping
        /// </summary>
        public bool NoClip { get; set; } = false;

        public bool CanGrow { get; set; }

        #region ICloneable Members
        new public object Clone()
        {
            return this.MemberwiseClone();
        }
        #endregion
    }
}