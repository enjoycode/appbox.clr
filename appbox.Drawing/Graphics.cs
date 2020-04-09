using System;
using SkiaSharp;

namespace appbox.Drawing
{
    public sealed class Graphics : IDisposable
    {
        #region ====Statics====
        public static Graphics FromImage(Image image, float scaleX = 1f, float scaleY = 1f)
        {
            if (image is Bitmap)
            {
                var bitmap = (Bitmap)image;
                var canvas = new SKCanvas(bitmap.skBitmap);
                return new Graphics(canvas, scaleX, scaleY, 72.0f, 72.0f);
            }
            throw new NotImplementedException();
        }

        public static Graphics FromCanvas(SKCanvas canvas)
        {
            return new Graphics(canvas, 1, 1, 72, 72);
        }
        #endregion

        #region ====Fields & Properties====
        private const float AA_OFFSET_X = 0.5f;
        private const float AA_OFFSET_Y = 0.5f;
        private SKCanvas skCanvas;
        private SKPaint skPaint = new SKPaint();
        private float deviceScaleX = 1;
        private float deviceScaleY = 1;
        private float deviceOffsetX = 0;
        private float deviceOffsetY = 0;
        private Rectangle deviceClip = Rectangle.Empty;

        private GraphicsUnit pageUnit = GraphicsUnit.Pixel;
        public GraphicsUnit PageUnit
        {
            get { return pageUnit; }
            set
            {
                if (pageUnit != value)
                {
                    var oldPx = GraphicsUnitConverter.Convert(pageUnit, GraphicsUnit.Pixel, 1, DpiX);
                    var newPx = GraphicsUnitConverter.Convert(value, GraphicsUnit.Pixel, 1, DpiX);
                    float scale = newPx / oldPx;
                    pageUnit = value;
                    deviceScaleX = scale * deviceScaleX;
                    deviceScaleY = scale * deviceScaleY;
                    skCanvas.Scale(scale, scale);
                }
            }
        }

        /// <summary>
        /// Graphics 支持的水平分辨率的值 以每英寸点数为单位
        /// </summary>
        public float DpiX { get; private set; } = 72.0f;

        /// <summary>
        /// Graphics 支持的垂直分辨率的值 以每英寸点数为单位
        /// </summary>
        public float DpiY { get; private set; } = 72.0f;

        public float DeviceScaleX
        {
            get { return deviceScaleX; }
            //set { deviceScaleX = value; }
        }

        public float DeviceScaleY
        {
            get { return deviceScaleY; }
            //set { deviceScaleY = value; }
        }

        //public float DeviceOffsetX
        //{
        //    get { return deviceOffsetX; }
        //    set { deviceOffsetX = value; }
        //}

        //public float DeviceOffsetY
        //{
        //    get { return deviceOffsetY; }
        //    set { deviceOffsetY = value; }
        //}

        public TextRenderingHint TextRenderingHint { get; set; }

        public SmoothingMode SmoothingMode { get; set; } = SmoothingMode.None;

        public Matrix Transform
        {
            get
            {
                SKMatrix cmatrix = skCanvas.TotalMatrix;
                var matrix = new Matrix(cmatrix);
                if (deviceOffsetX != 0 || deviceOffsetY != 0)
                    matrix.Translate(-deviceOffsetX, -deviceOffsetY); //TODO: test
                if (deviceScaleX != 1 || deviceScaleY != 1)
                    matrix.Scale(1.0f / deviceScaleX, 1.0f / deviceScaleY, MatrixOrder.Append);
                return matrix;
            }
            set
            {
                var matrix = value;
                if (deviceOffsetX != 0 || deviceOffsetY != 0)
                    matrix.Translate(deviceOffsetX, deviceOffsetY, MatrixOrder.Append);
                if (deviceScaleX != 1 || deviceScaleY != 1)
                    matrix.Scale(deviceScaleX, deviceScaleY, MatrixOrder.Append);
                var cmatrix = matrix.ToSKMatrix();
                skCanvas.SetMatrix(cmatrix);
            }
        }

