using System;
using System.Diagnostics;
using appbox.Server;

namespace appbox.Host
{

    /// <summary>
    /// 包装子进程及相应的通道
    /// </summary>
    sealed class ChildProcess
    {
        public ulong Id { get; private set; }
        public Process Process { get; private set; }
        public SharedMemoryChannel Channel { get; private set; }

        private ChildProcess(ulong id, Process process, SharedMemoryChannel channel)
        {
            Id = id;
            Process = process;
            Channel = channel;
        }

        #region ====Statics====
        internal static ChildProcess AppContainer { get; private set; }

        /// <summary>
        /// 启动应用子进程
        /// </summary>
        internal static void StartAppContainer()
        {
            //1.先建立通道
            var appChannel = new SharedMemoryChannel("AppChannel", 81920, new HostMessageDispatcher(), 1UL); //TODO: check count
            appChannel.StartReceive();
            //2.再启动子进程
            var process = RunProcess();
            AppContainer = new ChildProcess(1, process, appChannel);
        }

        private static Process RunProcess()
        {
            var process = new Process();
            process.StartInfo.UseShellExecute = false;
#if Windows
            process.StartInfo.FileName = "appbox.AppContainer.exe";
#else
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = "appbox.AppContainer";
#endif
            process.StartInfo.Arguments = Runtime.RuntimeContext.PeerId.ToString();
            process.EnableRaisingEvents = true;
            process.Exited += OnChildProcessExited;

            try
            {
                process.Start();
                return process;
            }
            catch (Exception ex)
            {
                Log.Warn("启动子进程[" + process.StartInfo.FileName + "]错误: " + ex.Message);
                throw;
            }
        }

        private static void OnChildProcessExited(object sender, EventArgs e)
        {
            //TODO:如果应用子进程，自动重启
            Log.Warn("子进程退出.");
        }

        /// <summary>
        /// 通知所有子进程刷新模型缓存
        /// </summary>
        internal static void InvalidModelsCache(string[] services, ulong[] others)
        {
            //TODO:暂只更新应用子进程
            var msg = new InvalidModelsCache(services, others);
            AppContainer.Channel.SendMessage(ref msg);
        }
#endregion
    }

}
