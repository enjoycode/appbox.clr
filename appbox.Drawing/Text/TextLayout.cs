using System;
using System.Collections.Generic;
using SkiaSharp;

namespace appbox.Drawing
{
    /// <summary>
    /// 临时用于Text Shaping
    /// </summary>
    public sealed class TextLayout
    {
        private bool needRun = true;
        private float dpi = 72f;

        /// <summary>
        /// 用于非上对齐时的偏移值
        /// </summary>
        public float OffsetY { get; private set; }

        /// <summary>
        /// 每行高度，包括leading
        /// </summary>
        public float LineHeight => font.FontMetrics.Descent - font.FontMetrics.Ascent + font.FontMetrics.Leading;

        internal List<TextLayoutLine> lines = new List<TextLayoutLine>();

        private Font font;
        public Font Font
        {
            get { return font; }
            set
            {
                if (font != value)
                {
                    font = value;
                    needRun = true;
                }
            }
        }

        private string text;
        public string Text
        {
            get { return text; }
            set
            {
                if (text != value)
                {
                    text = value;
                    needRun = true;
                }
            }
        }

        private StringFormat stringFormat;
        public StringFormat StringFormat
        {
            get { return stringFormat; }
            set
            {
                if (stringFormat != value)
                {
                    stringFormat = value;
                    needRun = true;
                }
            }
        }

        private float width = float.MaxValue;
        public float Width
        {
            get { return width; }
            set
            {
                if (width != value)
                {
                    width = value;
                    if (!needRun)
                        needRun = ReCalcOffsetX(value);
                }
            }
        }

        private float height = float.MaxValue;
        public float Height
        {
            get { return height; }
            set
            {
                if (height != value)
                {
                    var oldValue = height;
                    height = value;
                    if (!needRun)
                    {
                        if (float.IsPositiveInfinity(oldValue)) //todo: 如果原本为PositiveInfinity，现在为具体值，且已经运行过，则只需要重新计算OffsetY即可
                            CalcOffsetY();
                        else //TODO: 判断是否收缩新高度
                            needRun = true;
                    }
                }
            }
        }

        private bool IsVertical => stringFormat == null ?
            false : (stringFormat.FormatFlags & StringFormatFlags.DirectionVertical) == StringFormatFlags.DirectionVertical;

        private bool IsNoWrap => stringFormat == null ?
                    false : (stringFormat.FormatFlags & StringFormatFlags.NoWrap) == StringFormatFlags.NoWrap;

        public TextLayout(string text, Font font) : this(text, font, null) { }

        public TextLayout(string text, Font font, StringFormat sf, float dpi = 72f)
        {
            this.text = text;
            this.font = font;
            this.stringFormat = sf;
            this.dpi = dpi;
        }

        internal unsafe void Run()
        {
            if (!needRun)
                return;

            lines.Clear();

            //设置paint参数
            var paint = new SKPaint();
            paint.TextEncoding = SKTextEncoding.Utf16;
            font.ApplyToSKPaint(paint, GraphicsUnit.Pixel, dpi); //注意目标类型为像素
            //测量FontMetric
            SKFontMetrics metrics = font.FontMetrics;

            //根据文字排版方向确认限宽及限高
            float maxWidth = Math.Min(32767, IsVertical ? height : width); //TODO:
            float maxHeight = IsVertical ? width : height;
            //TODO:计算高度不满足一行的情况

            fixed (char* ptr = Text)
            {
                byte* textPtr = (byte*)ptr;
                byte* startTextPtr = (byte*)ptr;

                int totalBytes = text.Length * 2;
                int leftBytes = totalBytes;
                float y = 0;
                while (true)
                {
                    //根据是否允许换行，调用不同的breakText方法
                    var measuredWidth = 0f;
                    var lineBytes = (int)paint.BreakText(new IntPtr(startTextPtr), new IntPtr(leftBytes), maxWidth, out measuredWidth);
                    //var lineBytes = SkiaApi.sk_paint_break_text_icu(paint, new IntPtr(startTextPtr), leftBytes, maxWidth, null); //TODO:***** fix maxWidth

                    var newLine = new TextLayoutLine(this);
                    newLine.startByteIndex = (int)(startTextPtr - textPtr);
                    newLine.byteLength = lineBytes;
                    newLine.widths = paint.GetGlyphWidths(new IntPtr(startTextPtr), lineBytes);
                    //newLine.startCharIndex = 0; //TODO:

                    //根据对齐方式计算Line的offsetX值
                    if (stringFormat != null && stringFormat.Alignment != StringAlignment.Near)
                    {
                        //计算当前行Ink总宽度
                        float lineInkWidth = 0f;
                        for (int i = 0; i < newLine.widths.Length; i++)
                        {
                            lineInkWidth += newLine.widths[i];
                        }
                        float lineSpace = width - lineInkWidth;
                        if (lineSpace > 0f)
                        {
                            if (stringFormat.Alignment == StringAlignment.Center)
                                newLine.offsetX = lineSpace / 2f;
                            else if (stringFormat.Alignment == StringAlignment.Far)
                                newLine.offsetX = lineSpace;
                        }
                    }
                    //添加新行
                    lines.Add(newLine);

                    //判断是否允许换行，不允许直接退出循环
                    if (IsNoWrap)
                        break;

                    //计算下一行高度是否超出限高
                    y = y - metrics.Ascent + metrics.Descent + metrics.Leading;
                    if ((y - metrics.Ascent + metrics.Descent) > maxHeight)
                        break;

                    //偏移剩余部分，并判断是否全部计算完毕
                    startTextPtr += lineBytes;
                    if ((int)(startTextPtr - textPtr) >= totalBytes)
                        break;
                    leftBytes -= lineBytes;
                }
            }

            //根据对齐方式计算offsetY的值
            CalcOffsetY();

            needRun = false;
        }

