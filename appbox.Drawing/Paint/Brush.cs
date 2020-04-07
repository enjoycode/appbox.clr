using System;
using SkiaSharp;

namespace appbox.Drawing
{
    public abstract class Brush : IDisposable
    {

        internal abstract void ApplyToSKPaint(SKPaint skPaint);

        #region ====IDisposable Support====
        private bool disposedValue = false;

        protected virtual void DisposeInternal() { }

        protected void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    DisposeInternal();
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
