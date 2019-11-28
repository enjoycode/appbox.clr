using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using appbox.Runtime;

namespace appbox.Design.Tests
{
    sealed class MockRuntimContext : IRuntimeContext
    {
        public string AppPath => "/Users/lushuaijun/Projects/AppBoxFuture/appbox/cmake-build-debug";

        public bool IsMainDomain => throw new NotImplementedException();

        public ISessionInfo CurrentSession { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public ulong RuntimeId => throw new NotImplementedException();

        public ValueTask<ApplicationModel> GetApplicationModelAsync(uint appId)
        {
            throw new NotImplementedException();
        }

        public ValueTask<ApplicationModel> GetApplicationModelAsync(string appName)
        {
            throw new NotImplementedException();
        }

        public ValueTask<T> GetModelAsync<T>(ulong modelId) where T : ModelBase
        {
            throw new NotImplementedException();
        }

        public void InvalidModelsCache(string[] services, ulong[] others, bool byPublish)
        {
            throw new NotImplementedException();
        }

        public Task<object> InvokeAsync(string servicePath, InvokeArgs args)
        {
            throw new NotImplementedException();
        }
    }
}
