using System;
using SkiaSharp;

namespace appbox.Drawing
{

    public sealed class TextRenderer
    {

        /// <summary>
        /// 在使用指定字体绘制时，提供指定文本的尺寸（以像素为单位）
        /// </summary>
        /// <param name="text">要测量的文本</param>
        /// <param name="font">要应用于已测量文本的 System.Drawing.Font</param>
        /// <param name="dpi"></param>
        /// <returns>使用指定的 font 在一行上绘制的 text 的 System.Drawing.Size（以像素为单位）</returns>
        public static SizeF MeasureText(string text, Font font, float dpi = 96f) //todo:待验证
        {
            using var paint = new SKPaint();
            font.ApplyToSKPaint(paint, GraphicsUnit.Pixel, dpi);
            SKRect skrect = new SKRect();
            var width = paint.MeasureText(text, ref skrect);
            return new SizeF(width, font.GetHeight() /*skrect.Bottom - skrect.Top*/);
        }

        public static SizeF MeasureText(string text, Font font, SizeF maxSize, StringFormat format)
        {
            //todo:暂用TextLayout来处理
            var layout = new TextLayout(text, font);
            layout.Width = maxSize.Width;
            layout.Height = maxSize.Height;
            layout.StringFormat = format;
            return layout.GetInkSize();
        }

    }
}

