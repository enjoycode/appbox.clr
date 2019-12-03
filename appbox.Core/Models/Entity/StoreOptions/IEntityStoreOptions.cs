using System;
using System.Collections.Generic;
using appbox.Serialization;

namespace appbox.Models
{
    /// <summary>
    /// Entity映射的目标存储的相关选项，如索引等
    /// </summary>
    public interface IEntityStoreOptions : IBinSerializable, IJsonSerializable
    {

        bool HasIndexes { get; }

        /// <summary>
        /// 索引列表，仅用于设计时类型消除
        /// </summary>
        IEnumerable<IndexModelBase> Indexes { get; }

        void AcceptChanges();

        //void Import();

        //void UpdateFrom(IEntityStoreOptions other);
    }
}
