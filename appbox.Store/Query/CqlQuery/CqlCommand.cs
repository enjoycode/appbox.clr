using System;
using appbox.Data;

namespace appbox.Store
{
    public struct CqlCommand : ICqlCommand
    {

        public readonly Entity Entity;
        public readonly bool CheckExists;
        public readonly CqlCommandType Type;

        public CqlCommand(CqlCommandType type, Entity entity, bool ifNotExists = false)
        {
            Type = type;
            Entity = entity;
            CheckExists = ifNotExists;
        }

    }

    public enum CqlCommandType : byte
    {
        Insert,
        Update,
        Delete
    }

    public interface ICqlCommand
    { }
}
