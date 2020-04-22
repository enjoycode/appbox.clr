using System;
using System.IO;

namespace appbox.Drawing
{
    public sealed class Metafile : Image
    {
        public override void Save(Stream stream, ImageFormat format, int quality = 100)
        {
            throw new NotImplementedException();
        }
    }
}
