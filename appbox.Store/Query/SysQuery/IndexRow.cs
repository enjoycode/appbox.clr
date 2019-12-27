#if FUTURE

using System;
using appbox.Data;
using appbox.Server;

namespace appbox.Store
{
    public struct IndexRow : IDisposable
    {

        public static readonly IndexRow Empty = new IndexRow();

        private INativeData _value;

        public bool IsEmpty => _value == null;

        public KVTuple ValueTuple { get; private set; }

        public Guid TargetEntityId => ValueTuple.GetGuid(0).Value;

        internal IndexRow(INativeData value)
        {
            _value = value;
            ValueTuple = new KVTuple();
            ValueTuple.ReadFrom(_value.DataPtr, (int)_value.Size);
        }

        public override string ToString()
        {
            return _value == null ? $"IndexRow: Empty" : $"IndexRow: {StringHelper.ToHexString(_value.DataPtr, (int)_value.Size)}";
        }

        public void Dispose()
        {
            _value.Dispose();
            _value = null;
        }
    }
}

#endif
