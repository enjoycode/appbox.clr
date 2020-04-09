using System;
using System.Collections.Generic;
using System.Text;
using appbox.Drawing;

namespace appbox.Reporting.RDL.Utility
{
    /// <summary>
    /// A utility class that contains additional 
    /// methods for drawing and unit conversion (points/pixels).
    /// </summary>
    public sealed class Measurement
    {
        /// <summary>
        /// A method used to obtain a rectangle from the screen coordinates supplied.
        /// </summary>
        public static Drawing.Rectangle RectFromPoints(Point p1, Point p2)
        {
            Drawing.Rectangle r = new Drawing.Rectangle();
            // set the width and x of rectangle
            if (p1.X < p2.X)
            {
                r.X = p1.X;
                r.Width = p2.X - p1.X;
            }
            else
            {
                r.X = p2.X;
                r.Width = p1.X - p2.X;
            }
            // set the height and y of rectangle
            if (p1.Y < p2.Y)
            {
                r.Y = p1.Y;
                r.Height = p2.Y - p1.Y;
            }
            else
            {
                r.Y = p2.Y;
                r.Height = p1.Y - p2.Y;
            }
            return r;
        }

        /// <summary>
        /// The constant value used to calculate the conversion of pixels into points, as a float.
        /// </summary>
        public const float POINTSIZE_F = 72.27f;
        /// <summary>
        /// The constant value used to calculate the conversion of pixels into points, as a decimal.
        /// </summary>
        public const decimal POINTSIZE_M = 72.27m;

        public const float STANDARD_DPI_X = 96f;
        public const float STANDARD_DPI_Y = 96f;

        /// <summary>
        /// A method used to convert pixels into points.
        /// </summary>
        /// <returns>A float containing the converted measurement of the pixels into points.</returns>
        public static float PointsFromPixels(float pixels, float dpi)
        {
            return (pixels * POINTSIZE_F) / dpi;
        }
        /// <summary>
        /// A method used to convert pixels into points.
        /// </summary>
        /// <returns>A PointF containing the point X and Y values for the pixel X and Y values that were supplied.</returns>
        public static PointF PointsFromPixels(float pixelsX, float pixelsY, PointF Dpi)
        {
            return new PointF(PointsFromPixels(pixelsX, Dpi.X), PointsFromPixels(pixelsY, Dpi.Y));
        }
        /// <summary>
        /// A method used to convert points into pixels.
        /// </summary>
        /// <returns>An int containing the converted measurement of the points into pixels.</returns>
        public static int PixelsFromPoints(float points, float dpi)
        {
            int r = (int)(((double)points * dpi) / POINTSIZE_F);
            if (r == 0 && points > .0001f)
                r = 1;
            return r;
        }
        /// <summary>
        /// A method used to convert points into pixels.
        /// </summary>
        /// <returns>A PointF containing the pixel X and Y values for the point X and Y values that were supplied.</returns>
        public static PointF PixelsFromPoints(float pointsX, float pointsY, PointF Dpi)
        {
            return new PointF(PixelsFromPoints(pointsX, Dpi.X), PixelsFromPoints(pointsY, Dpi.Y));
        }
        /// <summary>
        /// A method used to convert points into twips.
        /// </summary>
        /// <returns>An int containing the twips for the number of points that were supplied.</returns>
        public static int TwipsFromPoints(float points)
        {
            return (int)Math.Round(points * 20, 0);
        }
        /// <summary>
        /// A method used to convert pixels into twips.
        /// </summary>
        /// <returns>An int containing the twips for the number of pixels that were supplied.</returns>
        public static int TwipsFromPixels(float pixels, float dpi)
        {
            return TwipsFromPoints(PointsFromPixels(pixels, dpi));
        }

        #region Obsolete Methods
        /// <summary>
        /// A method used to convert Pixels into Points. Obsolete. Use PointsFromPixels instead.
        /// </summary>
        /// <returns>A float containing the converted measurement of the pixels into points.</returns>
        [System.Obsolete("This method has been deprecated. Use PointsFromPixels() instead.")]
        public static float PointsX(float pixelX, float dpi)// pixels to points
        {
            return PointsFromPixels(pixelX, dpi);
        }
        /// <summary>
        /// A method used to convert Pixels into Points. Obsolete. Use PointsFromPixels instead.
        /// </summary>
        /// <returns>A float containing the converted measurement of the pixels into points.</returns>
        [System.Obsolete("This method has been deprecated. Use PointsFromPixels() instead.")]
        public static float PointsY(float pixelY, float dpi)
        {
            return PointsFromPixels(pixelY, dpi);
        }

        /// <summary>
        /// A method used to convert Points into Pixels. Obsolete. Use PixelsFromPoints instead.
        /// </summary>
        /// <returns>An int containing the converted measurement of the points into pixels.</returns>
        [System.Obsolete("This method has been deprecated. Use PixelsFromPoints() instead.")]
        public static int PixelsX(float pointX, float dpi)// points to pixels
        {
            return PixelsFromPoints(pointX, dpi);
        }
        /// <summary>
        /// A method used to convert Points into Pixels. Obsolete. Use PixelsFromPoints instead.
        /// </summary>
        /// <returns>An int containing the converted measurement of the points into pixels.</returns>
        [System.Obsolete("This method has been deprecated. Use PixelsFromPoints() instead.")]
        public static int PixelsY(float pointY, float dpi)
        {
            return PixelsFromPoints(pointY, dpi);
        }
        #endregion
    }
}