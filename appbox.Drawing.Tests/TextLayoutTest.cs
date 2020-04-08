using System;
using System.IO;
using Xunit;

namespace appbox.Drawing.Tests
{
    public class TextLayoutTest
    {
        [Fact]
        public void DrawTextLayout()
        {
            using var font = new Font(20);
            using var bmp = new Bitmap(620, 300);
            using var g = Graphics.FromImage(bmp);

            var text = "Hello Future! 你好，未来！";

            var sf = new StringFormat();
            var rect = new RectangleF(5, 5, 200, 80);
            for (int i = 0; i < 3; i++)
            {
                var rectX = rect;
                for (int j = 0; j < 3; j++)
                {
                    g.DrawRectangle(Color.Black, 1f, rectX);
                    sf.Alignment = (StringAlignment)i;
                    sf.LineAlignment = (StringAlignment)j;
                    g.DrawString(text, font, Color.Red, rectX, sf);

                    rectX.Offset(200 + 5, 0);
                }

                rect.Offset(0, rect.Height + 10);
            }

            using var fs = File.OpenWrite("A_TextLayout.jpg");
            bmp.Save(fs, ImageFormat.Jpeg);
        }
    }
}
