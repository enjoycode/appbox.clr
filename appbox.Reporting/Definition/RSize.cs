
using System;
using System.Xml;
using appbox.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;


namespace appbox.Reporting.RDL
{
    ///<summary>
    /// The Size definition.  Held in a normalized format but convertible to multiple measurements.
    ///</summary>
    [Serializable]
    public class RSize
    {

        internal const decimal PARTS_PER_INCH = 2540;   //25.4 mm/inch
        internal const decimal PARTS_PER_CM = 1000;     //10 mm/cm
        internal const decimal PARTS_PER_MM = 100;
        internal const decimal PARTS_PER_POINT = PARTS_PER_INCH / Utility.Measurement.POINTSIZE_M;
        internal const decimal PARTS_PER_PICA = PARTS_PER_POINT * 12M;

        internal RSize(ReportDefn r, string t)
        {
            // Size is specified in CSS Length Units
            // format is <decimal number nnn.nnn><optional space><unit>
            // in -> inches (1 inch = 2.54 cm)
            // cm -> centimeters (.01 meters)
            // mm -> millimeters (.001 meters)
            // pt -> points (1 point = 1/72.27 inches)
            // pc -> Picas (1 pica = 12 points)
            Original = t;                   // Save original string for recreation
            t = t.Trim();
            int space = t.LastIndexOf(' ');
            string n;                       // number string
            string u;                       // unit string
            decimal d;                      // initial number
            try     // Convert.ToDecimal can be very picky
            {
                if (space != -1)    // any spaces
                {
                    n = t.Substring(0, space).Trim();   // number string
                    u = t.Substring(space).Trim();  // unit string
                }
                else if (t.Length >= 3)
                {
                    n = t.Substring(0, t.Length - 2).Trim();
                    u = t.Substring(t.Length - 2).Trim();
                }
                else
                {
                    // Illegal unit
                    if (r != null)
                        r.rl.LogError(4, string.Format("Illegal size '{0}' specified, assuming 0 length.", t));
                    Size = 0;
                    return;
                }
                if (!Regex.IsMatch(n, @"\A[ ]*[-]?[0-9]*[.]?[0-9]*[ ]*\Z"))
                {
                    r.rl.LogError(4, string.Format("Unknown characters in '{0}' specified.  Number must be of form '###.##'.  Local conversion will be attempted.", t));
                    d = Convert.ToDecimal(n, NumberFormatInfo.CurrentInfo);     // initial number
                }
                else
                    d = Convert.ToDecimal(n, NumberFormatInfo.InvariantInfo);       // initial number
            }
            catch (Exception ex)
            {
                // Illegal unit
                if (r != null)
                    r.rl.LogError(4, "Illegal size '" + t + "' specified, assuming 0 length.\r\n" + ex.Message);
                Size = 0;
                return;
            }

            switch (u)          // convert to millimeters
            {
                case "in": //Inches
                    Size = (int)(d * PARTS_PER_INCH);
                    break;
                case "cm": //Centimeters
                    Size = (int)(d * PARTS_PER_CM);
                    break;
                case "mm": //Millimeters
                    Size = (int)(d * PARTS_PER_MM);
                    break;
                case "pt": //Points
                    Size = (int)(d * PARTS_PER_POINT);
                    break;
                case "pc": //Picas
                    Size = (int)(d * PARTS_PER_PICA);
                    break;
                default:
                    // Illegal unit
                    if (r != null)
                        r.rl.LogError(4, "Unknown sizing unit '" + u + "' specified, assuming inches.");
                    Size = (int)(d * PARTS_PER_INCH);
                    break;
            }
            if (Size > 160 * 2540)  // Size can't be greater than 160 inches according to spec
            {   // but RdlEngine supports higher values so just do a warning
                if (r != null)
                    r.rl.LogError(4, "Size '" + this.Original + "' is larger than the RDL specification maximum of 160 inches.");
                //				_Size = 160 * 2540;     // this would force maximum to spec max of 160
            }
        }

        static public float PointSize(string v)
        {
            RSize rs = new RSize(null, v);
            return rs.Points;
        }

