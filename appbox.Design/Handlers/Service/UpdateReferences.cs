using System;
using System.Linq;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    /// <summary>
    /// 更新服务模型的引用项
    /// </summary>
    sealed class UpdateReferences : IRequestHandler
    {
        public Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            var modelID = args.GetString();
            var array = args.GetObjectArray();
            var newDeps = array != null ? array.Cast<string>().ToArray() : new string[0];

            var modelNode = hub.DesignTree.FindModelNode(ModelType.Service, ulong.Parse(modelID));
            if (modelNode == null)
                throw new Exception("Can't find service model node");

            var model = (ServiceModel)modelNode.Model;
            var appName = modelNode.AppNode.Model.Name;
            //开始比对
            //bool hasChanged = false;
            if (model.HasReference)
            {
                //先处理移除的
                for (int i = model.References.Count - 1; i >= 0; i--)
                {
                    if (!newDeps.Contains(model.References[i]))
                    {
                        hub.TypeSystem.RemoveServiceReference(modelNode.ServiceProjectId, appName, model.References[i]);
                        model.References.RemoveAt(i);
                        //hasChanged = true;
                    }
                }
                //再处理新增的
                for (int i = 0; i < newDeps.Length; i++)
                {
                    if (!model.References.Contains(newDeps[i]))
                    {
                        hub.TypeSystem.AddServiceReference(modelNode.ServiceProjectId, appName, newDeps[i]);
                        model.References.Add(newDeps[i]);
                        //hasChanged = true;
                    }
                }
            }
            else if (newDeps.Length > 0)
            {
                for (int i = 0; i < newDeps.Length; i++)
                {
                    hub.TypeSystem.AddServiceReference(modelNode.ServiceProjectId, appName, newDeps[i]);
                    model.References.Add(newDeps[i]);
                }
                //hasChanged = true;
            }

            //if (hasChanged)
            //    await modelNode.SaveAsync(null);

            return Task.FromResult((object)null);
        }
    }

}
