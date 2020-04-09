using System;
using System.Reflection;

namespace appbox.Reporting.Tests
{
    static class Resources
    {

        static readonly Assembly resAssembly = typeof(GenReportTest).Assembly;

        internal static string LoadStringResource(string res)
        {
            var stream = resAssembly.GetManifestResourceStream("appbox.Reporting.Tests." + res);
            var reader = new System.IO.StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