        internal RSize(int normalizedSize)
        {
            Size = normalizedSize;
        }

        internal RSize(ReportDefn r, XmlNode xNode) : this(r, xNode.InnerText)
        {
        }

        /// <summary>
        /// Normalized size in 1/100,000 meters
        /// </summary>
        internal int Size { get; set; }

        // Return value as if specified as px
        internal int PixelsX
        {
            get
            {   // For now assume 96 dpi;  TODO: what would be better way; shouldn't use server display pixels
                decimal p = Size;
                p = p / 2540m;      // get it in inches
                p = p * 96;				// 
                if (p != 0)
                    return (int)p;
                return (Size > 0) ? 1 : (int)p;
            }
        }

        static internal readonly float POINTSIZED = 72.27f;
        static internal readonly decimal POINTSIZEM = 72.27m;

        static internal int PixelsFromPoints(float x)
        {
            int result = (int)(x * 96 / POINTSIZED);	// convert to pixels
            if (result == 0 && x > .0001f)
                return 1;
            return result;
        }

        static internal int PixelsFromPoints(Graphics g, float x)
        {
            int result = (int)(x * g.DpiX / POINTSIZED);	// convert to pixels
            if (result == 0 && x > .0001f)
                return 1;

            return result;
        }

        internal int PixelsY
        {
            get
            {   // For now assume 96 dpi
                decimal p = Size;
                p = p / 2540m;      // get it in inches
                p = p * 96;				// 
                if (p != 0)
                    return (int)p;
                return (Size > 0) ? 1 : (int)p;
            }
        }

        /// <summary>
        /// Converts the size into pixels.
        /// </summary>
        /// <param name="dpi">The dpi to be used in the convertion.</param>
        /// <returns>An int containing the size in pixels.</returns>
        internal int ToPixels(decimal dpi)
        {
            return RSize.ToPixels(Size, dpi);
        }

        /// <summary>
        /// Converts the size into pixels.
        /// </summary>
        /// <param name="size">The size to be converted.</param>
        /// <param name="dpi">The dpi to use in the convertion.</param>
        /// <returns>An int containing the size in pixels based on the specified DPI.</returns>
        static public int ToPixels(int size, decimal dpi)
        {
            // For now assume 96 dpi
            decimal p = size;
            p = p / PARTS_PER_INCH;		// get it in inches
            p = p * dpi;				// 
            if (p != 0)
                return (int)p;
            return (size > 0) ? 1 : (int)p;
        }

        internal float Points => (float)(Size / 2540.0 * POINTSIZED);

        internal float ToPoints()
        {
            return RSize.ToPoints(Size);
        }

        static public float ToPoints(int size)
        {
            return (float)(size / (double)PARTS_PER_INCH * Utility.Measurement.POINTSIZE_F);
        }

        /// <summary>
        /// TWIPS is 1/20 th of the size in points
        /// </summary>
        internal int Twips => TwipsFromPoints(Points);

        static internal int TwipsFromPoints(float pt) => (int)Math.Round(pt * 20, 0);

        static internal float PointsFromPixels(Graphics g, int x)
        {
            float result = x * POINTSIZED / g.DpiX; // convert to points from pixels
            return result;
        }

        static internal float PointsFromPixels(Graphics g, float x)
        {
            float result = x * POINTSIZED / g.DpiX; // convert to points from pixels
            return result;
        }

        /// <summary>
        /// save original string for recreation of syntax
        /// </summary>
        internal string Original { get; set; }

        /// <summary>
        /// The original size is "almost" compatible with CSS - except CSS doesn't allow blanks.
        /// </summary>
        internal string CSS
        {
            get
            {
                if (Original.IndexOf(' ') >= 0)
                    return Original.Replace(" ", "");

                return Original;
            }
        }

        #region ====operators====
        public static RSize operator +(RSize arg1, RSize arg2) => new RSize(arg1.Size + arg2.Size);

        public static RSize operator -(RSize arg1, RSize arg2) => new RSize(arg1.Size - arg2.Size);
        #endregion
    }
}