        private bool ReCalcOffsetX(float newWidth)
        {
            if (stringFormat != null && stringFormat.Alignment != StringAlignment.Near)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    //计算当前行Ink总宽度
                    float lineInkWidth = 0f;
                    for (int j = 0; j < line.widths.Length; j++)
                    {
                        lineInkWidth += line.widths[j];
                    }
                    if (lineInkWidth > newWidth)
                    {
                        width = newWidth;
                        return true;
                    }
                    float lineSpace = newWidth - lineInkWidth;
                    if (lineSpace > 0f)
                    {
                        if (stringFormat.Alignment == StringAlignment.Center)
                            line.offsetX = lineSpace / 2f;
                        else if (stringFormat.Alignment == StringAlignment.Far)
                            line.offsetX = lineSpace;
                    }
                    else
                    {
                        line.offsetX = 0f;
                    }
                    lines[i] = line;
                }
            }
            else
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    line.offsetX = 0f;
                    lines[i] = line;
                }
            }

            return false;
        }

        private void CalcOffsetY()
        {
            if (stringFormat != null && stringFormat.LineAlignment != StringAlignment.Near
                && !float.IsPositiveInfinity(height))
            {
                float allLineHeight = lines.Count * LineHeight;
                float heightSpace = height - allLineHeight;
                if (heightSpace > 0f)
                {
                    if (stringFormat.LineAlignment == StringAlignment.Center)
                        OffsetY = heightSpace / 2f;
                    else
                        OffsetY = heightSpace;
                }
            }
            else
            {
                OffsetY = 0f;
            }
        }

        /// 获取文本块的Size 类似于PangoLayout.getExtern
        public SizeF GetInkSize()
        {
            Run();

            float maxWidth = 0f;
            float maxHeight = 0f;
            for (int i = 0; i < lines.Count; i++)
            {
                float lineWidth = 0f; //lines[i].offsetX;
                for (int j = 0; j < lines[i].widths.Length; j++)
                {
                    lineWidth += lines[i].widths[j];
                }
                maxWidth = Math.Max(maxWidth, lineWidth);
                maxHeight += (LineHeight);
            }

            return new SizeF(maxWidth, maxHeight);
        }

        public SizeF GetInkSize(out int charactersFitted, out int linesFilled)
        {
            Run();

            float maxWidth = 0f;
            float maxHeight = 0f;
            int totalChars = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                float lineWidth = 0f; //lines[i].offsetX;
                for (int j = 0; j < lines[i].widths.Length; j++)
                {
                    if (lines[i].widths[j] > 0.0f)
                    {
                        lineWidth += lines[i].widths[j];
                        totalChars += 1;
                    }
                }
                maxWidth = Math.Max(maxWidth, lineWidth);
                maxHeight += (LineHeight);
            }

            charactersFitted = totalChars;
            linesFilled = lines.Count;
            return new SizeF(maxWidth, maxWidth == 0.0f ? 0.0f : maxHeight);
        }

        public SizeF GetLineInkSize(int lineIndex)
        {
            Run();
            var line = lines[lineIndex];
            float lineWidth = 0f; //line.offsetX;
            for (int j = 0; j < line.widths.Length; j++)
            {
                lineWidth += line.widths[j];
            }
            return new SizeF(lineWidth, LineHeight);
        }

        public PointF GetCursorPosition(int charIndex)
        {
            Run();

            //TODO:先根据charIndex找到对应的TextLayoutLine，这里暂时计算单行
            var line = lines[0];
            var x = line.GetCursorPosition(charIndex);
            return new PointF(x, 0);
        }

        public int GetCharIndex(float x, float y)
        {
            Run();

            //TODO:先根据y找到对应的TextLayoutLine，这里暂时计算单行
            var line = lines[0];
            return line.GetCharIndex(x);
        }

    }
}