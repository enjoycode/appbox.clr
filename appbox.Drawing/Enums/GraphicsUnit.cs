using System;

namespace appbox.Drawing
{
    public enum GraphicsUnit
    {
        /// <summary>
        /// Specifies the world coordinate system unit as the unit of measure.
        /// </summary>
        World = 0,
        /// <summary>
        /// Specifies the unit of measure of the display device. 
        /// Typically pixels for video displays, and 1/100 inch for printers.
        /// </summary>
        Display = 1,
        /// <summary>
        /// Specifies a device pixel as the unit of measure.
        /// </summary>
        Pixel = 2,
        /// <summary>
        /// Specifies a printer's point (1/72 inch) as the unit of measure.
        /// </summary>
        Point = 3,
        /// <summary>
        /// Specifies the inch as the unit of measure.
        /// </summary>
        Inch = 4,
        /// <summary>
        /// Specifies the document unit (1/300 inch) as the unit of measure.
        /// </summary>
        Document = 5,
        /// <summary>
        /// Specifies the millimeter as the unit of measure.
        /// </summary>
        Millimeter = 6
    }

    public static class GraphicsUnitConverter
    {

        public static float Convert(GraphicsUnit fromUnit, GraphicsUnit toUnit, float nSrc, float dpi)
        {
            if (fromUnit == toUnit)
                return nSrc;

            float inchs = 0;
            float nTrg = 0;

            switch (fromUnit)
            {
                case GraphicsUnit.Display:
                    inchs = nSrc / 75f;
                    break;
                case GraphicsUnit.Document:
                    inchs = nSrc / 300f;
                    break;
                case GraphicsUnit.Inch:
                    inchs = nSrc;
                    break;
                case GraphicsUnit.Millimeter:
                    inchs = nSrc / 25.4f;
                    break;
                case GraphicsUnit.Pixel:
                case GraphicsUnit.World:
                    inchs = nSrc / dpi;
                    break;
                case GraphicsUnit.Point:
                    inchs = nSrc / 72f;
                    break;
                default:
                    throw new ArgumentException("Invalid GraphicsUnit");
            }

            switch (toUnit)
            {
                case GraphicsUnit.Display:
                    nTrg = inchs * 75;
                    break;
                case GraphicsUnit.Document:
                    nTrg = inchs * 300;
                    break;
                case GraphicsUnit.Inch:
                    nTrg = inchs;
                    break;
                case GraphicsUnit.Millimeter:
                    nTrg = inchs * 25.4f;
                    break;
                case GraphicsUnit.Pixel:
                case GraphicsUnit.World:
                    nTrg = inchs * dpi;
                    break;
                case GraphicsUnit.Point:
                    nTrg = inchs * 72;
                    break;
                default:
                    throw new ArgumentException("Invalid GraphicsUnit");
            }

            return nTrg;
        }

    }
}

