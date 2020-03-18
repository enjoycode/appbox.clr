using System;
using System.Linq;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    /// <summary>
    /// 处理前端设计树拖动节点
    /// </summary>
    sealed class DragDropNode : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            var sourceNodeType = (DesignNodeType)args.GetInt32();
            var sourceNodeID = args.GetString();
            var targetNodeType = (DesignNodeType)args.GetInt32();
            var targetNodeID = args.GetString();
            var position = args.GetString(); //inner or before or after

            var sourceNode = hub.DesignTree.FindNode(sourceNodeType, sourceNodeID);
            var targetNode = hub.DesignTree.FindNode(targetNodeType, targetNodeID);
            if (sourceNode == null || targetNode == null)
                throw new Exception("处理拖动时无法找到相应的节点");

            //TODO: 再次验证是否允许操作，前端已验证过
            //TODO:完整实现以下逻辑，暂只支持Inside
            if (position == "inner")
            {
                switch (sourceNodeType)
                {
                    case DesignNodeType.FolderNode:
                        throw new NotImplementedException();
                    case DesignNodeType.EntityModelNode:
                    case DesignNodeType.ServiceModelNode:
                    case DesignNodeType.ViewModelNode:
                    case DesignNodeType.EnumModelNode:
                    case DesignNodeType.EventModelNode:
                    case DesignNodeType.PermissionModelNode:
                    case DesignNodeType.WorkflowModelNode:
                    case DesignNodeType.ReportModelNode:
                        await DropModelNodeInside((ModelNode)sourceNode, targetNode);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
            else
            {
                throw new NotImplementedException($"暂未实现 position={position}");
            }

            return null;
        }

        private static async Task DropModelNodeInside(ModelNode sourceNode, DesignNode targetNode)
        {
            //注意：目标节点可能是模型根目录
            if (targetNode.NodeType == DesignNodeType.ModelRootNode)
            {
                var rootNode = (ModelRootNode)targetNode;
                if (rootNode.AppID != sourceNode.Model.AppId)
                    throw new InvalidOperationException("无法拖动模型节点至不同的Application内");

                sourceNode.Parent.Nodes.Remove(sourceNode);
                targetNode.Nodes.Add(sourceNode);
                sourceNode.Model.FolderId = null;
                await StagedService.SaveModelAsync(sourceNode.Model); //直接保存
            }
            else if (targetNode.NodeType == DesignNodeType.FolderNode)
            {
                var targetFolder = ((FolderNode)targetNode).Folder;
                var rootFolder = targetFolder.GetRoot();
                if (rootFolder.AppId != sourceNode.Model.AppId)
                    throw new InvalidOperationException("无法拖动模型节点至不同的Application内");

                sourceNode.Parent.Nodes.Remove(sourceNode);
                targetNode.Nodes.Add(sourceNode);
                sourceNode.Model.FolderId = targetFolder.Id;
                await StagedService.SaveModelAsync(sourceNode.Model); //直接保存
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        //enum DropPosition
        //{
        //    Before = 0,
        //    After = 1,
        //    Inside = 2
        //}
    }
}
