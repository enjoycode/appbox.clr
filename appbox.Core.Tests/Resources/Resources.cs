using System;
using System.Reflection;

namespace appbox.Core.Tests
{
    static class Resources
    {

        private static readonly Assembly resAssembly = typeof(TestHelper).Assembly;

        internal static string GetString(string res)
        {
            var stream = resAssembly.GetManifestResourceStream("appbox.Core.Tests." + res);
            var reader = new System.IO.StreamReader(stream);
            return reader.ReadToEnd();
        }

        internal static byte[] GetBytes(string res)
        {
            var stream = resAssembly.GetManifestResourceStream("appbox.Core.Tests." + res);
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            return bytes;
        }
    }
}
