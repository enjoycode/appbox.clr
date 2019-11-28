using System;

namespace appbox.Models
{
    /// <summary>
    /// 实体模型的分区键
    /// </summary>
    internal struct PartitionKey
    {
        /// <summary>
        /// 分区键实体成员标识，0特殊表示默认的创建时间
        /// </summary>
        internal ushort MemberId;
        /// <summary>
        /// 分区键排序
        /// </summary>
        internal bool OrderByDesc;

        internal PartitionKeyRule Rule;
        /// <summary>
        /// Hash or RangeOfDate分区规则的附加参数
        /// </summary>
        internal int RuleArgument;
       
        //internal string GetName(EntityModel owner)
        //{
        //    if (MemberId == 0) return "CreateTime";
        //    return owner.GetMember(MemberId, true).Name;
        //}
    }

    /// <summary>
    /// 分区键的规则
    /// </summary>
    internal enum PartitionKeyRule: byte
    {
        /// <summary>
        /// 按指定成员值
        /// </summary>
        None = 0,
        /// <summary>
        /// 按Hash
        /// </summary>
        Hash,
        /// <summary>
        /// 按时间区间
        /// </summary>
        RangeOfDate,
    }

    internal enum DatePeriod : byte
    {
        Year,
        Month,
        Day
    }
}
