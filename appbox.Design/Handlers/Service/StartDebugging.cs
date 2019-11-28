using System.IO;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    /// <summary>
    /// 开始调试服务模型
    /// </summary>
    sealed class StartDebugging : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            var modelID = args.GetString();
            var methodName = args.GetString();
            var methodArgs = args.GetString();
            var breakpoints = args.GetString();

            //先编译服务模型，将编译结果保存至当前会话的调试目录内
            var debugFolder = Path.Combine(Runtime.RuntimeContext.Current.AppPath, "debug", hub.Session.SessionID.ToString());
            if (Directory.Exists(debugFolder))
                Directory.Delete(debugFolder, true);
            Directory.CreateDirectory(debugFolder);

            var serviceNode = hub.DesignTree.FindModelNode(ModelType.Service, ulong.Parse(modelID));
            await PublishService.CompileServiceAsync(hub, (ServiceModel)serviceNode.Model, debugFolder);

            //启动调试进程
            var appName = serviceNode.AppNode.Model.Name;
            hub.DebugService.DebugSourcePath = $"{appName}.Services.{serviceNode.Model.Name}.cs";
            hub.DebugService.StartDebugger($"{appName}.{serviceNode.Model.Name}.{methodName}", methodArgs, breakpoints);

            return null;
        }
    }
}
