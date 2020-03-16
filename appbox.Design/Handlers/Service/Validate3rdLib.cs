using System;
using System.Threading.Tasks;
using appbox.Data;
using OmniSharp.Mef;

namespace appbox.Design
{
    /// <summary>
    /// 验证上传的第三方类库
    /// </summary>
    sealed class Validate3rdLib : IRequestHandler
    {
        public Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            string fileName = args.GetString();
            int fileLen = args.GetInt32();
            string appName = args.GetString();
            if (string.IsNullOrEmpty(appName))
                throw new ArgumentException("Must asign App");

            //TODO:考虑检测系统所有内置组件是否存在相同名称的
            Log.Debug($"验证上传的第三方组件: {fileName} {fileLen}");

            return Task.FromResult<object>(null);
        }
    }
}
