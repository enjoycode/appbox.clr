using System;

namespace appbox.Serialization
{
    public sealed class SerializationException : Exception
    {
        public SerializationError Error { get; private set; }

        public SerializationException(SerializationError error)
        {
            this.Error = error;
        }

        public SerializationException(SerializationError error, string msg) : base(msg)
        {
            this.Error = error;
        }
    }

    public enum SerializationError
    {
        CanNotFindSerializer,
        UnknownTypeFlag,

        SysKnownTypeAlreadyRegisted,
        NotSupportedValueType,
        NotSupportedClassType,
        KnownTypeOverriderIsNull,

        NothingToRead,
        ReadVariantOutOfRange
    }
}

