using System;
using System.IO;
using Xunit;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace appbox.Drawing.Tests
{
    public class HarfBuzzTest
    {

        [Fact]
        public void ShapeTest()
        {
            using var bmp = new Bitmap(600, 400);
            using var canvas = new SKCanvas(bmp.skBitmap);

            using var typeface = SKFontManager.Default.MatchCharacter('中');
            using var paint = new SKPaint();
            paint.IsAntialias = true;
            paint.TextEncoding = SKTextEncoding.Utf16;
            paint.TextSize = 30;
            paint.Color = new SKColor(255, 0, 0, 255);
            paint.Style = SKPaintStyle.Fill;
            paint.Typeface = typeface;
            var src = "Hello Future! 你好，未来！";
            canvas.DrawText(src, 0, 100, paint);

            using var shaper = new SKShaper(typeface);
            paint.TextAlign = SKTextAlign.Right;
            //paint.TextEncoding = SKTextEncoding.GlyphId; //Not implemented
            canvas.DrawShapedText(shaper, src, 0, 200, paint);

            using var fs = File.OpenWrite("A_document.jpg");
            bmp.Save(fs, ImageFormat.Jpeg);
        }

    }
}
