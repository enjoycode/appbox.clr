using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    sealed class ChangeRouteSetting : IRequestHandler
    {
        public Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            var modelID = args.GetString();
            var routeEnable = args.GetBoolean();
            var routeParent = args.GetString();
            var routePath = args.GetString();
            if (routeEnable && !string.IsNullOrEmpty(routeParent) && string.IsNullOrEmpty(routePath))
                throw new InvalidOperationException("Assign RouteParent must set RoutePath");
            //TODO:判断路径有效性，以及是否重复

            var modelNode = hub.DesignTree.FindModelNode(ModelType.View, ulong.Parse(modelID));
            if (modelNode == null)
                throw new Exception($"Cannot find view model node: {modelID}");

            var viewModel = (ViewModel)modelNode.Model;
            viewModel.Flag = routeEnable == true ? ViewModelFlag.ListInRouter : ViewModelFlag.None;
            viewModel.RouteParent = routeParent;
            viewModel.RoutePath = routePath;

            return Task.FromResult<object>(true);
        }
    }
}
