using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    sealed class NewDataStore : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            //TODO:先判断DataStoreRootNode有没有被当前用户签出

            // 获取接收到的参数
            var storeType = (DataStoreKind)args.GetInt32();
            var storeProvider = args.GetString();
            var storeName = args.GetString();

            // 验证类名称的合法性
            if (string.IsNullOrEmpty(storeName))
                throw new Exception("DataStore name can not be null");
            if (!CodeHelper.IsValidIdentifier(storeName))
                throw new Exception("DataStore name invalid");
            // TODO: 验证名称是否已存在

            // 开始新建存储节点
            var model = new DataStoreModel(storeType, storeProvider, storeName);
            //添加节点至模型树并绑定签出信息
            var node = hub.DesignTree.StoreRootNode.AddModel(model, hub);

            // 保存至本地
            await node.SaveAsync();
            // 新建RoslynDocument
            hub.TypeSystem.CreateStoreDocument(node);

            return node;
        }
    }
}
