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
        private static readonly Dictionary<ReadOnlyMemory<char>, IService> services =
            new Dictionary<ReadOnlyMemory<char>, IService>();

        internal static void Init()
        {
            services.Add(nameof(Services.AdminService).AsMemory(), new Services.AdminService());
            services.Add(nameof(Services.ClusterService).AsMemory(), new Services.ClusterService());
            services.Add(nameof(Design.DesignService).AsMemory(), new Design.DesignService());
        }

        internal static bool TryGet(ReadOnlyMemory<char> serviceName, out IService instance)
        {
            return services.TryGetValue(serviceName, out instance);
        }
    }

}
