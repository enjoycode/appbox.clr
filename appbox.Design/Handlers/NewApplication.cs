using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    sealed class NewApplication : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            // 获取接收到的参数
            string appName = args.GetString();
            //string localizedName = args.GetObject() as string;

            var node = hub.DesignTree.FindApplicationNodeByName(appName);
            if (node != null)
                throw new Exception("Application has existed.");
            if (System.Text.Encoding.UTF8.GetByteCount(appName) > 8)
                throw new Exception("Application name too long");
            var appRootNode = hub.DesignTree.AppRootNode;
            var appModel = new ApplicationModel("appbox", appName); //TODO:fix owner
            var appNode = new ApplicationNode(hub.DesignTree, appModel);
            appRootNode.Nodes.Add(appNode);
            // 直接创建并保存
            await Store.ModelStore.CreateApplicationAsync(appModel);

            return new NewNodeResult
            {
                ParentNodeType = (int)appRootNode.NodeType,
                ParentNodeID = appRootNode.ID,
                NewNode = appNode
            };
        }
    }
}
