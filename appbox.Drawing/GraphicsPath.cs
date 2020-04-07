using System;
using SkiaSharp;

namespace appbox.Drawing
{
    public sealed class GraphicsPath : IDisposable
    {
        internal SKPath skPath;

        public GraphicsPath()
        {
            skPath = new SKPath
            {
                FillType = SKPathFillType.EvenOdd
            };
        }

        private GraphicsPath(SKPath from)
        {
            skPath = from;
        }

        public FillMode FillMode
        {
            set
            {
                if (value == FillMode.Alternate)
                {
                    skPath.FillType = SKPathFillType.InverseWinding;
                }
                else if (value == FillMode.Winding)
                {
                    skPath.FillType = SKPathFillType.Winding;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        public PointF[] PathPoints
        {
            get
            {
                int count = skPath.PointCount;
                if (count <= 0)
                    return null;

                SKPoint[] skPoint = skPath.GetPoints(count);
                PointF[] points = new PointF[count];
                for (int i = 0; i < count; i++)
                {
                    points[i].X = skPoint[i].X;
                    points[i].Y = skPoint[i].Y;
                }
                return points;
            }
        }

        public RectangleF GetBounds()
        {
            skPath.GetBounds(out SKRect rect);
            return new RectangleF(rect.Left, rect.Top, rect.Width, rect.Height);
        }

        public GraphicsPath Clone()
        {
            var newPath = new SKPath(skPath);
            return new GraphicsPath(newPath);
        }

        /// <summary>
        /// 在当前图形中添加一段立方体贝赛尔曲线
        /// </summary>
        /// <param name="pt1">起始点</param>
        /// <param name="pt2">第一个控制点</param>
        /// <param name="pt3">第二个控制点</param>
        /// <param name="pt4">终结点</param>
        public void AddBezier(PointF pt1, PointF pt2, PointF pt3, PointF pt4)
        {
            SKPoint lastPt = skPath.LastPoint;
            if (!IsNear(lastPt.X, pt1.X) || !IsNear(lastPt.Y, pt1.Y))
                skPath.MoveTo(pt1.X, pt1.Y);
            //else
            //SkiaApi.sk_path_line_to(nativePath, pt1.X, pt1.Y); //TODO: check need this?
            skPath.CubicTo(pt2.X, pt2.Y, pt3.X, pt3.Y, pt4.X, pt4.Y);
        }

        public bool IsVisible(Point pt)
        {
            return skPath.Contains(pt.X, pt.Y);
        }

        public void AddLine(float x1, float y1, float x2, float y2)
        {
            AddLine(new PointF(x1, y1), new PointF(x2, y2));
        }

        private static bool IsNear(float a, float b)
        {
            float v = a - b;
            return v >= -0.0001f && v <= 0.0001f;
        }

        public void AddLine(Point a, Point b)
        {
            SKPoint lastPt = skPath.LastPoint;
            if (lastPt.IsEmpty || !IsNear(lastPt.X, a.X) || !IsNear(lastPt.Y, a.Y))
                skPath.MoveTo(a.X, a.Y);
            skPath.LineTo(b.X, b.Y);
        }

        public void AddLine(PointF a, PointF b)
        {
            SKPoint lastPt = skPath.LastPoint;
            if (lastPt.IsEmpty || !IsNear(lastPt.X, a.X) || !IsNear(lastPt.Y, a.Y))
                skPath.MoveTo(a.X, a.Y);
            skPath.LineTo(b.X, b.Y);
        }

        public void AddLines(PointF[] points)
        {
            SKPoint lastPt = skPath.LastPoint;
            if (lastPt.IsEmpty || !IsNear(lastPt.X, points[0].X) || !IsNear(lastPt.Y, points[0].Y))
                skPath.MoveTo(points[0].X, points[0].Y);
            else
                skPath.LineTo(points[0].X, points[0].Y);

            for (int i = 1; i < points.Length; i++)
            {
                skPath.LineTo(points[i].X, points[i].Y);
            }
        }

        public void AddLines(Point[] points)
        {
            SKPoint lastPt = skPath.LastPoint;
            if (lastPt.IsEmpty || !IsNear(lastPt.X, points[0].X) || !IsNear(lastPt.Y, points[0].Y))
                skPath.MoveTo(points[0].X, points[0].Y);
            else
                skPath.LineTo(points[0].X, points[0].Y);

            for (int i = 1; i < points.Length; i++)
            {
                skPath.LineTo(points[i].X, points[i].Y);
            }
        }

        public void AddRectangle(Rectangle rect)
        {
            var r = Convert(rect);
            skPath.AddRect(r, SKPathDirection.Clockwise);
        }

        public void AddRectangle(RectangleF rect)
        {
            var r = Convert(rect);
            skPath.AddRect(r, SKPathDirection.Clockwise);
        }

        public void AddRectangles(Rectangle[] rects)
        {
            throw new NotImplementedException();
        }

        public void AddArc(RectangleF rect, float startAngle, float sweepAngle)
        {
            if (rect.IsEmpty || sweepAngle == 0)
            {
                return;
            }
            var r = Convert(rect);
            if (sweepAngle >= 360 || sweepAngle <= -360)
            {
                skPath.AddOval(r, sweepAngle > 0 ? SKPathDirection.Clockwise : SKPathDirection.CounterClockwise);
            }
            else
            {
                skPath.ArcTo(r, startAngle, sweepAngle, false);
            }
        }

        public void AddEllipse(Rectangle rect)
        {
            var r = Convert(rect);
            skPath.AddOval(r, SKPathDirection.Clockwise);
        }

        public void AddEllipse(RectangleF rect)
        {
            var r = Convert(rect);
            skPath.AddOval(r, SKPathDirection.Clockwise);
        }

        public void AddPolygon(PointF[] points)
        {
            skPath.MoveTo(points[0].X, points[0].Y);
            for (int i = 0; i < points.Length; i++)
            {
                skPath.LineTo(points[i].X, points[i].Y);
            }
            CloseFigure();
        }

        public void AddPolygon(Point[] points)
        {
            skPath.MoveTo(points[0].X, points[0].Y);
            for (int i = 0; i < points.Length; i++)
            {
                skPath.LineTo(points[i].X, points[i].Y);
            }
            CloseFigure();
        }

        public void AddPath(GraphicsPath other, bool connect)
        {
            skPath.AddPath(other.skPath, SKPathAddMode.Append);
        }

        public void CloseFigure()
        {
            skPath.Close();
        }

        public void CloseAllFigures()
        {
            //TODO: fix
            skPath.Close();
        }

        public void Transform(Matrix matrix)
        {
            var skMatrix = matrix.ToSKMatrix();
            skPath.Transform(skMatrix);
        }

        private SKRect Convert(Rectangle rect)
        {
            return new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        private SKRect Convert(RectangleF rect)
        {
            return new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        #region ====IDisposable Support====
        private bool disposedValue = false;

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (skPath != null)
                    {
                        skPath.Dispose();
                        skPath = null;
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