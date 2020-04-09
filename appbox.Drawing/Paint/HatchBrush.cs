using System;
using SkiaSharp;

namespace appbox.Drawing
{
    public sealed class HatchBrush : Brush
    {
        public HatchStyle HatchStyle { get; private set; }

        public Color ForegroundColor { get; private set; }

        public Color BackgroundColor { get; private set; }

        public HatchBrush(HatchStyle hatchstyle, Color foreColor, Color backColor)
        {
            HatchStyle = hatchstyle;
            ForegroundColor = foreColor;
            BackgroundColor = backColor;
        }

        internal override void ApplyToSKPaint(SKPaint skPaint)
        {
            throw new NotImplementedException();
        }
    }
}
