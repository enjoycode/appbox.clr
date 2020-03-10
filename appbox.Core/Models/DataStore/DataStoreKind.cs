using System;

namespace appbox.Models
{
    public enum DataStoreKind : byte
    {
        Sql = 0,
        Cql = 1,
        Blob = 2,
        /// <summary>
        /// 内置分布式数据库
        /// </summary>
        Future = 3,
    }
}
