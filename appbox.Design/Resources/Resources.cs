using System;
using System.Reflection;

namespace appbox.Design
{
    static class Resources
    {

        private static readonly Assembly resAssembly = typeof(DesignTree).Assembly;

        internal static string LoadStringResource(string res)
        {
            var stream = resAssembly.GetManifestResourceStream("appbox.Design." + res);
            var reader = new System.IO.StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
