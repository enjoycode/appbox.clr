using System;
using System.IO;
using SkiaSharp;

namespace appbox.Drawing
{
    public class Bitmap : Image
    {
        internal SKBitmap skBitmap;

        public Bitmap(int width, int height) : this(width, height, PixelFormat.Format32bppRgb) { }

        public Bitmap(int width, int height, PixelFormat format)
        {
            //TODO: fix format to SKColorType
            skBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque);
            skBitmap.Erase(new SKColor(255, 255, 255, 0)); //用于清除画布
        }

        internal Bitmap(SKBitmap skBitmap) { this.skBitmap = skBitmap; }

        public override void Save(Stream stream, ImageFormat format)
        {
            using var wstream = new SKManagedWStream(stream, false);
            using var pixmap = new SKPixmap();
            if (skBitmap.PeekPixels(pixmap))
            {
                pixmap.Encode(wstream, (SKEncodedImageFormat)format, 100);
            }
        }

        protected override void DisposeSKObject()
        {
            if (skBitmap != null)
            {
                skBitmap.Dispose();
                skBitmap = null;
            }
        }
    }
}
