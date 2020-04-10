namespace appbox.Reporting
{
    internal static class PixelConversions
    {
        public static int MmXFromPixel(float dpiX, float x)
        {
            int mm = (int)(x / dpiX * 25.4f);	// convert to pixels
            return mm;
        }

        public static int MmYFromPixel(float dpiY, float y)
        {
            int mm = (int)(y / dpiY * 25.4f);	// convert to pixels
            return mm;
        }

        public static int PixelXFromMm(float dpiX, float x)
        {
            int pixels = (int)((x * dpiX) / 25.4f);	// convert to pixels
            return pixels;
        }

        public static int PixelYFromMm(float dpiY, float y)
        {
            int pixel = (int)((y * dpiY) / 25.4f);	// convert to pixels
            return pixel;
        }

        public static float GetMagnification(float dpiX, float dpiY, int width, int height,
            float OptimalHeight, float OptimalWidth)
        {
            float AspectRatio = OptimalHeight / OptimalWidth;
            float r = height / width;
            if (r <= AspectRatio)
            {   // height is the limiting value
                r = MmYFromPixel(dpiY, height) / OptimalHeight;
            }
            else
            {   // width is the limiting value
                r = MmXFromPixel(dpiX, width) / OptimalWidth;
            }
            // Set the magnification limits
            //    Specification says 80% to 200% magnification allowed
            if (r < .8f)
                r = .8f;
            else if (r > 2f)
                r = 2;

            return r;
        }

    }
}