        public RectangleF ClipBounds
        {
            get
            {
                SKRect rect = skCanvas.LocalClipBounds;
                return new RectangleF(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            }
        }

        public CompositingMode CompositingMode { get; set; } //todo:
        public CompositingQuality CompositingQuality { get; set; } //todo:
        public InterpolationMode InterpolationMode { get; set; } //todo;
        public PixelOffsetMode PixelOffsetMode { get; set; } //todo:

        #endregion

        #region ====Ctor & Dispose====
        private Graphics(SKCanvas canvas, float scaleX, float scaleY, float dpiX, float dpiY)
        {
            skCanvas = canvas;
            DpiX = dpiX;
            DpiY = dpiY;
            SetDeviceScale(scaleX, scaleY);
        }
        #endregion

        #region ====Methods====
        public void Save()
        {
            skCanvas.Save();
        }

        public void Restore()
        {
            skCanvas.Restore();
        }

        public void Clear(Color color)
        {
            skCanvas.Clear(new SKColor((uint)color.Value));
        }

        public void Flush()
        {
            skCanvas.Flush();
        }

        #region ----Matrix Methods----
        /// <summary>
        /// Internal use only.
        /// </summary>
        public void SetDeviceOffset(float offsetX, float offsetY)
        {
            deviceOffsetX = offsetX;
            deviceOffsetY = offsetY;
            skCanvas.Translate(offsetX, offsetY);
        }

        private void SetDeviceScale(float scaleX, float scaleY)
        {
            deviceScaleX = scaleX;
            deviceScaleY = scaleY;
            if (scaleX != 1 || scaleY != 1)
                skCanvas.Scale(scaleX, scaleY);
        }

        public void TranslateTransform(float dx, float dy)
        {
            skCanvas.Translate(dx, dy);
        }

        public void TranslateTransform(float dx, float dy, MatrixOrder order)
        {
            if (order == MatrixOrder.Prepend)
                skCanvas.Translate(dx, dy);
            else
                throw new NotImplementedException(); //todo:
        }

        public void RotateTransform(float angle)
        {
            skCanvas.RotateDegrees(angle);
        }

        public void ScaleTransform(float sx, float sy)
        {
            skCanvas.Scale(sx, sy);
        }

        public void ResetTransform()
        {
            skCanvas.ResetMatrix();
            //注意：需要处理device scale及device offset
            if (deviceScaleX != 1.0f || deviceScaleY != 1.0f)
                skCanvas.Scale(deviceScaleX, deviceScaleY);
            if (deviceOffsetX != 0.0f || deviceOffsetY != 0.0f)
                skCanvas.Translate(deviceOffsetX, deviceOffsetY);
        }
        #endregion

        #region ----Clip Methods----
        /// <summary>
        /// 仅内部使用
        /// </summary>
        //public void SetDeviceClip(Rectangle rect)
        //{
        //    deviceClip = rect;
        //    var skRect = Convert(rect);
        //    skCanvas.ClipRect(skRect, SKClipOperation.Replace, false);
        //}

        public void SetClip(RectangleF rect)
        {
            SetClip(rect, CombineMode.Replace);
        }

        public void SetClip(Rectangle rect, CombineMode combineMode)
        {
            SetClip(rect.ToRectangleF(), combineMode);
        }

        public void SetClip(RectangleF rect, CombineMode combineMode)
        {
            //if (combineMode == CombineMode.Replace && deviceClip != Rectangle.Empty)
            //{
            //    var skRect = Convert(deviceClip);
            //    SkiaApi.sk_canvas_clip_rect_with_operation(canvas, ref skRect, SKClipOperation.Replace, false);
            //    skRect = Convert(rect);
            //    SkiaApi.sk_canvas_clip_rect_with_operation(canvas, ref skRect, SKClipOperation.Intersect, false);
            //}
            //else
            //{
            //    //Console.WriteLine("rect={0} deviceClip={1} mode={2}", rect, deviceClip, combineMode);
            //    var skRect = Convert(rect);
            //    var skOp = ToSKClipOperation(combineMode);
            //    SkiaApi.sk_canvas_clip_rect_with_operation(canvas, ref skRect, skOp, false);
            //}
        }

        //public void SetClip(GraphicsPath path, CombineMode combineMode)
        //{
        //    SkiaApi.sk_canvas_clip_path_with_operation(canvas, path.nativePath, ToSKClipOperation(combineMode), true);
        //}

        //public void SetClip(Region region, CombineMode combineMode)
        //{
        //    SkiaApi.sk_canvas_clip_region(canvas, region.NativeRegion, ToSKClipOperation(combineMode));
        //}

        public void ExcludeClip(Rectangle rect)
        {
            throw new NotImplementedException();
        }

        public void ResetClip()
        {
            //todo:暂重设为超级大
            //SKRect rect = new SKRect(0f, 0f, float.MaxValue, float.MaxValue);
            //SkiaApi.sk_canvas_clip_rect_with_operation(canvas, ref rect, SKClipOperation.Replace, false);
        }
        #endregion

        #region ----Draw & Fill Rectangle Methods----
        public void DrawRectangle(Color color, float width, Rectangle rect)
        {
            DrawRectangle(color, width, rect.ToRectangleF());
        }

        public void DrawRectangle(Color color, float width, RectangleF paintRect)
        {
            if (SmoothingMode == SmoothingMode.AntiAlias)
            {
                //apply antialiasing offset (if required and if no scaling is in effect)
                paintRect.X += AA_OFFSET_X;
                paintRect.Y += AA_OFFSET_Y;
                var x2 = paintRect.X + paintRect.Width;
                var y2 = paintRect.Y + paintRect.Height;
                paintRect = new RectangleF(paintRect.X, paintRect.Y, x2 - paintRect.X, y2 - paintRect.Y);
            }
            var r = Convert(paintRect);
            skPaint.Color = new SKColor((uint)color.Value);
            skPaint.Style = SKPaintStyle.Stroke;
            skPaint.StrokeWidth = width;
            skCanvas.DrawRect(r, skPaint);
        }

        public void DrawRectangle(Pen pen, Rectangle rect)
        {
            DrawRectangle(pen, rect.ToRectangleF());
        }

        public void DrawRectangle(Pen pen, RectangleF paintRect)
        {
            if (SmoothingMode == SmoothingMode.AntiAlias || pen.Alignment == PenAlignment.Center) //todo:临时解决方法
            {
                //apply antialiasing offset (if required and if no scaling is in effect)
                if (Transform.Rotation <= 0)
                {
                    paintRect.X += AA_OFFSET_X;
                    paintRect.Y += AA_OFFSET_Y;
                }
                else
                {
                    if (paintRect.X > AA_OFFSET_X)
                        paintRect.X -= AA_OFFSET_X;
                    if (paintRect.Y > AA_OFFSET_Y)
                        paintRect.Y -= AA_OFFSET_Y;
                }

                var x2 = paintRect.X + paintRect.Width;
                var y2 = paintRect.Y + paintRect.Height;
                paintRect = new RectangleF(paintRect.X, paintRect.Y, x2 - paintRect.X, y2 - paintRect.Y);
            }
            var r = Convert(paintRect);
            pen.ApplyToSKPaint(skPaint);
            skCanvas.DrawRect(r, skPaint);
            skPaint.Reset();
        }

        public void FillRectangle(Color color, float x, float y, float width, float height)
        {
            FillRectangle(color, new RectangleF(x, y, width, height));
        }

        public void FillRectangle(Color color, Rectangle rect)
        {
            FillRectangle(color, new RectangleF(rect.X, rect.Y, rect.Width, rect.Height));
        }

        public void FillRectangle(Color color, RectangleF rect)
        {
            var r = Convert(rect);
            skPaint.Color = new SKColor((uint)color.Value);
            skPaint.Style = SKPaintStyle.Fill;
            skCanvas.DrawRect(r, skPaint);
        }

        public void FillRectangle(Brush brush, Rectangle rect)
        {
            var r = Convert(rect);
            brush.ApplyToSKPaint(skPaint);
            skCanvas.DrawRect(r, skPaint);
            skPaint.Reset();
        }

        public void FillRectangle(Brush brush, RectangleF rect)
        {
            var r = Convert(rect);
            brush.ApplyToSKPaint(skPaint);
            skCanvas.DrawRect(r, skPaint);
            skPaint.Reset();
        }

        public void FillRectangle(Brush brush, float x, float y, float width, float height)
        {
            FillRectangle(brush, new RectangleF(x, y, width, height));
        }
        #endregion

        #region ====Draw & Fill RoundRectangle Methods====
        public void DrawRoundRectangle(Color color, float penWidth, RectangleF rect, float rx, float ry)
        {
            skPaint.Color = new SKColor((uint)color.Value);
            skPaint.Style = SKPaintStyle.Stroke;
            skPaint.StrokeWidth = penWidth;
            skPaint.IsAntialias = true;
            var r = Convert(rect);
            skCanvas.DrawRoundRect(r, rx, ry, skPaint);
        }

        public void FillRoundRectangle(Color color, RectangleF rect, float rx, float ry)
        {
            skPaint.Color = new SKColor((uint)color.Value);
            skPaint.Style = SKPaintStyle.Fill;
            skPaint.IsAntialias = true;
            var r = Convert(rect);
            skCanvas.DrawRoundRect(r, rx, ry, skPaint);
        }
        #endregion

        #region ----Draw & Fill Ellipse Methods----
        public void DrawEllipse(Color color, float penWidth, Rectangle rect)
        {
            DrawEllipse(color, penWidth, new RectangleF(rect.X, rect.Y, rect.Width, rect.Height));
        }

        public void FillEllipse(Color color, Rectangle rect)
        {
            FillEllipse(color, new RectangleF(rect.X, rect.Y, rect.Width, rect.Height));
        }

        public void DrawEllipse(Pen pen, Rectangle rect)
        {
            DrawEllipse(pen, new RectangleF(rect.X, rect.Y, rect.Width, rect.Height));
        }

        public void DrawEllipse(Pen pen, RectangleF rect)
        {
            pen.ApplyToSKPaint(skPaint);
            skPaint.IsAntialias = true;
            var r = Convert(rect);
            skCanvas.DrawOval(r, skPaint);
        }

        public void DrawEllipse(Color color, float penWidth, RectangleF rect)
        {
            skPaint.Color = new SKColor((uint)color.Value);
            skPaint.Style = SKPaintStyle.Stroke;
            skPaint.StrokeWidth = penWidth;
            skPaint.IsAntialias = true;
            var r = Convert(rect);
            skCanvas.DrawOval(r, skPaint);
        }

        public void FillEllipse(Color color, RectangleF rect)
        {
            skPaint.Color = new SKColor((uint)color.Value);
            skPaint.Style = SKPaintStyle.Fill;
            skPaint.IsAntialias = true;
            var r = Convert(rect);
            skCanvas.DrawOval(r, skPaint);
        }

        public void FillEllipse(Brush brush, RectangleF rect)
        {
            brush.ApplyToSKPaint(skPaint);
            skPaint.IsAntialias = true;
            var r = Convert(rect);
            skCanvas.DrawOval(r, skPaint);
        }
        #endregion

        #region ----Draw & Fill Path Methods----
        public void DrawPath(Pen pen, GraphicsPath path)
        {
            pen.ApplyToSKPaint(skPaint);
            DrawPathInternal(path);
        }

        public void DrawPath(Color color, float width, GraphicsPath path)
        {
            skPaint.Color = new SKColor((uint)color.Value);
            skPaint.Style = SKPaintStyle.Stroke;
            skPaint.StrokeWidth = width;
            DrawPathInternal(path);
        }

        public void FillPath(Color color, GraphicsPath path)
        {
            skPaint.Color = new SKColor((uint)color.Value);
            skPaint.Style = SKPaintStyle.Fill;

            DrawPathInternal(path);
        }

        private void DrawPathInternal(GraphicsPath path)
        {
            if (SmoothingMode == SmoothingMode.AntiAlias)
            {
                skPaint.IsAntialias = true;
                //TODO:临时解决方法
                Matrix matrix;
                if (Transform.Rotation <= 0)
                    matrix = new Matrix(new PointF(AA_OFFSET_X, AA_OFFSET_Y));
                else
                    matrix = new Matrix(new PointF(-AA_OFFSET_X, -AA_OFFSET_Y));
                var cmatrix = matrix.ToSKMatrix();
                path.skPath.Transform(cmatrix);
                //SkiaApi.sk_canvas_translate(canvas, AA_OFFSET_X, AA_OFFSET_Y);
            }
            skCanvas.DrawPath(path.skPath, skPaint);

            if (SmoothingMode == SmoothingMode.AntiAlias)
            {
                //TODO:临时解决方法
                Matrix matrix;
                if (Transform.Rotation <= 0)
                    matrix = new Matrix(new PointF(-AA_OFFSET_X, -AA_OFFSET_Y));
                else
                    matrix = new Matrix(new PointF(AA_OFFSET_X, AA_OFFSET_Y));
                var cmatrix = matrix.ToSKMatrix();
                path.skPath.Transform(cmatrix);
                //SkiaApi.sk_canvas_translate(canvas, -AA_OFFSET_X, -AA_OFFSET_Y);
            }
            skPaint.Reset();
        }

        public void FillPath(Brush brush, GraphicsPath path)
        {
            brush.ApplyToSKPaint(skPaint);
            DrawPathInternal(path);
        }
        #endregion

        #region ----Draw & Fill Polygon Methods----
        public void FillPolygon(Brush brush, PointF[] points)
        {
            using var path = new GraphicsPath();
            path.AddLines(points);
            path.CloseFigure();
            FillPath(brush, path);
        }

        public void FillPolygon(Brush brush, Point[] points)
        {
            using var path = new GraphicsPath();
            path.AddLines(points);
            path.CloseFigure();
            FillPath(brush, path);
        }

        public void DrawPolygon(Color color, float width, PointF[] points)
        {
            using var path = new GraphicsPath();
            path.AddLines(points);
            path.CloseFigure();
            DrawPath(color, width, path);
        }

        public void FillPolygon(Color color, PointF[] points)
        {
            using var path = new GraphicsPath();
            path.AddLines(points);
            path.CloseFigure();
            FillPath(color, path);
        }
        #endregion

        #region ----Draw String & TextLayout Methods----
        public void DrawString(string str, Font font, Brush brush, float x, float y)
        {
            font.ApplyToSKPaint(skPaint, PageUnit, DpiX);
            brush.ApplyToSKPaint(skPaint);
            skPaint.TextEncoding = SKTextEncoding.Utf16;

            unsafe
            {
                fixed (char* ptr = str)
                {
                    SkiaApi.sk_canvas_draw_text(skCanvas.Handle, ptr, new IntPtr(str.Length * 2),
                        x, y + GraphicsUnitConverter.Convert(GraphicsUnit.Pixel, PageUnit, -font.FontMetrics.Ascent, DpiY),
                        skPaint.Handle);
                }
            }

            skPaint.Reset();
        }

        public void DrawString(string str, Font font, Color color, float x, float y)
        {
            font.ApplyToSKPaint(skPaint, PageUnit, DpiX);
            skPaint.Color = new SKColor((uint)color.Value);
            skPaint.Style = SKPaintStyle.Fill;
            skPaint.TextEncoding = SKTextEncoding.Utf16;
            unsafe
            {
                fixed (char* ptr = str)
                {
                    SkiaApi.sk_canvas_draw_text(skCanvas.Handle, ptr, new IntPtr(str.Length * 2),
                        x, y + GraphicsUnitConverter.Convert(GraphicsUnit.Pixel, PageUnit, -font.FontMetrics.Ascent, DpiY),
                        skPaint.Handle);
                }
            }

            skPaint.Reset();
        }

        public void DrawString(string str, Font font, Color color, RectangleF rect, StringFormat format)
        {
            //TODO: 暂用TextLayout实现
            var layout = new TextLayout(str, font, format, DpiX)
            {
                Width = GraphicsUnitConverter.Convert(PageUnit, GraphicsUnit.Pixel, rect.Width, DpiX),
                Height = GraphicsUnitConverter.Convert(PageUnit, GraphicsUnit.Pixel, rect.Height, DpiY)
            };

            DrawTextLayout(layout, color, rect.X, rect.Y);
        }

        public void DrawString(string str, Font font, Brush brush, RectangleF rect, StringFormat format)
        {
            if (brush is SolidBrush solidBrush)
                DrawString(str, font, solidBrush.Color, rect, format);
            else
                DrawString(str, font, Color.Black, rect, format); //TODO:
        }

        public unsafe void DrawTextLayout(TextLayout layout, Color color, float x, float y)
        {
            layout.Run();
            layout.Font.ApplyToSKPaint(skPaint, PageUnit, DpiX);
            skPaint.Color = new SKColor((uint)color.Value);
            skPaint.Style = SKPaintStyle.Fill;
            skPaint.TextEncoding = SKTextEncoding.Utf16;
            skPaint.IsAntialias = true;

            var cy = y + GraphicsUnitConverter.Convert(GraphicsUnit.Pixel, PageUnit, layout.OffsetY, DpiY);
            fixed (char* ptr = layout.Text)
            {
                for (int i = 0; i < layout.lines.Count; i++)
                {
                    cy += GraphicsUnitConverter.Convert(GraphicsUnit.Pixel, PageUnit, -layout.Font.FontMetrics.Ascent, DpiY);
                    var length = layout.lines[i].widths.Length;
                    SKPoint* pos = stackalloc SKPoint[length];
                    var cx = x + GraphicsUnitConverter.Convert(GraphicsUnit.Pixel, PageUnit, layout.lines[i].offsetX, DpiX);
                    for (int j = 0; j < length; j++)
                    {
                        pos[j].X = cx;
                        pos[j].Y = cy;
                        cx += GraphicsUnitConverter.Convert(GraphicsUnit.Pixel, PageUnit, layout.lines[i].widths[j], DpiX);
                    }

                    SkiaApi.sk_canvas_draw_pos_text(skCanvas.Handle, ((byte*)ptr) + layout.lines[i].startByteIndex,
                                                    new IntPtr(layout.lines[i].byteLength), pos, skPaint.Handle);

                    cy += GraphicsUnitConverter.Convert(GraphicsUnit.Pixel, PageUnit,
                        layout.Font.FontMetrics.Descent + layout.Font.FontMetrics.Leading, DpiX);
                }
            }

            //System.Diagnostics.Debug.WriteLine("Graphics.DrawTextLayout at {0},{1} with: {2}",x,y,layout.Text);
        }
        #endregion

        #region ----MeasureString Methods----
        public SizeF MeasureString(string text, Font font)
        {
            using var paint = new SKPaint();
            paint.TextEncoding = SKTextEncoding.Utf16;
            font.ApplyToSKPaint(paint, PageUnit, DpiX);

            SKRect bounds = SKRect.Empty;
            var width = paint.MeasureText(text, ref bounds);
            return new SizeF(width, font.GetHeight(this));
        }

        public SizeF MeasureString(string text, Font font, float maxWidth, StringFormat format)
        {
            return MeasureString(text, font, new SizeF(maxWidth, float.MaxValue), format);
        }

        public SizeF MeasureString(string text, Font font, SizeF maxSize, StringFormat format)
        {
            //TODO:暂用TextLayout来处理
            var layout = new TextLayout(text, font, format, DpiX);
            layout.Width = maxSize.Width;
            layout.Height = maxSize.Height;
            return layout.GetInkSize(); //TODO:单位转换处理
        }
        #endregion

        #region ----Draw Image Methods----
        //public void DrawImageUnscaledAndClipped(Image image, Rectangle rect)
        //{
        //    DrawImage(image, rect.ToRectangleF(), new RectangleF(0, 0, image.Width, image.Height));
        //}

        //public void DrawImage(Image image, Rectangle rect, int x, int y, int width, int height, GraphicsUnit unit)
        //{
        //    DrawImage(image, rect, new RectangleF(x, y, width, height));
        //}

        //public void DrawImage(Image image, Rectangle rect, int x, int y, int width, int height, GraphicsUnit unit, ImageAttributes attribute)
        //{
        //    //暂时忽略 ImageAttributes
        //    DrawImage(image, rect, new RectangleF(x, y, width, height));
        //}

        //public void DrawImage(Image image, int x, int y)
        //{
        //    if (image.HiDpi)
        //    {
        //        DrawImage(image, new Rectangle(x, y, image.Width, image.Height));
        //    }
        //    else
        //    {
        //        if (image is Bitmap)
        //        {
        //            SkiaApi.sk_canvas_draw_bitmap(canvas, ((Bitmap)image).NativeBitmap, x, y, this.nativePaint);
        //        }
        //    }
        //}

        //public void DrawImage(Image image, Point point)
        //{
        //    DrawImage(image, point.X, point.Y);
        //}

        ///// <summary>
        ///// Draws the specified Image at the specified location and with the specified size.
        ///// </summary>
        //public void DrawImage(Image image, Rectangle rect)
        //{
        //    DrawImage(image, new RectangleF(rect.X, rect.Y, rect.Width, rect.Height), new RectangleF(0, 0, image.Width, image.Height));
        //}

        ///// <summary>
        ///// Draws the specified Image at the specified location and with the specified size.
        ///// </summary>
        //public void DrawImage(Image image, RectangleF rect)
        //{
        //    DrawImage(image, rect, new RectangleF(0, 0, image.Width, image.Height));
        //}

        ///// <summary>
        ///// Draws the specified Image at the specified location and with the specified size.
        ///// </summary>
        //public void DrawImage(Image image, int x, int y, int width, int height)
        //{
        //    DrawImage(image, new RectangleF(x, y, width, height), new RectangleF(0, 0, image.Width, image.Height));
        //}

        //public void DrawImage(Image image, RectangleF dest, RectangleF src)
        //{
        //    if (image is Bitmap)
        //    {
        //        if (image.HiDpi)
        //            src = new RectangleF(src.X, src.Y, src.Width * 2, src.Height * 2);

        //        var nativeBitmap = ((Bitmap)image).NativeBitmap;
        //        if (nativeBitmap != IntPtr.Zero)
        //        {
        //            var dstRect = Convert(dest);
        //            var srcRect = Convert(src);
        //            SkiaApi.sk_paint_set_color(this.nativePaint, new SKColor(0x10, 0x10, 0x10)); //todo:: 临时解决RichTextEditor图片绘制看不见现象
        //            SkiaApi.sk_canvas_draw_bitmap_rect(canvas, nativeBitmap, ref srcRect, ref dstRect, this.nativePaint);
        //        }
        //        else
        //        {
        //            Log.Debug("Bitmap.NativeHandle is IntPtr.Zero");
        //        }
        //    }
        //}
        #endregion

        #region ----Draw Line Methods----
        public void DrawLine(Color color, float width, float x0, float y0, float x1, float y1)
        {
            skPaint.Color = new SKColor((uint)color.Value);
            skPaint.Style = SKPaintStyle.Stroke;
            skPaint.StrokeWidth = width;
            skCanvas.DrawLine(x0, y0, x1, y1, skPaint);
        }

        public void DrawLine(Pen pen, float x0, float y0, float x1, float y1)
        {
            pen?.ApplyToSKPaint(skPaint);
            skCanvas.DrawLine(x0, y0, x1, y1, skPaint);
            skPaint.Reset();
        }
        #endregion

        #endregion

        //public void DrawGlphy(byte[] bytes, int bytesCount, int x, int y, Font font)
        //{
        //    SkiaApi.sk_paint_set_style(nativePaint, SKPaintStyle.Fill);
        //    font.ApplyToPaint(this.nativePaint, this.PageUnit, this.DpiX);
        //    SkiaApi.sk_paint_set_text_encoding(this.nativePaint, SKTextEncoding.Utf16);
        //    SkiaApi.sk_canvas_draw_text(this.canvas, bytes, bytesCount, x, y, this.nativePaint);
        //    SkiaApi.sk_paint_reset(nativePaint);
        //}

        //public void DrawGlphyTest(byte[] bytes, int bytesCount, int x, int y, Font font)
        //{
        //    //SkiaApi.sk_paint_set_style(nativePaint, SKPaintStyle.Fill);
        //    //SkiaApi.sk_paint_reset(nativePaint);
        //    String msg = "f";
        //    byte[] buffer = new byte[200];
        //    unsafe
        //    {
        //        fixed (char* ptr = msg)
        //        {
        //            fixed (byte* p = buffer)
        //            {
        //                bytesCount = SkiaApi.sk_typeface_chars_to_glyphs(font.GetNativeFontFace, (IntPtr)ptr, SKEncoding.Utf16, (IntPtr)p, 200);
        //            }
        //        }
        //    }
        //    font.ApplyToPaint(this.nativePaint, this.PageUnit, this.DpiX);
        //    SkiaApi.sk_paint_set_text_encoding(this.nativePaint, SKTextEncoding.GlyphId);
        //    SkiaApi.sk_canvas_draw_text(this.canvas, buffer, bytesCount, x, y, this.nativePaint);
        //}

        #region ====Static Converters====
        private static SKClipOperation ToSKClipOperation(CombineMode mode)
        {
            return mode switch
            {
                //CombineMode.Replace => SKClipOperation.Replace,
                CombineMode.Intersect => SKClipOperation.Intersect,
                //CombineMode.Union => SKClipOperation.Union,
                //CombineMode.Xor => SKClipOperation.XOR,
                CombineMode.Exclude => SKClipOperation.Difference,
                //CombineMode.Complement => SKClipOperation.ReverseDifference,
                _ => throw new Exception("Unkonw CombineMode value"),
            };
        }

        //private static SKRegionOperation ToSKRegionOperation(CombineMode mode)
        //{
        //    switch (mode)
        //    {
        //        case CombineMode.Replace:
        //            return SKRegionOperation.Replace;
        //        case CombineMode.Intersect:
        //            return SKRegionOperation.Intersect;
        //        case CombineMode.Union:
        //            return SKRegionOperation.Union;
        //        case CombineMode.Xor:
        //            return SKRegionOperation.XOR;
        //        case CombineMode.Exclude:
        //            return SKRegionOperation.Difference;
        //        case CombineMode.Complement:
        //            return SKRegionOperation.ReverseDifference;
        //        default:
        //            throw new Exception("Unkonw CombineMode value");
        //    }
        //}

        private static SKRect Convert(Rectangle rect)
        {
            return new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        private static SKRect Convert(RectangleF rect)
        {
            return new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }
        #endregion

        #region ====IDisposable Support====
        private bool disposedValue = false;

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (skPaint != null)
                    {
                        skPaint.Dispose();
                        skPaint = null;
                    }
                    if (skCanvas != null)
                    {
                        skCanvas.Dispose();
                        skCanvas = null;
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose() => Dispose(true);
        #endregion

    }
}