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

        public abstract void Save(Stream stream, ImageFormat format);


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
