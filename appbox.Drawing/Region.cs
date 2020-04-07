using System;
using SkiaSharp;

namespace appbox.Drawing
{
    public sealed class Region : IDisposable
    {
        internal SKRegion skRegion;

        public Region()
        {
            skRegion = new SKRegion();
        }

        public Region(Rectangle rect)
        {
            SKRectI skRectI = new SKRectI(rect.Left, rect.Top, rect.Right, rect.Bottom);
            skRegion = new SKRegion(skRectI);
        }

        public Region(GraphicsPath path)
        {
            skRegion = new SKRegion(path.skPath);
        }

        public bool IsVisible(Point pt)
        {
            return skRegion.Contains(pt.X, pt.Y);
        }

        public void Intersect(Region region)
        {
            skRegion.Intersects(region.skRegion);
        }

        public void Intersect(GraphicsPath path)
        {
            skRegion.Intersects(path.skPath);
        }

        #region ====IDisposable Support====
        private bool disposedValue = false;

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (skRegion != null)
                    {
                        skRegion.Dispose();
                        skRegion = null;
                    }
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

