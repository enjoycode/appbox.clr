#if FUTURE

using System;
using appbox.Data;

namespace appbox.Store
{
    /// <summary>
    /// 快照一致性只读事务
    /// </summary>
    public sealed class ReadonlyTransaction : ITransaction
    {
        //TODO:缓存同一事务加载的数据, 分为EntityCache及EntityMemberCache
    }
}

#endif
