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
        static readonly Dictionary<string, IService> services = new Dictionary<string, IService>();

        internal static void Init()
        {
            services.Add(nameof(Services.AdminService), new Services.AdminService());
            services.Add(nameof(Services.ClusterService), new Services.ClusterService());
            services.Add(nameof(Design.DesignService), new Design.DesignService());
        }

        internal static bool TryGet(string serviceName, out IService instance)
        {
            return services.TryGetValue(serviceName, out instance);
        }
    }

}
