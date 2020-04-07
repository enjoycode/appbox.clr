using System;
using SkiaSharp;

namespace appbox.Drawing
{
    public sealed class Pen : IDisposable
    {

        private static readonly float[] dotArray = { 1.0f, 1.0f };
        private static readonly float[] dashArray = { 3.0f, 1.0f };
        private static readonly float[] dashDotArray = { 3.0f, 1.0f, 1.0f, 1.0f };
        private static readonly float[] dashDotDotArray = { 3.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f };

        public Color Color { get; set; }
        public float Width { get; set; }

        public LineJoin LineJoin { get; set; }
        public LineCap StartCap { get; set; }
        public LineCap EndCap { get; set; }

        private DashStyle dashStyle;
        public PenAlignment Alignment { get; set; }

        public float[] DashPattern { get; set; }

        public DashStyle DashStyle
        {
            get { return dashStyle; }
            set
            {
                dashStyle = value;
                switch (dashStyle)
                {
                    case DashStyle.Solid:
                        DashPattern = null;
                        break;
                    case DashStyle.Dot:
                        DashPattern = Pen.dotArray;
                        break;
                    case DashStyle.Dash:
                        DashPattern = Pen.dashArray;
                        break;
                    case DashStyle.DashDot:
                        DashPattern = Pen.dashDotArray;
                        break;
                    case DashStyle.DashDotDot:
                        DashPattern = Pen.dashDotDotArray;
                        break;
                    case DashStyle.Custom:
                        /* we keep the current assigned value when switching to Custom */
                        /*dashPattern should be assigned before dashStyle assigned*/
                        throw new Exception("dashPattern != nil && dashPattern!.count > 0");
                }
            }
        }

        private SKPathEffect skPathEffect;

        public Pen(Color color) : this(color, 1.0f) { }

        public Pen(Color color, float width)
        {
            Color = color;
            Width = width;
        }

        internal void ApplyToSKPaint(SKPaint skPaint)
        {
            skPaint.Color = new SKColor((uint)Color.Value);
            skPaint.Style = SKPaintStyle.Stroke;
            skPaint.StrokeWidth = Width;

            if (dashStyle != DashStyle.Solid)
            {
                if (skPathEffect == null)
                    skPathEffect = SKPathEffect.CreateDash(DashPattern, 0);
                skPaint.PathEffect = skPathEffect;
            }
        }

        #region ====IDisposable Support====
        private bool disposedValue = false;

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (skPathEffect != null)
                    {
                        skPathEffect.Dispose();
                        skPathEffect = null;
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