using System;
using System.Threading.Tasks;
using appbox.Data;

namespace appbox.Runtime
{
    /// <summary>
    /// 服务接口
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// 调用服务方法
        /// </summary>
        /// <param name="method">eg: "SaveOrder"</param>
        /// <param name="args"></param>
        /// <returns></returns>
        ValueTask<AnyValue> InvokeAsync(ReadOnlyMemory<char> method, InvokeArgs args);
    }
}
