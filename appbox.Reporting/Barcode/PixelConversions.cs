namespace appbox.Reporting
{
    internal static class PixelConversions
    {
        public static int MmFromPixel(float dpiX, float pixel)
        {
            int mm = (int)(pixel / dpiX * 25.4f);	// convert to pixels
            return mm;
        }

        public static int PixelFromMm(float dpiX, float mm)
        {
            int pixels = (int)(mm * dpiX / 25.4f);	// convert to pixels
            return pixels;
        }

        public static float GetMagnification(float dpiX, float dpiY, int width, int height,
            float OptimalHeight, float OptimalWidth)
        {
            float AspectRatio = OptimalHeight / OptimalWidth;
            float r = height / width;
            if (r <= AspectRatio)
            {   // height is the limiting value
                r = MmFromPixel(dpiY, height) / OptimalHeight;
            }
            else
            {   // width is the limiting value
                r = MmFromPixel(dpiX, width) / OptimalWidth;
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
