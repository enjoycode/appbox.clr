using System;
using System.Runtime.InteropServices;

namespace appbox.Drawing
{
    internal static class SkiaApi
    {
        private const string SKIA = "libSkiaSharp";

        // void sk_canvas_draw_text(sk_canvas_t*, const char* text, size_t byteLength, float x, float y, const sk_paint_t* paint)
        [DllImport(SKIA, CallingConvention = CallingConvention.Cdecl)]
        internal unsafe static extern void sk_canvas_draw_text(IntPtr canvas,
            /* char */ void* text, /* size_t */ IntPtr byteLength,
            float x, float y, IntPtr paint);

    }
}
