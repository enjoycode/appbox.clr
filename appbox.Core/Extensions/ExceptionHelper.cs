using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace appbox
{
    public static class ExceptionHelper
    {

        public static string GetExceptionDetailInfo(Exception ex)
        {
            if (ex == null)
                return string.Empty;

            AggregateException aex = ex as AggregateException;
            if (aex != null)
                ex = aex.InnerException;

            return $"Type:{ex.GetType().FullName} {Environment.NewLine} Message:{ex.Message} {Environment.NewLine} Trace:{ex.StackTrace}";
        }

        internal static NotImplementedException NotImplemented([CallerFilePath] string file = "",
                                            [CallerMemberName] string method = "",
                                            [CallerLineNumber] int line = 0)
        {
            return new NotImplementedException($"File:{Path.GetFileNameWithoutExtension(file)} Method:{method} Line:{line}");
        }

    }
}
