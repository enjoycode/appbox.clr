using System;
using System.Runtime.Serialization;

namespace CSharpTest.Net.Collections
{
    [Serializable]
    internal class LurchTableCorruptionException : Exception
    {
        public LurchTableCorruptionException()
        {
        }

        public LurchTableCorruptionException(string message) : base(message)
        {
        }

        public LurchTableCorruptionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected LurchTableCorruptionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}