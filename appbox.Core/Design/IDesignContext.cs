using System;
using appbox.Models;

namespace appbox.Design
{
    /// <summary>
    /// 设计时上下文，每个开发者对应一个实例
    /// </summary>
    public interface IDesignContext
    {
        ApplicationModel GetApplicationModel(uint appId);

        EntityModel GetEntityModel(ulong modelID);

    }
}
