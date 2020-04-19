using System;
using SkiaSharp;

namespace appbox.Drawing
{
    public sealed class Font : IDisposable
    {
        public static readonly string DefaultFontFamilyName;
        private const string NotoFont = "DefaultFont.otf";
        private static readonly bool DefaultFontIsNoto;

        static Font()
        {
            //TODO:编译条件确认是否使用默认中文字体
            var typeface = SKFontManager.Default.MatchCharacter('中');
            if (typeface == null)
            {
                //使用默认Noto字体
                typeface = SKFontManager.Default.CreateTypeface(NotoFont);
                if (typeface == null)
                    typeface = SKTypeface.CreateDefault();
                else
                    DefaultFontIsNoto = true;
            }

            DefaultFontFamilyName = typeface.FamilyName;
            typeface.Dispose();
        }

        #region ====Fields & Properties====
        private SKTypeface skTypeface;
        private SKFontMetrics? fontMetrics = null; //TODO: reset to null when change some property

        internal SKFontMetrics FontMetrics
        {
            get
            {
                if (skTypeface == null)
                    return new SKFontMetrics();
                if (fontMetrics == null)
                {
                    using var paint = new SKPaint();
                    paint.TextEncoding = SKTextEncoding.Utf16;
                    ApplyToSKPaint(paint, GraphicsUnit.Pixel, 72f); //目标类型为像素 //TODO: dpi=96
                    paint.GetFontMetrics(out SKFontMetrics metrics);
                    fontMetrics = metrics;
                }
                return fontMetrics.Value;
            }
        }

        /// <summary>
        /// Gets the em-size of this Font measured in the units specified by the Unit property.
        /// </summary>
        public float Size { get; private set; }

        /// <summary>
        /// Gets the em-size, in points, of this Font.
        /// </summary>
        public float SizeInPoints
        {
            //注意：考虑大部分是Reporting使用，暂传入dpi=72
            get { return GraphicsUnitConverter.Convert(Unit, GraphicsUnit.Point, Size, 72.0f); }
        }

        /// <summary>
        /// Gets the unit of measure for this Font.
        /// </summary>
        public GraphicsUnit Unit { get; private set; }

        /// <summary>
        /// The line spacing, in pixels, of this font.
        /// </summary>
        public int Height
        {
            get { return (int)Math.Ceiling(GetHeight()); }
        }

        /// <summary>
        ///  临时给RichTextEditor
        /// </summary>
        /// <value>The FH eight.</value>
        //public int FHeight
        //{
        //    get
        //    {
        //        var metrics = FontMetrics;
        //        return (int)Math.Ceiling(-metrics.Ascent + metrics.Descent);
        //    }
        //}

        public string Name => skTypeface == null ? string.Empty : skTypeface.FamilyName;

        public FontStyle Style { get; private set; }

        public bool Bold { get; }

        public bool Italic { get; }

        public bool Underline { get; }

        public bool Strikeout { get; } //todo: fix upper style property

        public string FamilyName => skTypeface.FamilyName;

        //public FontFamily FontFamily
        //{
        //    get
        //    {
        //        return null;
        //        //throw new NotImplementedException();
        //    }
        //}
        #endregion

        #region ====Ctor & Dispose====
        public Font(float size) :
            this(null, size, FontStyle.Regular, GraphicsUnit.Point)
        { }

        public Font(string familyName, float size) :
            this(familyName, size, FontStyle.Regular)
        { }

        public Font(string familyName, float size, FontStyle style) :
            this(familyName, size, style, GraphicsUnit.Point)
        { }

        public Font(string familyName, float size, FontStyle style, GraphicsUnit unit)
        {
            SKFontStyle skFontStyle = style switch
            {
                FontStyle.Bold => SKFontStyle.Normal,
                FontStyle.Italic => SKFontStyle.Italic,
                _ => SKFontStyle.Normal,
            };

            if (string.IsNullOrEmpty(familyName) || familyName == DefaultFontFamilyName)
            {
                if (DefaultFontIsNoto)
                {
                    skTypeface = SKFontManager.Default.CreateTypeface(NotoFont);
                }
                else
                {
                    skTypeface = SKTypeface.FromFamilyName(SKTypeface.Default.FamilyName, skFontStyle);
                }
            }
            else
            {
                // SKTypeface.FromFamilyName(familyName, SKFontStyle)找不到返回默认的
                skTypeface = SKTypeface.FromFamilyName(familyName, skFontStyle);
            }

            Size = size;
            Style = style;
            Unit = unit;
        }
        #endregion

        #region ====Methods====
        /// <summary>
        /// Returns the line spacing, in pixels, of this font.
        /// </summary>
        public float GetHeight()
        {
            var metrics = FontMetrics;
            return -metrics.Ascent + metrics.Descent + metrics.Leading;
        }

        public float GetHeight(Graphics graphics)
        {
            //TODO:转换单位
            return GetHeight();
        }

        internal void ApplyToSKPaint(SKPaint skPaint, GraphicsUnit targetUnit, float dpi)
        {
            skPaint.Typeface = skTypeface;
            float sizePixel = GraphicsUnitConverter.Convert(Unit, targetUnit, Size, dpi); //注意:转换单位
            skPaint.TextSize = sizePixel; //TODO: 转换与System.Drawing不一致 20 -> 15，待修改
            skPaint.IsAntialias = true;
            skPaint.SubpixelText = true;
        }
        #endregion

        #region ====IDisposable Support====
        private bool disposedValue = false;

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (skTypeface != null)
                    {
                        skTypeface.Dispose();
                        skTypeface = null;
                    }
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}