using System;
using System.IO;

namespace appbox.Drawing
{
    public sealed class Metafile : Image
    {
        public override void Save(Stream stream, ImageFormat format)
        {
            throw new NotImplementedException();
        }
    }
}
