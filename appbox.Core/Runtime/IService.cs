using System;
using System.Threading.Tasks;
using appbox.Data;

namespace appbox.Runtime
{
    public interface IService
    {
        Task<object> InvokeAsync(string method, InvokeArgs args);
    }
}
