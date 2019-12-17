using System;
using System.Reflection;

namespace appbox.Design.Tests
{
    static class Resources
    {

        static readonly Assembly resAssembly = typeof(Core.Tests.MockRuntimeContext).Assembly;

        internal static string LoadStringResource(string res)
        {
            var stream = resAssembly.GetManifestResourceStream("appbox.Design.Tests." + res);
            var reader = new System.IO.StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
