using System;
using SkiaSharp;

namespace appbox.Drawing
{
    /// <summary>
    /// Represents a light-weight 3*3 Matrix to be used for GDI+ transformations.
    /// </summary>
    public sealed class Matrix
    {
        #region Static Members

        public static readonly Matrix Identity = new Matrix(1F, 0F, 0F, 1F, 0F, 0F);
        public static readonly Matrix Empty = new Matrix(0F, 0F, 0F, 0F, 0F, 0F);
        public const float PI = 3.141593F;
        public const float TwoPI = PI * 2F;
        public const float RadianToDegree = (float)(180D / PI);
        public const float DegreeToRadian = (float)(PI / 180D);

        #endregion

        #region Fields

        public float DX { get; private set; }
        public float DY { get; private set; }
        public float M11 { get; private set; }
        public float M12 { get; private set; }
        public float M21 { get; private set; }
        public float M22 { get; private set; }

        #endregion

        #region Constructors

        public Matrix()
        {
            this.DX = Identity.DX;
            this.DY = Identity.DY;
            this.M11 = Identity.M11;
            this.M12 = Identity.M12;
            this.M21 = Identity.M21;
            this.M22 = Identity.M22;
        }

        /// <summary>
        /// Initializes a new Matrix, using the specified parameters.
        /// </summary>
        /// <param name="m11"></param>
        /// <param name="m12"></param>
        /// <param name="m21"></param>
        /// <param name="m22"></param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        public Matrix(float m11, float m12, float m21, float m22, float dx, float dy)
        {
            this.M11 = m11;
            this.M12 = m12;
            this.M21 = m21;
            this.M22 = m22;
            this.DX = dx;
            this.DY = dy;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="source"></param>
        public Matrix(Matrix source)
        {
            this.M11 = source.M11;
            this.M12 = source.M12;
            this.M21 = source.M21;
            this.M22 = source.M22;
            this.DX = source.DX;
            this.DY = source.DY;
        }

        internal Matrix(SKMatrix source)
        {
            this.M11 = source.ScaleX;
            this.M12 = source.SkewY;
            this.M21 = source.SkewX;
            this.M22 = source.ScaleY;
            this.DX = source.TransX;
            this.DY = source.TransY;
        }

        // /// <summary>
        // /// Initializes a new Matrix, using the elements of the specified GDI+ Matrix instance.
        // /// </summary>
        // /// <param name="gdiMatrix"></param>
        // public Matrix(Matrix gdiMatrix)
        // {
        //     float[] elements = gdiMatrix.Elements;
        //     this.M11 = elements[0];
        //     this.M12 = elements[1];
        //     this.M21 = elements[2];
        //     this.M22 = elements[3];
        //     this.DX = elements[4];
        //     this.DY = elements[5];
        // }

        /// <summary>
        /// Initializes a new Matrix, applying the specified X and Y values as DX and DY members of the matrix.
        /// </summary>
        /// <param name="offset"></param>
        public Matrix(PointF offset)
        {
            this.M11 = 1F;
            this.M12 = 0F;
            this.M21 = 0F;
            this.M22 = 1F;
            this.DX = offset.X;
            this.DY = offset.Y;
        }

        /// <summary>
        /// Initializes a new Matrix, scaling it by the provided parameters, at the origin (0, 0).
        /// </summary>
        /// <param name="scaleX"></param>
        /// <param name="scaleY"></param>
        public Matrix(float scaleX, float scaleY)
            : this(scaleX, scaleY, PointF.Empty)
        {
        }

        /// <summary>
        /// Initializes a new Matrix, scaling it by the provided parameters, at the specified origin.
        /// </summary>
        /// <param name="scaleX"></param>
        /// <param name="scaleY"></param>
        /// <param name="origin"></param>
        public Matrix(float scaleX, float scaleY, PointF origin)
        {
            this.M11 = scaleX;
            this.M12 = 0F;
            this.M21 = 0F;
            this.M22 = scaleY;
            this.DX = origin.X - (scaleX * origin.X);
            this.DY = origin.Y - (scaleY * origin.Y);
        }

        /// <summary>
        /// Initializes a new Matrix, rotated by the specified angle (in degrees) at origin (0, 0).
        /// </summary>
        /// <param name="angle"></param>
        public Matrix(float angle)
            : this(angle, PointF.Empty)
        {
        }

        /// <summary>
        /// Initializes a new Matrix, rotated by the specified angle (in degrees) at the provided origin.
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="origin"></param>
        public Matrix(float angle, PointF origin)
        {
            if (angle == 0F || angle == 360F)
            {
                this.DX = Identity.DX;
                this.DY = Identity.DY;
                this.M11 = Identity.M11;
                this.M12 = Identity.M12;
                this.M21 = Identity.M21;
                this.M22 = Identity.M22;
            }
            else
            {
                float cos;
                float sin;
                Matrix.GetCosSin(angle, out cos, out sin);

                this.M11 = cos;
                this.M12 = sin;
                this.M21 = -sin;
                this.M22 = cos;
                //calculate DX and DY
                if (origin != PointF.Empty)
                {
                    float x = origin.X;
                    float y = origin.Y;
                    this.DX = x - (cos * x) + (sin * y);
                    this.DY = y - (cos * y) - (sin * x);
                }
                else
                {
                    this.DX = 0F;
                    this.DY = 0F;
                }
            }
        }

        private static void GetCosSin(float angle, out float cos, out float sin)
        {
            //normalize angle - e.g. -90 = 270
            if (angle < 0)
            {
                angle = 360 + angle;
            }

            //handle special cases to eliminate floating-point approximation errors
            if (angle == 90F)
            {
                cos = 0F;
                sin = 1F;
            }
            else if (angle == 180)
            {
                cos = -1F;
                sin = 0F;
            }
            else if (angle == 270F)
            {
                cos = 0F;
                sin = -1;
            }
            else
            {
                float angleInRadians = angle * Matrix.DegreeToRadian;
                cos = (float)Math.Cos(angleInRadians);
                sin = (float)Math.Sin(angleInRadians);
            }
        }

        #endregion

        #region Matrix Operations

        internal SKMatrix ToSKMatrix()
        {
            var nativeMatrix = new SKMatrix();

            nativeMatrix.ScaleX = this.M11;  //mat.0
            nativeMatrix.SkewY = this.M12;  //mat.3
            nativeMatrix.SkewX = this.M21;   //mat.1
            nativeMatrix.ScaleY = this.M22; //mat.4
            nativeMatrix.TransX = this.DX; //mat.2
            nativeMatrix.TransY = this.DY; //mat.5
            nativeMatrix.Persp0 = 0;
            nativeMatrix.Persp1 = 0;
            nativeMatrix.Persp2 = 1;

            return nativeMatrix;
        }

        public void Scale(float scaleX, float scaleY)
        {
            this.Scale(scaleX, scaleY, MatrixOrder.Prepend);
        }

        public void Scale(float scaleX, float scaleY, MatrixOrder order)
        {
            this.Multiply(new Matrix(scaleX, scaleY), order);
        }

        public void Rotate(float angle)
        {
            this.Rotate(angle, MatrixOrder.Prepend);
        }

        public void Rotate(float angle, MatrixOrder order)
        {
            this.Multiply(new Matrix(angle), order);
        }

        public void RotateAt(float angle, PointF origin)
        {
            this.RotateAt(angle, origin, MatrixOrder.Prepend);
        }

        public void RotateAt(float angle, PointF origin, MatrixOrder order)
        {
            if (angle != 0F)
            {
                this.Multiply(new Matrix(angle, origin), order);
            }
        }

        public void Translate(float dx, float dy)
        {
            this.Translate(dx, dy, MatrixOrder.Prepend);
        }

        public void Translate(float dx, float dy, MatrixOrder order)
        {
            this.Multiply(new Matrix(new PointF(dx, dy)), order);
        }

        public void Multiply(Matrix m)
        {
            this.Multiply(m, MatrixOrder.Prepend);
        }

        public void Multiply(Matrix m, MatrixOrder order)
        {
            Matrix res = null;
            if (order == MatrixOrder.Append)
                res = this * m;
            else
                res = m * this;

            this.DX = res.DX;
            this.DY = res.DY;
            this.M11 = res.M11;
            this.M12 = res.M12;
            this.M21 = res.M21;
            this.M22 = res.M22;
        }

        public Matrix Clone()
        {
            var res = new Matrix();
            res.DX = this.DX;
            res.DY = this.DY;
            res.M11 = this.M11;
            res.M12 = this.M12;
            res.M21 = this.M21;
            res.M22 = this.M22;
            return res;
        }

        public void Divide(Matrix m)
        {
            m.Invert();
            this.Multiply(m, MatrixOrder.Prepend);
        }

        public void Invert()
        {
            if (this.IsIdentity)
            {
                return;
            }

            float determinant = this.Determinant;
            if (determinant == 0F)
            {
                //nothing to invert, make us empty
                //this = Empty;
                this.DX = Empty.DX;
                this.DY = Empty.DY;
                this.M11 = Empty.M11;
                this.M12 = Empty.M12;
                this.M21 = Empty.M21;
                this.M22 = Empty.M22;
                return;
            }

            float m11 = this.M22 / determinant;
            float m12 = -this.M12 / determinant;
            float m21 = -this.M21 / determinant;
            float m22 = this.M11 / determinant;
            float dx = (this.DX * -m11) - (this.DY * m21);
            float dy = (this.DX * -m12) - (this.DY * m22);

            Matrix res = new Matrix(m11, m12, m21, m22, dx, dy);
            this.DX = res.DX;
            this.DY = res.DY;
            this.M11 = res.M11;
            this.M12 = res.M12;
            this.M21 = res.M21;
            this.M22 = res.M22;
        }

        public void Reset()
        {
            //this = Identity;
            this.DX = Identity.DX;
            this.DY = Identity.DY;
            this.M11 = Identity.M11;
            this.M12 = Identity.M12;
            this.M21 = Identity.M21;
            this.M22 = Identity.M22;
        }

        #endregion

        #region Transformation Methods

        public PointF TransformPoint(PointF point)
        {
            float x = point.X;
            float y = point.Y;

            return new PointF((x * this.M11 + y * this.M21 + this.DX), (x * this.M12 + y * this.M22 + this.DY));
        }

        public void TransformPoints(PointF[] points)
        {
            int length = points.Length;
            for (int i = 0; i < length; i++)
            {
                points[i] = this.TransformPoint(points[i]);
            }
        }

        public RectangleF TransformRectangle(RectangleF bounds)
        {
            PointF topLeft = bounds.Location;
            PointF bottomRight = new PointF(topLeft.X + bounds.Width, topLeft.Y + bounds.Height);

            topLeft = this.TransformPoint(topLeft);
            bottomRight = this.TransformPoint(bottomRight);

            return new RectangleF(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
        }

        #endregion

        #region Helper Methods

        public bool Equals(Matrix gdiMatrix)
        {
            return this.Equals(gdiMatrix.Elements);
        }

        public bool Equals(float[] elements)
        {
            if (elements.Length != 6)
            {
                throw new ArgumentException("Invalid float array to compare to.");
            }

            return this.M11 == elements[0] &&
            this.M12 == elements[1] &&
            this.M21 == elements[2] &&
            this.M22 == elements[3] &&
            this.DX == elements[4] &&
            this.DY == elements[5];
        }

        public static float PointsDistance(PointF pt1, PointF pt2)
        {
            double distX = pt2.X - pt1.X;
            double distY = pt2.Y - pt1.Y;

            return (float)Math.Sqrt((distX * distX) + (distY * distY));
        }

        #endregion

        #region Operators

        public static Matrix operator *(Matrix a, Matrix b)
        {
            if (a == null || b == null)
                throw new ArgumentNullException();

            return new Matrix((a.M11 * b.M11) + (a.M12 * b.M21),
                (a.M11 * b.M12) + (a.M12 * b.M22),
                (a.M21 * b.M11) + (a.M22 * b.M21),
                (a.M21 * b.M12) + (a.M22 * b.M22),
                ((a.DX * b.M11) + (a.DY * b.M21)) + b.DX,
                ((a.DX * b.M12) + (a.DY * b.M22)) + b.DY);
        }
        #endregion

        #region Overrides

        public override int GetHashCode()
        {
            return this.M11.GetHashCode() ^
            this.M12.GetHashCode() ^
            this.M21.GetHashCode() ^
            this.M22.GetHashCode() ^
            this.DX.GetHashCode() ^
            this.DY.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var b = obj as Matrix;
            if (b == null)
                return false;

            return this.M11 == b.M11 &&
            this.M12 == b.M12 &&
            this.M21 == b.M21 &&
            this.M22 == b.M22 &&
            this.DX == b.DX &&
            this.DY == b.DY;
        }

        public override string ToString()
        {
            return "Matrix: Offset [" + this.DX + ", " + this.DY + "]";
        }

        #endregion

        #region Properties

        /// <summary>
        /// Determines whether the current matrix is empty.
        /// </summary>
        public bool IsEmpty
        {
            get { return this.Equals(Matrix.Empty); }
        }

        /// <summary>
        /// Determines whether this matrix equals to the Identity one.
        /// </summary>
        public bool IsIdentity
        {
            get { return this.Equals(Matrix.Identity); }
        }

        /// <summary>
        /// Gets the determinant - [(M11 * M22) - (M12 * M21)] - of this Matrix.
        /// </summary>
        public float Determinant
        {
            get
            {
                return (this.M11 * this.M22) - (this.M12 * this.M21);
            }
        }

        /// <summary>
        /// Determines whether this matrix may be inverted. That is to have non-zero determinant.
        /// </summary>
        public bool IsInvertible
        {
            get { return this.Determinant != 0F; }
        }

        /// <summary>
        /// Gets the scale by the X axis, provided by this matrix.
        /// </summary>
        public float ScaleX
        {
            get
            {
                PointF pt1 = this.TransformPoint(PointF.Empty);
                PointF pt2 = this.TransformPoint(new PointF(1F, 0F));

                return Matrix.PointsDistance(pt1, pt2);
            }
        }

        /// <summary>
        /// Gets the scale by the Y axis, provided by this matrix.
        /// </summary>
        public float ScaleY
        {
            get
            {
                PointF pt1 = this.TransformPoint(PointF.Empty);
                PointF pt2 = this.TransformPoint(new PointF(0F, 1F));

                return Matrix.PointsDistance(pt1, pt2);
            }
        }

        /// <summary>
        /// Gets the rotation (in degrees) applied to this matrix.
        /// </summary>
        public float Rotation
        {
            get
            {
                double angleInRadians = Math.Atan2(this.M12, this.M11);
                return (float)(angleInRadians * Matrix.RadianToDegree);
            }
        }

        /// <summary>
        /// Gets all the six fields of the matrix as an array.
        /// </summary>
        public float[] Elements
        {
            get
            {
                return new float[] { this.M11, this.M12, this.M21, this.M22, this.DX, this.DY };
            }
        }

        public float OffsetX
        {
            get { return this.DX; }
        }

        public float OffsetY
        {
            get { return this.DY; }
        }
        #endregion
    }

    public enum MatrixOrder
    {
        Append = 1,
        Prepend = 0
    }
}

