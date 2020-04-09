using System;
using SkiaSharp;

namespace appbox.Drawing
{
    public abstract class Brush : IDisposable
    {

        internal abstract void ApplyToSKPaint(SKPaint skPaint);

        #region ====IDisposable Support====
        private bool disposedValue = false;

        protected virtual void DisposeSKObject() { }

        protected void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    DisposeSKObject();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}
