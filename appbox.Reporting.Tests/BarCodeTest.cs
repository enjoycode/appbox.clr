using System;
using System.Collections.Generic;
using Xunit;
using appbox.Drawing;
using System.IO;

namespace appbox.Reporting.Tests
{
    public class BarCodeTest
    {
        [Fact]
        public void DrawBarCodeTest()
        {
            var code128 = new BarCode128();
            var props = new Dictionary<string, object>
            {
                { "Code", "Hello Future!" }
            };
            code128.SetProperties(props);

            var bmp = code128.DrawImage(180, 40);
            var img = new Bitmap(bmp);
            using var fs = File.OpenWrite("A_BarCode128.jpg");
            img.Save(fs, ImageFormat.Jpeg);
        }
    }
}
