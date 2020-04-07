using System;
using SkiaSharp;

namespace appbox.Drawing
{
    public sealed class SolidBrush : Brush
    {
        public Color Color { get; set; }

        public SolidBrush(Color color)
        {
            Color = color;
        }

        internal override void ApplyToSKPaint(SKPaint skPaint)
        {
            skPaint.Color = new SKColor((uint)Color.Value);
            skPaint.Style = SKPaintStyle.Fill;
        }

    }
}
