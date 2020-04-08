using System;
using System.IO;
using Xunit;
using SkiaSharp;

namespace appbox.Drawing.Tests
{
    public class DrawTest
    {

        [Fact]
        public void FirstDraw()
        {
            using var font = new Font("PingFang SC", 30);
            using var bmp = new Bitmap(400, 300);
            using var g = Graphics.FromImage(bmp);
            g.FillRectangle(Color.Red, new Rectangle(10, 10, 380, 280));
            g.DrawString("Hello Future!", font, Color.Yellow, 50, 50);
            g.DrawString("你好未来!", font, Color.White, 50, 100);

            using var fs = File.OpenWrite("A_FirstDraw.jpg");
            bmp.Save(fs, ImageFormat.Jpeg);
        }

        [Fact]
        public void DrawEmojiTest()
        {
            var stream = SKFileWStream.OpenStream("A_document.pdf");
            var document = SKDocument.CreatePdf(stream);
            var canvas = document.BeginPage(256, 256);

            var emojiChar = StringUtilities.GetUnicodeCharacterCode("🚀", SKTextEncoding.Utf32);
            // ask the font manager for a font with that character
            var emojiTypeface = SKFontManager.Default.MatchCharacter(emojiChar);

            // draw it
            using var paint = new SKPaint { Typeface = emojiTypeface };
            canvas.DrawText("🌐 🍪 🍕 🚀", 20, 40, paint);

            document.EndPage();
            document.Close();
        }

        [Fact]
        public void FontManagerTest()
        {
            var ls = SKFontManager.Default.FontFamilies;
            var tf1 = SKFontManager.Default.MatchFamily("Arial", SKFontStyle.Normal);
            var tf2 = SKFontManager.Default.MatchCharacter('中');
        }

    }
}
