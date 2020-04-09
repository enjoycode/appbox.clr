using System;
using System.IO;
using Xunit;

namespace appbox.Drawing.Tests
{
    public class TextLayoutTest
    {
        private const string OutFile = "A_TextLayout.jpg";

        [Fact]
        public void DrawTextLayout()
        {
            {
                using var font = new Font(20);
                using var bmp = new Bitmap(620, 600);
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

                using var fs = File.OpenWrite(OutFile);
                bmp.Save(fs, ImageFormat.Jpeg);
                fs.Close();
            }

            //----以下System.Drawing对比----
            {
                using var font = new System.Drawing.Font("PingFang SC", 15);
                var points = font.SizeInPoints;
                var height = font.Height;
                
                using var bmp = System.Drawing.Bitmap.FromFile(OutFile);
                using var g = System.Drawing.Graphics.FromImage(bmp);
                using var pen = new System.Drawing.Pen(System.Drawing.Color.Black, 1);
                using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Red);

                var text = "Hello Future! 你好，未来！";

                var sf = new System.Drawing.StringFormat();
                var rect = new System.Drawing.RectangleF(5, 305, 200, 80);
                for (int i = 0; i < 3; i++)
                {
                    var rectX = rect;
                    for (int j = 0; j < 3; j++)
                    {
                        g.DrawRectangle(pen, rectX.X, rectX.Y, rectX.Width, rectX.Height);

                        sf.Alignment = (System.Drawing.StringAlignment)i;
                        sf.LineAlignment = (System.Drawing.StringAlignment)j;
                        g.DrawString(text, font, brush, rectX, sf);

                        rectX.Offset(200 + 5, 0);
                    }

                    rect.Offset(0, rect.Height + 10);
                }

                using var fs = File.OpenWrite(OutFile);
                bmp.Save(fs, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
        }

    }
}
