using System;
using System.Xml;
using System.Text;
using appbox.Drawing;
using System.Globalization;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// Style (borders, fonts, background, padding, ...) of a ReportItem.
    ///</summary>
    [Serializable]
    internal class Style : ReportLink
    {
        internal static readonly string DefaultFontFamily = Font.DefaultFontFamilyName;

        #region ====Properties====
        /// <summary>
        /// Color of the border
        /// </summary>
        internal StyleBorderColor BorderColor { get; set; }

        /// <summary>
        /// Style of the border
        /// </summary>
        internal StyleBorderStyle BorderStyle { get; set; }

        /// <summary>
        /// Width of the border
        /// </summary>
        internal StyleBorderWidth BorderWidth { get; set; }

        /// <summary>
        /// (Color) Color of the background
        /// If omitted, the background is transparent
        /// </summary>
        internal Expression BackgroundColor { get; set; }

        /// <summary>
        /// The type of background gradient
        /// </summary>
        internal Expression BackgroundGradientType { get; set; }

        /// <summary>
        /// (Color) End color for the background gradient.
        /// If omitted, there is no gradient.
        /// </summary>
        internal Expression BackgroundGradientEndColor { get; set; }

        /// <summary>
        /// A background image for the report item.
        /// If omitted, there is no background image.
        /// </summary>
        internal StyleBackgroundImage BackgroundImage { get; set; }

        /// <summary>
        /// true if all Style elements are constant
        /// </summary>
        internal bool ConstantStyle { get; private set; }

        /// <summary>
        /// (Enum FontStyle) Font style Default: Normal
        /// </summary>
        internal Expression FontStyle { get; set; }

        /// <summary>
        /// (string)Name of the font family
        /// </summary>
        internal Expression FontFamily { get; set; }

        /// <summary>
        /// (Size) Point size of the font
        /// Default: 10 pt. Min: 1 pt. Max: 200 pt.
        /// </summary>
        internal Expression FontSize { get; set; }

        /// <summary>
        /// (Enum FontWeight) Thickness of the font
        /// </summary>
        internal Expression FontWeight { get; set; }

        /// <summary>
        /// (string) .NET Framework formatting string1
        ///	Note: Locale-dependent currency
        ///	formatting (format code �C�) is based on
        ///	the language setting for the report item
        ///	Locale-dependent date formatting is
        ///	supported and should be based on the
        ///	language property of the ReportItem.
        ///	Default: No formatting.
        /// </summary>
        internal Expression Format { get; set; }

        /// <summary>
        /// (Enum TextDecoration) Special text formatting Default: none
        /// </summary>
        internal Expression TextDecoration { get; set; }

        /// <summary>
        /// (Enum TextAlign) Horizontal alignment of the text Default: General
        /// </summary>
        internal Expression TextAlign { get; set; }

        /// <summary>
        /// (Enum VerticalAlign) Vertical alignment of the text Default: Top
        /// </summary>
        internal Expression VerticalAlign { get; set; }

        /// <summary>
        /// (Color) The foreground color	Default: Black
        /// </summary>
        internal Expression Color { get; set; }

        /// <summary>
        /// (Size)Padding between the left edge of the
        /// report item and its contents1
        /// Default: 0 pt. Max: 1000 pt.
        /// </summary>
        internal Expression PaddingLeft { get; set; }

        /// <summary>
        /// (Size) Padding between the right edge of the
        /// report item and its contents
        /// Default: 0 pt. Max: 1000 pt.
        /// </summary>
        internal Expression PaddingRight { get; set; }

        /// <summary>
        /// (Size) Padding between the top edge of the
        /// report item and its contents
        /// Default: 0 pt. Max: 1000 pt.
        /// </summary>
        internal Expression PaddingTop { get; set; }

        /// <summary>
        /// (Size) Padding between the top edge of the
        ///	report item and its contents
        /// Default: 0 pt. Max: 1000 pt
        /// </summary>
        internal Expression PaddingBottom { get; set; }

        /// <summary>
        /// (Size) Height of a line of text
        /// Default: Report output format determines
        /// line height based on font size
        /// Min: 1 pt. Max: 1000 pt.
        /// </summary>
        internal Expression LineHeight { get; set; }

        /// <summary>
        /// (Enum Direction) Indicates whether text is written left-to-right (default)
        /// or right-to-left.
        /// Does not impact the alignment of text unless using General alignment.
        /// </summary>
        internal Expression Direction { get; set; }

        /// <summary>
        /// (Enum WritingMode) Indicates whether text is written
        /// horizontally or vertically.
        /// </summary>
        internal Expression WritingMode { get; set; }

        /// <summary>
        /// (Language) The primary language of the text.
        /// Default is Report.Language.
        /// </summary>
        internal Expression Language { get; set; }

        /// <summary>
        /// (Enum UnicodeBiDirection) 
        /// Indicates the level of embedding with
        /// respect to the Bi-directional algorithm. Default: normal
        /// </summary>
        internal Expression UnicodeBiDirectional { get; set; }

        /// <summary>
        /// (Enum Calendar)
        ///	Indicates the calendar to use for
        ///	formatting dates. Must be compatible in
        ///	.NET framework with the Language setting.
        /// </summary>
        internal Expression Calendar { get; set; }

        /// <summary>
        /// (Language) The digit format to use as described by its
        /// primary language. Any language is legal.
        /// Default is the Language property.
        /// </summary>
        internal Expression NumeralLanguage { get; set; }

        /// <summary>
        /// (Integer) The variant of the digit format to use.
        /// Currently defined values are:
        /// 1: default, follow Unicode context rules
        /// 2: 0123456789
        /// 3: traditional digits for the script as
        ///     defined in GDI+. Currently supported for:
        ///		ar | bn | bo | fa | gu | hi | kn | kok | lo | mr |
        ///		ms | or | pa | sa | ta | te | th | ur and variants.
        /// 4: ko, ja, zh-CHS, zh-CHT only
        /// 5: ko, ja, zh-CHS, zh-CHT only
        /// 6: ko, ja, zh-CHS, zh-CHT only [Wide
        ///     versions of regular digits]
        /// 7: ko only
        /// </summary>
        internal Expression NumeralVariant { get; set; }
        #endregion

        internal Style(ReportDefn r, ReportLink p, XmlNode xNode) : base(r, p)
        {
            BorderColor = null;
            BorderStyle = null;
            BorderWidth = null;
            BackgroundColor = null;
            BackgroundGradientType = null;
            BackgroundGradientEndColor = null;
            BackgroundImage = null;
            FontStyle = null;
            FontFamily = null;
            FontSize = null;
            FontWeight = null;
            Format = null;
            TextDecoration = null;
            TextAlign = null;
            VerticalAlign = null;
            Color = null;
            PaddingLeft = null;
            PaddingRight = null;
            PaddingTop = null;
            PaddingBottom = null;
            LineHeight = null;
            Direction = null;
            WritingMode = null;
            Language = null;
            UnicodeBiDirectional = null;
            Calendar = null;
            NumeralLanguage = null;
            NumeralVariant = null;

            // Loop thru all the child nodes
            foreach (XmlNode xNodeLoop in xNode.ChildNodes)
            {
                if (xNodeLoop.NodeType != XmlNodeType.Element)
                    continue;
                switch (xNodeLoop.Name)
                {
                    case "BorderColor":
                        BorderColor = new StyleBorderColor(r, this, xNodeLoop);
                        break;
                    case "BorderStyle":
                        BorderStyle = new StyleBorderStyle(r, this, xNodeLoop);
                        break;
                    case "BorderWidth":
                        BorderWidth = new StyleBorderWidth(r, this, xNodeLoop);
                        break;
                    case "BackgroundColor":
                        BackgroundColor = new Expression(r, this, xNodeLoop, ExpressionType.Color);
                        break;
                    case "BackgroundGradientType":
                        BackgroundGradientType = new Expression(r, this, xNodeLoop, ExpressionType.Enum);
                        break;
                    case "BackgroundGradientEndColor":
                        BackgroundGradientEndColor = new Expression(r, this, xNodeLoop, ExpressionType.Color);
                        break;
                    case "BackgroundImage":
                        BackgroundImage = new StyleBackgroundImage(r, this, xNodeLoop);
                        break;
                    case "FontStyle":
                        FontStyle = new Expression(r, this, xNodeLoop, ExpressionType.Enum);
                        break;
                    case "FontFamily":
                        FontFamily = new Expression(r, this, xNodeLoop, ExpressionType.String);
                        break;
                    case "FontSize":
                        FontSize = new Expression(r, this, xNodeLoop, ExpressionType.ReportUnit);
                        break;
                    case "FontWeight":
                        FontWeight = new Expression(r, this, xNodeLoop, ExpressionType.Enum);
                        break;
                    case "Format":
                        Format = new Expression(r, this, xNodeLoop, ExpressionType.String);
                        break;
                    case "TextDecoration":
                        TextDecoration = new Expression(r, this, xNodeLoop, ExpressionType.Enum);
                        break;
                    case "TextAlign":
                        TextAlign = new Expression(r, this, xNodeLoop, ExpressionType.Enum);
                        break;
                    case "VerticalAlign":
                        VerticalAlign = new Expression(r, this, xNodeLoop, ExpressionType.Enum);
                        break;
                    case "Color":
                        Color = new Expression(r, this, xNodeLoop, ExpressionType.Color);
                        break;
                    case "PaddingLeft":
                        PaddingLeft = new Expression(r, this, xNodeLoop, ExpressionType.ReportUnit);
                        break;
                    case "PaddingRight":
                        PaddingRight = new Expression(r, this, xNodeLoop, ExpressionType.ReportUnit);
                        break;
                    case "PaddingTop":
                        PaddingTop = new Expression(r, this, xNodeLoop, ExpressionType.ReportUnit);
                        break;
                    case "PaddingBottom":
                        PaddingBottom = new Expression(r, this, xNodeLoop, ExpressionType.ReportUnit);
                        break;
                    case "LineHeight":
                        LineHeight = new Expression(r, this, xNodeLoop, ExpressionType.ReportUnit);
                        break;
                    case "Direction":
                        Direction = new Expression(r, this, xNodeLoop, ExpressionType.Enum);
                        break;
                    case "WritingMode":
                        WritingMode = new Expression(r, this, xNodeLoop, ExpressionType.Enum);
                        break;
                    case "Language":
                        Language = new Expression(r, this, xNodeLoop, ExpressionType.Language);
                        break;
                    case "UnicodeBiDirectional":
                        UnicodeBiDirectional = new Expression(r, this, xNodeLoop, ExpressionType.Enum);
                        break;
                    case "Calendar":
                        Calendar = new Expression(r, this, xNodeLoop, ExpressionType.Enum);
                        break;
                    case "NumeralLanguage":
                        NumeralLanguage = new Expression(r, this, xNodeLoop, ExpressionType.Language);
                        break;
                    case "NumeralVariant":
                        NumeralVariant = new Expression(r, this, xNodeLoop, ExpressionType.Integer);
                        break;
                    default:
                        // don't know this element - log it
                        OwnerReport.rl.LogError(4, "Unknown Style element '" + xNodeLoop.Name + "' ignored.");
                        break;
                }
            }
        }

        // Handle parsing of function in final pass
        override internal void FinalPass()
        {
            if (BorderColor != null)
                BorderColor.FinalPass();
            if (BorderStyle != null)
                BorderStyle.FinalPass();
            if (BorderWidth != null)
                BorderWidth.FinalPass();
            if (BackgroundColor != null)
                BackgroundColor.FinalPass();
            if (BackgroundGradientType != null)
                BackgroundGradientType.FinalPass();
            if (BackgroundGradientEndColor != null)
                BackgroundGradientEndColor.FinalPass();
            if (BackgroundImage != null)
                BackgroundImage.FinalPass();
            if (FontStyle != null)
                FontStyle.FinalPass();
            if (FontFamily != null)
                FontFamily.FinalPass();
            if (FontSize != null)
                FontSize.FinalPass();
            if (FontWeight != null)
                FontWeight.FinalPass();
            if (Format != null)
                Format.FinalPass();
            if (TextDecoration != null)
                TextDecoration.FinalPass();
            if (TextAlign != null)
                TextAlign.FinalPass();
            if (VerticalAlign != null)
                VerticalAlign.FinalPass();
            if (Color != null)
                Color.FinalPass();
            if (PaddingLeft != null)
                PaddingLeft.FinalPass();
            if (PaddingRight != null)
                PaddingRight.FinalPass();
            if (PaddingTop != null)
                PaddingTop.FinalPass();
            if (PaddingBottom != null)
                PaddingBottom.FinalPass();
            if (LineHeight != null)
                LineHeight.FinalPass();
            if (Direction != null)
                Direction.FinalPass();
            if (WritingMode != null)
                WritingMode.FinalPass();
            if (Language != null)
                Language.FinalPass();
            if (UnicodeBiDirectional != null)
                UnicodeBiDirectional.FinalPass();
            if (Calendar != null)
                Calendar.FinalPass();
            if (NumeralLanguage != null)
                NumeralLanguage.FinalPass();
            if (NumeralVariant != null)
                NumeralVariant.FinalPass();

            ConstantStyle = IsConstant();
            return;
        }

        internal void DrawBackground(Report rpt, Graphics g, Row r, Drawing.Rectangle rect)
        {
            LinearGradientBrush linGrBrush = null;

            if (BackgroundGradientType != null &&
                BackgroundGradientEndColor != null &&
                BackgroundColor != null)
            {
                string bgt = BackgroundGradientType.EvaluateString(rpt, r);
                string bgc = BackgroundColor.EvaluateString(rpt, r);

                Color c = XmlUtil.ColorFromHtml(bgc, Drawing.Color.White, rpt);

                string bgec = BackgroundGradientEndColor.EvaluateString(rpt, r);
                Color ec = XmlUtil.ColorFromHtml(bgec, Drawing.Color.White, rpt);

                switch (bgt)
                {
                    case "LeftRight":
                        linGrBrush = new LinearGradientBrush(rect, c, ec, LinearGradientMode.Horizontal);
                        break;
                    case "TopBottom":
                        linGrBrush = new LinearGradientBrush(rect, c, ec, LinearGradientMode.Vertical);
                        break;
                    case "Center":  //??
                        linGrBrush = new LinearGradientBrush(rect, c, ec, LinearGradientMode.Horizontal);
                        break;
                    case "DiagonalLeft":
                        linGrBrush = new LinearGradientBrush(rect, c, ec, LinearGradientMode.ForwardDiagonal);
                        break;
                    case "DiagonalRight":
                        linGrBrush = new LinearGradientBrush(rect, c, ec, LinearGradientMode.BackwardDiagonal);
                        break;
                    case "HorizontalCenter":
                        linGrBrush = new LinearGradientBrush(rect, c, ec, LinearGradientMode.Horizontal);
                        break;
                    case "VerticalCenter":
                        linGrBrush = new LinearGradientBrush(rect, c, ec, LinearGradientMode.Vertical);
                        break;
                    case "None":
                    default:
                        break;
                }
            }

            if (linGrBrush != null)
            {
                g.FillRectangle(linGrBrush, rect);
                linGrBrush.Dispose();
            }
            else
            {
                if (this.BackgroundColor != null)
                {
                    string bgc = this.BackgroundColor.EvaluateString(rpt, r);
                    Color c = XmlUtil.ColorFromHtml(bgc, Drawing.Color.White, rpt);

                    SolidBrush sb = new SolidBrush(c);
                    g.FillRectangle(sb, rect);
                    sb.Dispose();
                }
            }
            return;
        }

        internal void DrawBackgroundCircle(Report rpt, Graphics g, Row r, Drawing.Rectangle rect)
        {
            // Don't use the gradient in this case (since it won't match) the rest of the 
            //    background.  (Routine is only used by ChartPie in the doughnut case.)
            if (BackgroundColor != null)
            {
                string bgc = BackgroundColor.EvaluateString(rpt, r);
                Color c = XmlUtil.ColorFromHtml(bgc, Drawing.Color.White, rpt);

                SolidBrush sb = new SolidBrush(c);
                g.FillEllipse(sb, rect);
                g.DrawEllipse(Drawing.Color.Black, 1, rect);
                sb.Dispose();
            }
            return;
        }

        // Draw a border using the current style
        internal void DrawBorder(Report rpt, Graphics g, Row r, Drawing.Rectangle rect)
        {
            if (BorderStyle == null)
                return;

            StyleBorderStyle bs = this.BorderStyle;

            // Create points for each part of rectangular border
            Point tl = new Point(rect.Left, rect.Top);
            Point tr = new Point(rect.Right, rect.Top);
            Point bl = new Point(rect.Left, rect.Bottom);
            Point br = new Point(rect.Right, rect.Bottom);
            // Determine characteristics for each line to be drawn
            BorderStyleEnum topBS, bottomBS, leftBS, rightBS;
            topBS = bottomBS = leftBS = rightBS = BorderStyleEnum.None;
            string v;           // temporary work value
            if (BorderStyle != null)
            {
                if (BorderStyle.Default != null)
                {
                    v = BorderStyle.Default.EvaluateString(rpt, r);
                    topBS = bottomBS = leftBS = rightBS = StyleBorderStyle.GetBorderStyle(v, BorderStyleEnum.None);
                }
                if (BorderStyle.Top != null)
                {
                    v = BorderStyle.Top.EvaluateString(rpt, r);
                    topBS = StyleBorderStyle.GetBorderStyle(v, topBS);
                }
                if (BorderStyle.Bottom != null)
                {
                    v = BorderStyle.Bottom.EvaluateString(rpt, r);
                    bottomBS = StyleBorderStyle.GetBorderStyle(v, bottomBS);
                }
                if (BorderStyle.Left != null)
                {
                    v = BorderStyle.Left.EvaluateString(rpt, r);
                    leftBS = StyleBorderStyle.GetBorderStyle(v, leftBS);
                }
                if (BorderStyle.Right != null)
                {
                    v = BorderStyle.Right.EvaluateString(rpt, r);
                    rightBS = StyleBorderStyle.GetBorderStyle(v, rightBS);
                }
            }

            Color topColor, bottomColor, leftColor, rightColor;
            topColor = bottomColor = leftColor = rightColor = Drawing.Color.Black;
            if (BorderColor != null)
            {
                if (BorderColor.Default != null)
                {
                    v = BorderColor.Default.EvaluateString(rpt, r);
                    topColor = bottomColor = leftColor = rightColor =
                        XmlUtil.ColorFromHtml(v, Drawing.Color.Black, rpt);
                }
                if (BorderColor.Top != null)
                {
                    v = BorderColor.Top.EvaluateString(rpt, r);
                    topColor = XmlUtil.ColorFromHtml(v, Drawing.Color.Black, rpt);
                }
                if (BorderColor.Bottom != null)
                {
                    v = BorderColor.Bottom.EvaluateString(rpt, r);
                    bottomColor = XmlUtil.ColorFromHtml(v, Drawing.Color.Black, rpt);
                }
                if (BorderColor.Left != null)
                {
                    v = BorderColor.Left.EvaluateString(rpt, r);
                    leftColor = XmlUtil.ColorFromHtml(v, Drawing.Color.Black, rpt);
                }
                if (BorderColor.Right != null)
                {
                    v = BorderColor.Right.EvaluateString(rpt, r);
                    rightColor = XmlUtil.ColorFromHtml(v, Drawing.Color.Black, rpt);
                }
            }

            int topWidth, bottomWidth, leftWidth, rightWidth;
            topWidth = bottomWidth = leftWidth = rightWidth = 1;
            if (BorderWidth != null)
            {
                if (BorderWidth.Default != null)
                {
                    topWidth = bottomWidth = leftWidth = rightWidth =
                        new RSize(OwnerReport, BorderWidth.Default.EvaluateString(rpt, r)).PixelsX;
                }
                if (BorderWidth.Top != null)
                {
                    topWidth = new RSize(OwnerReport, BorderWidth.Top.EvaluateString(rpt, r)).PixelsX;
                }
                if (BorderWidth.Bottom != null)
                {
                    bottomWidth = new RSize(OwnerReport, BorderWidth.Bottom.EvaluateString(rpt, r)).PixelsX;
                }
                if (BorderWidth.Left != null)
                {
                    leftWidth = new RSize(OwnerReport, BorderWidth.Left.EvaluateString(rpt, r)).PixelsY;
                }
                if (BorderWidth.Right != null)
                {
                    rightWidth = new RSize(OwnerReport, BorderWidth.Right.EvaluateString(rpt, r)).PixelsY;
                }
            }

            Pen p = null;
            try
            {
                // top line
                if (topBS != BorderStyleEnum.None)
                {
                    p = new Pen(topColor, topWidth);
                    DrawBorderDashStyle(p, topBS);
                    g.DrawLine(topColor, topWidth, tl.X, tl.Y, tr.X, tr.Y);
                    p.Dispose(); p = null;
                }
                // right line
                if (rightBS != BorderStyleEnum.None)
                {
                    p = new Pen(rightColor, rightWidth);
                    DrawBorderDashStyle(p, rightBS);
                    g.DrawLine(rightColor, rightWidth, tr.X, tr.Y, br.X, br.Y);
                    p.Dispose(); p = null;
                }
                // bottom line
                if (bottomBS != BorderStyleEnum.None)
                {
                    p = new Pen(bottomColor, bottomWidth);
                    DrawBorderDashStyle(p, bottomBS);
                    g.DrawLine(bottomColor, bottomWidth, br.X, br.Y, bl.X, bl.Y);
                    p.Dispose(); p = null;
                }
                // left line
                if (leftBS != BorderStyleEnum.None)
                {
                    p = new Pen(leftColor, leftWidth);
                    DrawBorderDashStyle(p, leftBS);
                    g.DrawLine(leftColor, leftWidth, bl.X, bl.Y, tl.X, tl.Y);
                    p.Dispose(); p = null;
                }
            }
            finally
            {
                if (p != null)
                    p.Dispose();
            }
        }

        private void DrawBorderDashStyle(Pen p, BorderStyleEnum bs)
        {
            switch (bs)
            {
                case BorderStyleEnum.Dashed:
                    p.DashStyle = DashStyle.Dash;
                    break;
                case BorderStyleEnum.Dotted:
                    p.DashStyle = DashStyle.Dot;
                    break;
                case BorderStyleEnum.Double:
                    p.DashStyle = DashStyle.Solid;      // TODO:	really need to create custom?
                    break;
                case BorderStyleEnum.Groove:
                    p.DashStyle = DashStyle.Solid;      // TODO:
                    break;
                case BorderStyleEnum.Inset:
                    p.DashStyle = DashStyle.Solid;      // TODO:
                    break;
                case BorderStyleEnum.None:
                    p.DashStyle = DashStyle.Solid;      // only happens for lines
                    break;
                case BorderStyleEnum.Outset:
                    p.DashStyle = DashStyle.Solid;      // TODO:
                    break;
                case BorderStyleEnum.Ridge:
                    p.DashStyle = DashStyle.Solid;      // TODO:
                    break;
                case BorderStyleEnum.Solid:
                    p.DashStyle = DashStyle.Solid;
                    break;
                case BorderStyleEnum.WindowInset:
                    p.DashStyle = DashStyle.Solid;      // TODO:
                    break;
                default:
                    p.DashStyle = DashStyle.Solid;      // really an error
                    break;
            }
            return;
        }

        // Draw a line into the specified graphics object using the current style
        internal void DrawStyleLine(Report rpt, Graphics g, Row r, Point s, Point e)
        {
            Pen p = null;
            try
            {
                int width;
                Drawing.Color color;
                BorderStyleEnum bs;

                // Border Width default is used for the line width
                if (BorderWidth != null && BorderWidth.Default != null)
                    width = new RSize(OwnerReport, BorderWidth.Default.EvaluateString(rpt, r)).PixelsX;
                else
                    width = 1;

                // Border Color default is used for the line color
                if (BorderColor != null && BorderColor.Default != null)
                {
                    string v = BorderColor.Default.EvaluateString(rpt, r);
                    color = XmlUtil.ColorFromHtml(v, Drawing.Color.Black, rpt);
                }
                else
                    color = Drawing.Color.Black;

                //
                if (BorderStyle != null && BorderStyle.Default != null)
                {
                    string v = BorderStyle.Default.EvaluateString(rpt, r);
                    bs = StyleBorderStyle.GetBorderStyle(v, BorderStyleEnum.None);
                }
                else
                    bs = BorderStyleEnum.Solid;

                p = new Pen(color, width);
                DrawBorderDashStyle(p, bs);
                g.DrawLine(color, width, s.X, s.Y, e.X, e.Y);
            }
            finally
            {
                if (p != null)
                    p.Dispose();
            }
        }

        // Draw a string into the specified graphics object using the current style
        //  information
        internal void DrawString(Report rpt, Graphics g, object o, TypeCode tc, Row r, Drawing.Rectangle rect)
        {
            Font drawFont = null;               // Font we'll draw with
            Brush drawBrush = null;         // Brush we'll draw with
            StringFormat drawFormat = null; // StringFormat we'll draw with
            string s;                       // the string to draw

            try         // Want to make sure we dispose of the font and brush (no matter what)
            {
                s = Style.GetFormatedString(rpt, this, r, o, tc);

                drawFont = GetFont(rpt, r);

                drawBrush = GetBrush(rpt, r);

                drawFormat = GetStringFormat(rpt, r);

                // Draw string
                drawFormat.FormatFlags |= StringFormatFlags.NoWrap;
                g.DrawString(s, drawFont, drawBrush, rect, drawFormat);
            }
            finally
            {
                if (drawFont != null)
                    drawFont.Dispose();
                if (drawBrush != null)
                    drawBrush.Dispose();
                if (drawFormat != null)
                    drawFormat.Dispose();
            }
        }

        static internal void DrawStringDefaults(Graphics g, object o, Drawing.Rectangle rect)
        {
            Font drawFont = null;
            SolidBrush drawBrush = null;
            StringFormat drawFormat = null;
            try
            {
                // Just use defaults to Create font and brush.
                drawFont = new Font(DefaultFontFamily, 10);
                drawBrush = new SolidBrush(Drawing.Color.Black);
                // Set format of string.
                drawFormat = new StringFormat();
                drawFormat.Alignment = StringAlignment.Center;

                // 06122007AJM Fixed so that long names are written vertically
                // need to add w to make slightly bigger
                SizeF len = g.MeasureString(o.ToString() + "w", drawFont);
                if (len.Width > rect.Width)
                {
                    drawFormat.FormatFlags = StringFormatFlags.DirectionVertical;
                    rect = new Drawing.Rectangle(rect.X, rect.Y, rect.Width, (int)len.Width);
                    drawFormat.Alignment = StringAlignment.Near;
                }

                // Draw string to image
                g.DrawString(o.ToString(), drawFont, drawBrush, rect, drawFormat);
            }
            finally
            {
                if (drawFont != null)
                    drawFont.Dispose();
                if (drawBrush != null)
                    drawBrush.Dispose();
                if (drawFormat != null)
                    drawFormat.Dispose();
            }

        }

        // Calc size of a string with the specified graphics object using the current style
        //  information
        internal Size MeasureString(Report rpt, Graphics g, object o, TypeCode tc, Row r, int maxWidth)
        {
            Font drawFont = null;               // Font we'll draw with
            StringFormat drawFormat = null; // StringFormat we'll draw with
            string s;                       // the string to draw

            Size size = Size.Empty;
            try         // Want to make sure we dispose of the font and brush (no matter what)
            {
                s = Style.GetFormatedString(rpt, this, r, o, tc);

                drawFont = GetFont(rpt, r);

                drawFormat = GetStringFormat(rpt, r);

                // Measure string
                if (maxWidth == int.MaxValue)
                    drawFormat.FormatFlags |= StringFormatFlags.NoWrap;

                // 06122007AJM need to add w to make slightly bigger
                SizeF ms = g.MeasureString(s + "w", drawFont, maxWidth, drawFormat);
                size = new Size((int)Math.Ceiling(ms.Width),
                    (int)Math.Ceiling(ms.Height));
            }
            finally
            {
                if (drawFont != null)
                    drawFont.Dispose();
                if (drawFormat != null)
                    drawFormat.Dispose();
            }

            return size;
        }

        // Measure a string using the defaults for a Style font
        static internal Size MeasureStringDefaults(Report rpt, Graphics g, object o, TypeCode tc, Row r, int maxWidth)
        {
            Font drawFont = null;               // Font we'll draw with
            StringFormat drawFormat = null; // StringFormat we'll draw with
            string s;                       // the string to draw

            Size size = Size.Empty;
            try         // Want to make sure we dispose of the font and brush (no matter what)
            {
                s = Style.GetFormatedString(rpt, null, r, o, tc);

                drawFont = new Font(DefaultFontFamily, 10);
                drawFormat = new StringFormat();
                drawFormat.Alignment = StringAlignment.Near;

                // Measure string
                if (maxWidth == int.MaxValue)
                    drawFormat.FormatFlags |= StringFormatFlags.NoWrap;
                // 06122007AJM need to add w to make slightly bigger
                SizeF ms = g.MeasureString(s + "w", drawFont, maxWidth, drawFormat);
                size = new Size((int)Math.Ceiling(ms.Width),
                    (int)Math.Ceiling(ms.Height));
            }
            finally
            {
                if (drawFont != null)
                    drawFont.Dispose();
                if (drawFormat != null)
                    drawFormat.Dispose();
            }

            return size;
        }

        internal Brush GetBrush(Report rpt, Row r)
        {
            Brush drawBrush;
            // Get the brush information
            if (Color != null)
            {
                string c = Color.EvaluateString(rpt, r);
                Color color = XmlUtil.ColorFromHtml(c, Drawing.Color.Black, rpt);
                drawBrush = new SolidBrush(color);
            }
            else
                drawBrush = new SolidBrush(Drawing.Color.Black);
            return drawBrush;
        }

        internal Font GetFont(Report rpt, Row r)
        {
            // Get the font information
            // FAMILY
            string ff;
            if (FontFamily != null)
                ff = FontFamily.EvaluateString(rpt, r);
            else
                ff = DefaultFontFamily;

            // STYLE
            FontStyle fs = 0;
            if (FontStyle != null)
            {
                string fStyle = FontStyle.EvaluateString(rpt, r);
                if (fStyle == "Italic")
                    fs |= Drawing.FontStyle.Italic;
            }
            if (TextDecoration != null)
            {
                string td = TextDecoration.EvaluateString(rpt, r);
                switch (td)
                {
                    case "Underline":
                        fs |= Drawing.FontStyle.Underline;
                        break;
                    case "Overline":    // Don't support this
                        break;
                    case "LineThrough":
                        fs |= Drawing.FontStyle.Strikeout;
                        break;
                    case "None":
                    default:
                        break;
                }
            }

            // WEIGHT
            if (FontWeight != null)
            {
                string weight = FontWeight.EvaluateString(rpt, r);
                switch (weight.ToLower())
                {
                    case "bold":
                    case "bolder":
                    case "500":
                    case "600":
                    case "700":
                    case "800":
                    case "900":
                        fs |= Drawing.FontStyle.Bold;
                        break;
                    // Nothing to do otherwise since we don't have finer gradations
                    case "normal":
                    case "lighter":
                    case "100":
                    case "200":
                    case "300":
                    case "400":
                    default:
                        break;
                }
            }

            // SIZE
            float size;         // Value is in points
            if (FontSize != null)
            {
                string lsize = FontSize.EvaluateString(rpt, r);
                RSize rs = new RSize(OwnerReport, lsize);
                size = rs.Points;
            }
            else
                size = 10;

            FontFamily fFamily = StyleInfo.GetFontFamily(ff);
            return new Font(fFamily.Name, size, fs);
        }

        internal StringFormat GetStringFormat(Report rpt, Row r)
        {
            return GetStringFormat(rpt, r, StringAlignment.Center);
        }

        internal StringFormat GetStringFormat(Report rpt, Row r, StringAlignment defTextAlign)
        {
            // Set format of string.
            StringFormat drawFormat = new StringFormat();

            if (this.Direction != null)
            {
                string dir = this.Direction.EvaluateString(rpt, r);
                if (dir == "RTL")
                    drawFormat.FormatFlags |= StringFormatFlags.DirectionRightToLeft;
            }
            if (this.WritingMode != null)
            {
                string wm = this.WritingMode.EvaluateString(rpt, r);
                if (wm == "tb-rl")
                    drawFormat.FormatFlags |= StringFormatFlags.DirectionVertical;
            }

            if (this.TextAlign != null)
            {
                string ta = this.TextAlign.EvaluateString(rpt, r);
                switch (ta.ToLower())
                {
                    case "left":
                        drawFormat.Alignment = StringAlignment.Near;
                        break;
                    case "right":
                        drawFormat.Alignment = StringAlignment.Far;
                        break;
                    case "general":
                        drawFormat.Alignment = defTextAlign;
                        break;
                    case "center":
                    default:
                        drawFormat.Alignment = StringAlignment.Center;
                        break;
                }
            }
            else
                drawFormat.Alignment = defTextAlign;

            if (this.VerticalAlign != null)
            {
                string va = this.VerticalAlign.EvaluateString(rpt, r);
                switch (va.ToLower())
                {
                    case "top":
                    default:
                        drawFormat.LineAlignment = StringAlignment.Near;
                        break;
                    case "bottom":
                        drawFormat.LineAlignment = StringAlignment.Far;
                        break;
                    case "middle":
                        drawFormat.LineAlignment = StringAlignment.Center;
                        break;
                }
            }
            else
                drawFormat.LineAlignment = StringAlignment.Near;

            drawFormat.Trimming = StringTrimming.None;
            return drawFormat;
        }

        // Generate a CSS string from the specified styles
        internal string GetCSS(Report rpt, Row row, bool bDefaults)
        {
            WorkClass wc = GetWC(rpt);
            if (wc != null && wc.CssStyle != null)  // When CssStyle is available; style is a constant
                return wc.CssStyle;                 //   The first time called bDefaults will affect all subsequant calls

            StringBuilder sb = new StringBuilder();

            if (this.Parent is Table || this.Parent is Matrix)
                sb.Append("border-collapse:collapse;"); // collapse the borders

            if (BorderColor != null)
                sb.Append(BorderColor.GetCSS(rpt, row, bDefaults));
            else if (bDefaults)
                sb.Append(StyleBorderColor.GetCSSDefaults());

            if (BorderStyle != null)
                sb.Append(BorderStyle.GetCSS(rpt, row, bDefaults));
            else if (bDefaults)
                sb.Append(StyleBorderStyle.GetCSSDefaults());

            if (BorderWidth != null)
                sb.Append(BorderWidth.GetCSS(rpt, row, bDefaults));
            else if (bDefaults)
                sb.Append(StyleBorderWidth.GetCSSDefaults());

            if (BackgroundColor != null)
                sb.AppendFormat(NumberFormatInfo.InvariantInfo, "background-color:{0};", BackgroundColor.EvaluateString(rpt, row));
            else if (bDefaults)
                sb.Append("background-color:transparent;");

            if (BackgroundImage != null)
                sb.Append(BackgroundImage.GetCSS(rpt, row, bDefaults));
            else if (bDefaults)
                sb.Append(StyleBackgroundImage.GetCSSDefaults());

            if (FontStyle != null)
                sb.AppendFormat(NumberFormatInfo.InvariantInfo, "font-style:{0};", FontStyle.EvaluateString(rpt, row));
            else if (bDefaults)
                sb.Append("font-style:normal;");

            if (FontFamily != null)
                sb.AppendFormat(NumberFormatInfo.InvariantInfo, "font-family:{0};", FontFamily.EvaluateString(rpt, row));
            else if (bDefaults)
                sb.Append($"font-family:{DefaultFontFamily};");

            if (FontSize != null)
                sb.AppendFormat(NumberFormatInfo.InvariantInfo, "font-size:{0};", FontSize.EvaluateString(rpt, row));
            else if (bDefaults)
                sb.Append("font-size:10pt;");

            if (FontWeight != null)
                sb.AppendFormat(NumberFormatInfo.InvariantInfo, "font-weight:{0};", FontWeight.EvaluateString(rpt, row));
            else if (bDefaults)
                sb.Append("font-weight:normal;");

            if (TextDecoration != null)
                sb.AppendFormat(NumberFormatInfo.InvariantInfo, "text-decoration:{0};", TextDecoration.EvaluateString(rpt, row));
            else if (bDefaults)
                sb.Append("text-decoration:none;");

            if (TextAlign != null)
                sb.AppendFormat(NumberFormatInfo.InvariantInfo, "text-align:{0};", TextAlign.EvaluateString(rpt, row));
            else if (bDefaults)
                sb.Append("");  // no CSS default for text align

            if (VerticalAlign != null)
                sb.AppendFormat(NumberFormatInfo.InvariantInfo, "vertical-align:{0};", VerticalAlign.EvaluateString(rpt, row));
            else if (bDefaults)
                sb.Append("vertical-align:top;");

            if (Color != null)
                sb.AppendFormat(NumberFormatInfo.InvariantInfo, "color:{0};", Color.EvaluateString(rpt, row));
            else if (bDefaults)
                sb.Append("color:black;");

            if (PaddingLeft != null)
                sb.AppendFormat(NumberFormatInfo.InvariantInfo, "padding-left:{0};", PaddingLeft.EvaluateString(rpt, row));
            else if (bDefaults)
                sb.Append("padding-left:0pt;");

            if (PaddingRight != null)
                sb.AppendFormat(NumberFormatInfo.InvariantInfo, "padding-right:{0};", PaddingRight.EvaluateString(rpt, row));
            else if (bDefaults)
                sb.Append("padding-right:0pt;");

            if (PaddingTop != null)
                sb.AppendFormat(NumberFormatInfo.InvariantInfo, "padding-top:{0};", PaddingTop.EvaluateString(rpt, row));
            else if (bDefaults)
                sb.Append("padding-top:0pt;");

            if (PaddingBottom != null)
                sb.AppendFormat(NumberFormatInfo.InvariantInfo, "padding-bottom:{0};", PaddingBottom.EvaluateString(rpt, row));
            else if (bDefaults)
                sb.Append("padding-bottom:0pt;");

            if (LineHeight != null)
                sb.AppendFormat(NumberFormatInfo.InvariantInfo, "line-height:{0};", LineHeight.EvaluateString(rpt, row));
            else if (bDefaults)
                sb.Append("line-height:normal;");

            if (this.ConstantStyle)        // We'll only do this work once
            {                               //   when all are constant
                wc.CssStyle = sb.ToString();
                return wc.CssStyle;
            }

            return sb.ToString();
        }

        // Generate an evaluated version of all the style parameters; used for page processing
        internal StyleInfo GetStyleInfo(Report rpt, Row r)
        {
            WorkClass wc = GetWC(rpt);
            if (wc != null && wc.StyleInfo != null)     // When StyleInfo is available; style is a constant
            {
                return (StyleInfo)wc.StyleInfo.Clone(); // clone it because others can modify it after this		
            }

            StyleInfo si = new StyleInfo();

            if (this.BorderColor != null)
            {
                StyleBorderColor bc = this.BorderColor;
                si.BColorLeft = bc.EvalLeft(rpt, r);
                si.BColorRight = bc.EvalRight(rpt, r);
                si.BColorTop = bc.EvalTop(rpt, r);
                si.BColorBottom = bc.EvalBottom(rpt, r);
            }

            if (BorderStyle != null)
            {
                StyleBorderStyle bs = BorderStyle;
                si.BStyleLeft = bs.EvalLeft(rpt, r);
                si.BStyleRight = bs.EvalRight(rpt, r);
                si.BStyleTop = bs.EvalTop(rpt, r);
                si.BStyleBottom = bs.EvalBottom(rpt, r);
            }

            if (BorderWidth != null)
            {
                StyleBorderWidth bw = BorderWidth;
                si.BWidthLeft = bw.EvalLeft(rpt, r);
                si.BWidthRight = bw.EvalRight(rpt, r);
                si.BWidthTop = bw.EvalTop(rpt, r);
                si.BWidthBottom = bw.EvalBottom(rpt, r);
            }

            si.BackgroundColor = EvalBackgroundColor(rpt, r);
            // When background color not specified; and reportitem part of table
            //   use the tables background color
            if (si.BackgroundColor == Drawing.Color.Empty)
            {
                ReportItem ri = Parent as ReportItem;
                if (ri != null)
                {
                    if (ri.TC != null)
                    {
                        Table t = ri.TC.OwnerTable;
                        if (t.Style != null)
                            si.BackgroundColor = t.Style.EvalBackgroundColor(rpt, r);
                    }
                }
            }
            si.BackgroundGradientType = EvalBackgroundGradientType(rpt, r);
            si.BackgroundGradientEndColor = this.EvalBackgroundGradientEndColor(rpt, r);
            if (BackgroundImage != null)
            {
                si.BackgroundImage = BackgroundImage.GetPageImage(rpt, r);
            }
            else
                si.BackgroundImage = null;

            si.FontStyle = this.EvalFontStyle(rpt, r);
            si.FontFamily = this.EvalFontFamily(rpt, r);
            si.FontSize = this.EvalFontSize(rpt, r);
            si.FontWeight = this.EvalFontWeight(rpt, r);
            si._Format = this.EvalFormat(rpt, r);           //(string) .NET Framework formatting string1
            si.TextDecoration = this.EvalTextDecoration(rpt, r);
            si.TextAlign = this.EvalTextAlign(rpt, r);
            si.VerticalAlign = this.EvalVerticalAlign(rpt, r);
            si.Color = this.EvalColor(rpt, r);
            si.PaddingLeft = this.EvalPaddingLeft(rpt, r);
            si.PaddingRight = this.EvalPaddingRight(rpt, r);
            si.PaddingTop = this.EvalPaddingTop(rpt, r);
            si.PaddingBottom = this.EvalPaddingBottom(rpt, r);
            si.LineHeight = this.EvalLineHeight(rpt, r);
            si.Direction = this.EvalDirection(rpt, r);
            si.WritingMode = this.EvalWritingMode(rpt, r);
            si.Language = this.EvalLanguage(rpt, r);
            si.UnicodeBiDirectional = this.EvalUnicodeBiDirectional(rpt, r);
            si.Calendar = this.EvalCalendar(rpt, r);
            si.NumeralLanguage = this.EvalNumeralLanguage(rpt, r);
            si.NumeralVariant = this.EvalNumeralVariant(rpt, r);

            if (this.ConstantStyle)        // We'll only do this work once
            {
                wc.StyleInfo = si;          //   when all are constant
                si = (StyleInfo)wc.StyleInfo.Clone();
            }

            return si;
        }

        // Format a string; passed a style but style may be null;
        static internal string GetFormatedString(Report rpt, Style s, Row row, object o, TypeCode tc)
        {
            string t = null;
            if (o == null)
                return "";

            string format = null;
            try
            {
                if (s != null && s.Format != null)
                {
                    format = s.Format.EvaluateString(rpt, row);
                    if (format != null && format.Length > 0)
                    {
                        switch (tc)
                        {
                            case TypeCode.DateTime:
                                t = ((DateTime)o).ToString(format);
                                break;
                            case TypeCode.Int16:
                                t = ((short)o).ToString(format);
                                break;
                            case TypeCode.UInt16:
                                t = ((ushort)o).ToString(format);
                                break;
                            case TypeCode.Int32:
                                t = ((int)o).ToString(format);
                                break;
                            case TypeCode.UInt32:
                                t = ((uint)o).ToString(format);
                                break;
                            case TypeCode.Int64:
                                t = ((long)o).ToString(format);
                                break;
                            case TypeCode.UInt64:
                                t = ((ulong)o).ToString(format);
                                break;
                            case TypeCode.String:
                                t = (string)o;
                                break;
                            case TypeCode.Decimal:
                                t = ((decimal)o).ToString(format);
                                break;
                            case TypeCode.Single:
                                t = ((float)o).ToString(format);
                                break;
                            case TypeCode.Double:
                                t = ((double)o).ToString(format);
                                break;
                            default:
                                var formatedMethod = o.GetType().GetMethod("ToString", new Type[] { typeof(string) });
                                if (formatedMethod != null)
                                    t = (string)formatedMethod.Invoke(o, new object[] { format });
                                else
                                    t = o.ToString();
                                break;
                        }
                    }
                    else
                        t = o.ToString();       // No format provided
                }
                else
                {   // No style provided
                    t = o.ToString();
                }
            }
            catch (Exception ex)
            {
                rpt.rl.LogError(1, string.Format("Value:{0} Format:{1} exception: {2}", o, format,
                    ex.InnerException != null ? ex.InnerException.Message : ex.Message));
                t = o.ToString();       // probably type mismatch from expectation
            }
            return t;
        }

        private bool IsConstant()
        {
            bool rc = true;

            if (BorderColor != null)
                rc = BorderColor.IsConstant();

            if (!rc)
                return false;

            if (BorderStyle != null)
                rc = BorderStyle.IsConstant();

            if (!rc)
                return false;

            if (BorderWidth != null)
                rc = BorderWidth.IsConstant();

            if (!rc)
                return false;

            if (BackgroundColor != null)
                rc = BackgroundColor.IsConstant();

            if (!rc)
                return false;

            if (BackgroundImage != null)
                rc = BackgroundImage.IsConstant();

            if (!rc)
                return false;

            if (FontStyle != null)
                rc = FontStyle.IsConstant();

            if (!rc)
                return false;

            if (FontFamily != null)
                rc = FontFamily.IsConstant();

            if (!rc)
                return false;

            if (FontSize != null)
                rc = FontSize.IsConstant();

            if (!rc)
                return false;

            if (FontWeight != null)
                rc = FontWeight.IsConstant();

            if (!rc)
                return false;

            if (TextDecoration != null)
                rc = TextDecoration.IsConstant();

            if (!rc)
                return false;

            if (TextAlign != null)
                rc = TextAlign.IsConstant();

            if (!rc)
                return false;

            if (VerticalAlign != null)
                rc = VerticalAlign.IsConstant();

            if (!rc)
                return false;

            if (Color != null)
                rc = Color.IsConstant();

            if (!rc)
                return false;

            if (PaddingLeft != null)
                rc = PaddingLeft.IsConstant();

            if (!rc)
                return false;

            if (PaddingRight != null)
                rc = PaddingRight.IsConstant();

            if (!rc)
                return false;

            if (PaddingTop != null)
                rc = PaddingTop.IsConstant();

            if (!rc)
                return false;

            if (PaddingBottom != null)
                rc = PaddingBottom.IsConstant();

            if (!rc)
                return false;

            if (LineHeight != null)
                rc = LineHeight.IsConstant();

            if (!rc)
                return false;

            return rc;
        }

        internal Drawing.Rectangle PaddingAdjust(Report rpt, Row r, Drawing.Rectangle rect, bool bAddIn)
        {
            int pbottom = this.EvalPaddingBottomPx(rpt, r);
            int ptop = this.EvalPaddingTopPx(rpt, r);
            int pleft = this.EvalPaddingLeftPx(rpt, r);
            int pright = this.EvalPaddingRightPx(rpt, r);

            Drawing.Rectangle rt;
            if (bAddIn)     // add in when trying to size the object
                rt = new Drawing.Rectangle(rect.Left - pleft, rect.Top - ptop,
                    rect.Width + pleft + pright, rect.Height + ptop + pbottom);
            else            // otherwise you want the rectangle of the embedded object
                rt = new Drawing.Rectangle(rect.Left + pleft, rect.Top + ptop,
                    rect.Width - pleft - pright, rect.Height - ptop - pbottom);
            return rt;
        }

        internal Color EvalBackgroundColor(Report rpt, Row row)
        {
            if (BackgroundColor == null)
                return Drawing.Color.Empty;

            string c = BackgroundColor.EvaluateString(rpt, row);
            return XmlUtil.ColorFromHtml(c, Drawing.Color.Empty, rpt);
        }

        internal BackgroundGradientTypeEnum EvalBackgroundGradientType(Report rpt, Row r)
        {
            if (BackgroundGradientType == null)
                return BackgroundGradientTypeEnum.None;

            string bgt = BackgroundGradientType.EvaluateString(rpt, r);
            return StyleInfo.GetBackgroundGradientType(bgt, BackgroundGradientTypeEnum.None);
        }

        internal Color EvalBackgroundGradientEndColor(Report rpt, Row r)
        {
            if (BackgroundGradientEndColor == null)
                return Drawing.Color.Empty;

            string c = BackgroundGradientEndColor.EvaluateString(rpt, r);
            return XmlUtil.ColorFromHtml(c, Drawing.Color.Empty, rpt);
        }

        internal bool IsFontItalic(Report rpt, Row r)
        {
            if (EvalFontStyle(rpt, r) == FontStyleEnum.Italic)
                return true;

            return false;
        }

        internal FontStyleEnum EvalFontStyle(Report rpt, Row row)
        {
            if (FontStyle == null)
                return FontStyleEnum.Normal;

            string fs = FontStyle.EvaluateString(rpt, row);
            return StyleInfo.GetFontStyle(fs, FontStyleEnum.Normal);
        }

        internal string EvalFontFamily(Report rpt, Row row)
        {
            if (FontFamily == null)
                return DefaultFontFamily;

            return FontFamily.EvaluateString(rpt, row);
        }

        internal float EvalFontSize(Report rpt, Row row)
        {
            if (FontSize == null)
                return 10;

            string pts;
            pts = FontSize.EvaluateString(rpt, row);
            RSize sz = new RSize(this.OwnerReport, pts);

            return sz.Points;
        }

        internal FontWeightEnum EvalFontWeight(Report rpt, Row row)
        {
            if (FontWeight == null)
                return FontWeightEnum.Normal;

            string weight = this.FontWeight.EvaluateString(rpt, row);
            return StyleInfo.GetFontWeight(weight, FontWeightEnum.Normal);
        }

        internal bool IsFontBold(Report rpt, Row r)
        {
            if (this.FontWeight == null)
                return false;

            string weight = this.FontWeight.EvaluateString(rpt, r);
            switch (weight.ToLower())
            {
                case "bold":
                case "bolder":
                case "500":
                case "600":
                case "700":
                case "800":
                case "900":
                    return true;
                default:
                    return false;
            }
        }

        internal string EvalFormat(Report rpt, Row row)
        {
            if (Format == null)
                return "General";

            string f = Format.EvaluateString(rpt, row);

            if (f == null || f.Length == 0)
                return "General";
            return f;
        }

        internal TextDecorationEnum EvalTextDecoration(Report rpt, Row r)
        {
            if (TextDecoration == null)
                return TextDecorationEnum.None;

            string td = TextDecoration.EvaluateString(rpt, r);
            return StyleInfo.GetTextDecoration(td, TextDecorationEnum.None);
        }

        internal TextAlignEnum EvalTextAlign(Report rpt, Row row)
        {
            if (TextAlign == null)
                return TextAlignEnum.General;

            string a = TextAlign.EvaluateString(rpt, row);
            return StyleInfo.GetTextAlign(a, TextAlignEnum.General);
        }

        internal VerticalAlignEnum EvalVerticalAlign(Report rpt, Row row)
        {
            if (VerticalAlign == null)
                return VerticalAlignEnum.Top;

            string v = VerticalAlign.EvaluateString(rpt, row);
            return StyleInfo.GetVerticalAlign(v, VerticalAlignEnum.Top);
        }

        internal Color EvalColor(Report rpt, Row row)
        {
            if (Color == null)
                return Drawing.Color.Black;

            string c = Color.EvaluateString(rpt, row);
            return XmlUtil.ColorFromHtml(c, Drawing.Color.Black, rpt);
        }

        internal float EvalPaddingLeft(Report rpt, Row row)
        {
            if (PaddingLeft == null)
                return 0;

            string v = PaddingLeft.EvaluateString(rpt, row);
            RSize rz = new RSize(OwnerReport, v);
            return rz.Points;
        }

        internal int EvalPaddingLeftPx(Report rpt, Row row)
        {
            if (PaddingLeft == null)
                return 0;

            string v = PaddingLeft.EvaluateString(rpt, row);
            RSize rz = new RSize(OwnerReport, v);
            return rz.PixelsX;
        }

        internal float EvalPaddingRight(Report rpt, Row row)
        {
            if (PaddingRight == null)
                return 0;

            string v = PaddingRight.EvaluateString(rpt, row);
            RSize rz = new RSize(OwnerReport, v);
            return rz.Points;
        }

        internal int EvalPaddingRightPx(Report rpt, Row row)
        {
            if (PaddingRight == null)
                return 0;

            string v = PaddingRight.EvaluateString(rpt, row);
            RSize rz = new RSize(OwnerReport, v);
            return rz.PixelsX;
        }

        internal float EvalPaddingTop(Report rpt, Row row)
        {
            if (PaddingTop == null)
                return 0;

            string v = PaddingTop.EvaluateString(rpt, row);
            RSize rz = new RSize(OwnerReport, v);
            return rz.Points;
        }

        internal int EvalPaddingTopPx(Report rpt, Row row)
        {
            if (PaddingTop == null)
                return 0;

            string v = PaddingTop.EvaluateString(rpt, row);
            RSize rz = new RSize(OwnerReport, v);
            return rz.PixelsY;
        }

        internal float EvalPaddingBottom(Report rpt, Row row)
        {
            if (PaddingBottom == null)
                return 0;

            string v = PaddingBottom.EvaluateString(rpt, row);
            RSize rz = new RSize(OwnerReport, v);
            return rz.Points;
        }

        internal int EvalPaddingBottomPx(Report rpt, Row row)
        {
            if (PaddingBottom == null)
                return 0;

            string v = PaddingBottom.EvaluateString(rpt, row);
            RSize rz = new RSize(OwnerReport, v);
            return rz.PixelsY;
        }

        internal float EvalLineHeight(Report rpt, Row r)
        {
            if (LineHeight == null)
                return float.NaN;

            string sz = LineHeight.EvaluateString(rpt, r);
            RSize rz = new RSize(OwnerReport, sz);
            return rz.Points;
        }

        internal DirectionEnum EvalDirection(Report rpt, Row r)
        {
            if (Direction == null)
                return DirectionEnum.LTR;

            string d = Direction.EvaluateString(rpt, r);
            return StyleInfo.GetDirection(d, DirectionEnum.LTR);
        }

        internal WritingModeEnum EvalWritingMode(Report rpt, Row r)
        {
            if (WritingMode == null)
                return WritingModeEnum.lr_tb;

            string w = WritingMode.EvaluateString(rpt, r);

            return StyleInfo.GetWritingMode(w, WritingModeEnum.lr_tb);
        }

        internal string EvalLanguage(Report rpt, Row r)
        {
            if (Language == null)
                return OwnerReport.EvalLanguage(rpt, r);

            return Language.EvaluateString(rpt, r);
        }

        internal UnicodeBiDirectionalEnum EvalUnicodeBiDirectional(Report rpt, Row r)
        {
            if (UnicodeBiDirectional == null)
                return UnicodeBiDirectionalEnum.Normal;

            string u = UnicodeBiDirectional.EvaluateString(rpt, r);
            return StyleInfo.GetUnicodeBiDirectional(u, UnicodeBiDirectionalEnum.Normal);
        }

        internal CalendarEnum EvalCalendar(Report rpt, Row r)
        {
            if (Calendar == null)
                return CalendarEnum.Gregorian;

            string c = Calendar.EvaluateString(rpt, r);
            return StyleInfo.GetCalendar(c, CalendarEnum.Gregorian);
        }

        internal string EvalNumeralLanguage(Report rpt, Row r)
        {
            if (NumeralLanguage == null)
                return EvalLanguage(rpt, r);

            return NumeralLanguage.EvaluateString(rpt, r);
        }

        internal int EvalNumeralVariant(Report rpt, Row r)
        {
            if (NumeralVariant == null)
                return 1;

            int v = (int)NumeralVariant.EvaluateDouble(rpt, r);
            if (v < 1 || v > 7)     // correct for bad data
                v = 1;
            return v;
        }

        private WorkClass GetWC(Report rpt)
        {
            if (!this.ConstantStyle)
                return null;

            WorkClass wc = rpt.Cache.Get(this, "wc") as WorkClass;
            if (wc == null)
            {
                wc = new WorkClass();
                rpt.Cache.Add(this, "wc", wc);
            }
            return wc;
        }

        private void RemoveWC(Report rpt)
        {
            rpt.Cache.Remove(this, "wc");
        }

        class WorkClass
        {
            internal string CssStyle;       // When ConstantStyle is true; this will hold cache of css
            internal StyleInfo StyleInfo;   // When ConstantStyle is true; this will hold cache of StyleInfo
            internal WorkClass()
            {
                CssStyle = null;
                StyleInfo = null;
            }
        }

    }
}
