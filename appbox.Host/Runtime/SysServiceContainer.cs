using System;
using System.Collections.Generic;
using appbox.Runtime;

namespace appbox.Server.Runtime
{

    /// <summary>
    /// 系统服务容器，启动时注册，不能动态添加
    /// </summary>
    static class SysServiceContainer
    {

        private static readonly IService AdminService = new Services.AdminService();
        private static readonly IService ClusterService = new Services.ClusterService();
        private static readonly IService DesignService = new Design.DesignService();

        internal static void Init()
        {
        }

        internal static bool TryGet(ReadOnlyMemory<char> serviceName, out IService instance)
        {
            //TODO:待实现ReadOnlyMemoryHasher后从字典表获取
            if(serviceName.Span.SequenceEqual(nameof(DesignService).AsSpan()))
            {
                instance = DesignService; return true;
            }
            if (serviceName.Span.SequenceEqual(nameof(AdminService).AsSpan()))
            {
                instance = AdminService; return true;
            }
            if (serviceName.Span.SequenceEqual(nameof(ClusterService).AsSpan()))
            {
                instance = ClusterService; return true;
            }
            instance = null;
            return false;
        }
    }

}
