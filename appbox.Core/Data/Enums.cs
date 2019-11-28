using System;

namespace appbox.Data
{
    /// <summary>
    /// 持久化状态
    /// </summary>
    public enum PersistentState : byte
    {
        Detached = 0,
        Unchanged = 1,
        Modified = 2,
        Deleted = 3,
    }
}
