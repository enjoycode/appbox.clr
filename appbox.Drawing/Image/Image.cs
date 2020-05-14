using System;
using System.IO;
using SkiaSharp;

namespace appbox.Drawing
{
    public abstract class Image : IDisposable
    {
        public static Image FromStream(Stream stream)
        {
            //TODO:暂只支持位图
            var bmp = SKBitmap.Decode(stream);
            return new Bitmap(bmp);
        }

        /// <summary>
        /// Width of pixels
        /// </summary>
        public int Width
        {
            get
            {
                if (this is Bitmap bmp && bmp.skBitmap != null)
                {
                    SKImageInfo info = bmp.skBitmap.Info;
                    return /*HiDpi == true ? info.Width / 2 :*/ info.Width;
                }
                return 0;
            }
        }

        /// <summary>
        /// Height of pixels
        /// </summary>
        public int Height
        {
            get
            {
                if (this is Bitmap bmp && bmp.skBitmap != null)
                {
                    SKImageInfo info = bmp.skBitmap.Info;
                    return /*HiDpi == true ? info.Height / 2 :*/ info.Height;
                }
                return 0;
            }
        }

        public float HorizontalResolution => 72f; //TODO:
        public float VerticalResolution => 72f; //TODO:

        public ImageFormat RawFormat
        {
            get { return ImageFormat.MemoryBmp; }
        }

        public Size Size
        {
            get { return new Size(Width, Height); }
        }

        public PixelFormat PixelFormat { get { throw new NotImplementedException(); } } //todo::

        public abstract void Save(Stream stream, ImageFormat format, int quality = 100);

        public Image Clone()
        {
            if (this is Bitmap)
            {
                //todo:暂重画
                var src = (Bitmap)this;
                var clone = new Bitmap(src.Width, src.Height);
                using (var g = Graphics.FromImage(clone))
                {
                    g.DrawImage(src, 0, 0);
                }
                return clone;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        #region ====IDisposable Support====
        private bool disposedValue = false;

        protected virtual void DisposeSKObject() { }

        protected void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    DisposeSKObject();
                disposedValue = true;
            }
        }

        public void Dispose() => Dispose(true);
        #endregion
    }
}
