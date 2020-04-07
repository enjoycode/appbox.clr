using System;
using System.IO;

namespace appbox.Drawing
{
    public abstract class Image
    {
        public abstract void Save(Stream stream, ImageFormat format);
    }
}
