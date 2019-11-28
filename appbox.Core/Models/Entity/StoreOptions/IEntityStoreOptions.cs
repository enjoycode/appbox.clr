using System;
using appbox.Serialization;

namespace appbox.Models
{
    /// <summary>
    /// Entity映射的目标存储的相关选项，如索引等
    /// </summary>
    public interface IEntityStoreOptions : IBinSerializable, IJsonSerializable
    {
        void AcceptChanges();

        //void Import();

        //void UpdateFrom(IEntityStoreOptions other);
    }
}
