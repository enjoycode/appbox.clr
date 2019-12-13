using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using appbox.Runtime;

namespace appbox.Core.Tests
{
    sealed class MockRuntimContext : IRuntimeContext
    {
        private readonly Dictionary<ulong, ModelBase> _entityModels = new Dictionary<ulong, ModelBase>();

        public void AddModel(ModelBase model)
        {
            _entityModels.Add(model.Id, model);
        }

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
            if (_entityModels.TryGetValue(modelId, out ModelBase found))
            {
                return new ValueTask<T>((T)found);
            }
            return new ValueTask<T>(default(T));
        }

        public void InvalidModelsCache(string[] services, ulong[] others, bool byPublish)
        {
            throw new NotImplementedException();
        }

        public ValueTask<AnyValue> InvokeAsync(string servicePath, InvokeArgs args)
        {
            throw new NotImplementedException();
        }
    }
}
