using System;
using SkiaSharp;

namespace appbox.Drawing
{
	public sealed class LinearGradientBrush : Brush
	{
		#region Fileds
		private PointF point1;
		private PointF point2;
		private ColorBlend presetColors;
        private float angle;
		private bool isAngleScalable;
		private Matrix _matrix = new Matrix();
		internal SKShader skShader;

		public WrapMode WrapMode { get; set; }

		public ColorBlend InterpolationColors
		{
			get { return presetColors; }
			set
			{
				presetColors = value;

				if (skShader != null)
				{
					skShader.Dispose();
					skShader = null;
				}
			}
		}
		#endregion

		public LinearGradientBrush(RectangleF rect, Color c1, Color c2, LinearGradientMode mode)
            : this(rect, c1, c2, GetAngleFromLinearGradientMode(mode))
		{ }

		public LinearGradientBrush(RectangleF rect, Color c1, Color c2, float angle,
            bool isAngleScalable = false, WrapMode wrapMode = WrapMode.Tile) : base()
		{
			if (rect.Width == 0 || rect.Height == 0)
			{
				throw new Exception("LinearGradient.Argument rect");
			}

			WrapMode = wrapMode;
			presetColors = new ColorBlend(2);
			presetColors.Colors[0] = c1;
			presetColors.Colors[1] = c2;

			this.angle = angle;
			this.isAngleScalable = isAngleScalable;
			point1 = new PointF();
			point2 = new PointF();
			point1.X = rect.X;
			point1.Y = rect.Y;
			point2.X = rect.X + rect.Width + 1;
			point2.Y = rect.Y;

			SetupInitialMatrix(rect); //gdip_linear_gradient_setup_initial_matrix  
		}

        protected override void DisposeSKObject()
        {
            if (skShader != null)
            {
				skShader.Dispose();
				skShader = null;
            }
        }

        private void SetupInitialMatrix(RectangleF rectf)
		{
			float cosAngle, sinAngle, absCosAngle, absSinAngle;
			float transX, transY, wRatio, hRatio;
			//var slope, rectRight, rectBottom: Float

			float dangle = (float)((angle % 360f) * (Math.PI / 180f));
			cosAngle = (float)Math.Cos(dangle);
			sinAngle = (float)Math.Sin(dangle);
			absCosAngle = (float)Math.Abs(cosAngle);
			absSinAngle = (float)Math.Abs(sinAngle);

			// this._matrix.init_identity();

			transX = rectf.X + (rectf.Width / 2.0f);
			transY = rectf.Y + (rectf.Height / 2.0f);

			wRatio = (absCosAngle * rectf.Width + absSinAngle * rectf.Height) / rectf.Width;
			hRatio = (absSinAngle * rectf.Width + absCosAngle * rectf.Height) / rectf.Height;

			_matrix.Translate(transX, transY);
			_matrix.Rotate(angle);
			_matrix.Scale(wRatio, hRatio);
			_matrix.Translate(-transX, -transY);
			/*
                    if isAngleScalable && !GraphicsHelper.NearZero(cosAngle) && !GraphicsHelper.NearZero(sinAngle) {
                        PointF[] pts = new PointF[3];
                        rectRight = rectf.X + rectf.Width;
                        rectBottom = rectf.Y + rectf.Height;
                        pts[0].X = rectf.X;
                        pts[0].Y = rectf.Y;
                        pts[1].X = rectRight;
                        pts[1].Y = rectf.Y;
                        pts[2].X = rectf.X;
                        pts[2].Y = rectBottom;

                        GraphicsHelper.TransformMatrixPoints(this._matrix, pts);
                        if (sinAngle > 0 && cosAngle > 0) {
                            slope = (float)(-1.0f / ((rectf.Width / rectf.Height) * Math.TanF(this.angle)));
                            pts[0].Y = (slope * (pts[0].X - rectf.X)) + rectf.Y;
                            pts[1].X = ((pts[1].Y - rectBottom) / slope) + rectRight;
                            pts[2].X = ((pts[2].Y - rectf.Y) / slope) + rectf.X;
                        }else if (sinAngle > 0 && cosAngle < 0) {
                            slope = (float)(-1.0f / ((rectf.Width / rectf.Height) * Math.TanF(this.angle - (float)(Math.PI) / 2f)));
                            pts[0].X = ((pts[0].Y - rectBottom) / slope) + rectRight;
                            pts[1].Y = (slope * (pts[1].X - rectRight)) + rectBottom;
                            pts[2].Y = (slope * (pts[2].X - rectf.X)) + rectf.Y;
                        }
                        else if (sinAngle < 0 && cosAngle < 0)
                        {
                            slope = (float)(-1.0f / (((rectf.Width / rectf.Height) * Math.TanF(this.angle))));
                            pts[0].Y = (slope * (pts[0].X - rectRight)) + rectBottom;
                            pts[1].X = ((pts[1].Y - rectf.Y) / slope) + rectf.X;
                            pts[2].X = ((pts[2].Y - rectBottom) / slope) + rectRight;
                        }
                        else
                        {
                            slope = (float)(-1.0f / ((rectf.Width / rectf.Height) * Math.TanF(this.angle - 3 * (float)(Math.PI) / 2)));
                            pts[0].X = ((pts[0].Y - rectf.Y) / slope) + rectf.X;
                            pts[1].Y = (slope * (pts[1].X - rectf.X)) + rectf.Y;
                            pts[2].Y = (slope * (pts[2].X - rectRight)) + rectBottom;
                        }

                        GraphicsHelper.MatrixInitFromRect3Points(&this._matrix, rectf, pts);
                    }
             */
		}

        private static float GetAngleFromLinearGradientMode(LinearGradientMode mode)
		{
			switch (mode)
			{
				case LinearGradientMode.Vertical:
					return 90.0f;
				case LinearGradientMode.ForwardDiagonal:
					return 45.0f;
				case LinearGradientMode.BackwardDiagonal:
					return 135.0f;
				case LinearGradientMode.Horizontal:
					break;
			}
			return 0;
		}

		internal override void ApplyToSKPaint(SKPaint skPaint)
		{
			if (skShader == null)
			{
				//SKPoint* points = stackalloc SKPoint[2];
				//points[0].X = point1.X;
				//points[0].Y = point1.Y;
				//points[1].X = point2.X;
				//points[1].Y = point2.Y;

				var colors = new SKColor[presetColors.Colors.Length];
				var colorPos = new float[presetColors.Colors.Length];
				//SKColor* colors = stackalloc SKColor[presetColors.Colors.Length];
				//float* colorPos = stackalloc float[presetColors.Colors.Length];
				for (int i = 0; i < presetColors.Colors.Length; i++)
				{
					colors[i] = new SKColor((uint)presetColors.Colors[i].Value);
					colorPos[i] = presetColors.Positions[i];
				}

				var cmatrix = _matrix.ToSKMatrix();
				//nativeShader = SkiaApi.sk_shader_new_linear_gradient(points, colors, colorPos,
				//													 presetColors.Colors.Length,
				//													 SKShaderTileMode.Clamp, ref cmatrix);
				skShader = SKShader.CreateLinearGradient(new SKPoint(point1.X, point1.Y),
					new SKPoint(point2.X, point2.Y), colors, colorPos, SKShaderTileMode.Clamp, cmatrix);
			}
			skPaint.Shader = skShader;
		}
	}

	public enum WrapMode
	{
		Tile,
		TileFlipX,
		TileFlipY,
		TileFlipXY,
		Clamp
	}

	public enum LinearGradientMode
	{
		Horizontal,
		Vertical,
		ForwardDiagonal,
		BackwardDiagonal
	}
}