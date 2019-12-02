using System;
using appbox.Models;

namespace appbox.Server
{
    /// <summary>
    /// 设计时上下文，每个开发者对应一个实例
    /// </summary>
    public interface IDesignContext
    {
        EntityModel GetEntityModel(ulong modelID);
    }
}
