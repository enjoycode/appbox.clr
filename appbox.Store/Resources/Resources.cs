using System;
using System.Reflection;

namespace appbox.Store
{
    static class Resources
    {

        private static readonly Assembly resAssembly = typeof(BlobStore).Assembly;

        internal static string GetString(string res)
        {
            var stream = resAssembly.GetManifestResourceStream("appbox.Store." + res);
            if (stream == null) return null;
            var reader = new System.IO.StreamReader(stream);
            return reader.ReadToEnd();
        }

        internal static byte[] GetBytes(string res)
        {
            var stream = resAssembly.GetManifestResourceStream("appbox.Store." + res);
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            return bytes;
        }
    }
}
