using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Runtime;
using OmniSharp.Mef;
using OmniSharp.Roslyn.CSharp.Services;

namespace appbox.Design
{
    public sealed class DesignService : IService
    {
        private static readonly Dictionary<ReadOnlyMemory<char>, IRequestHandler> handlers;

        static DesignService()
        {
            handlers = new Dictionary<ReadOnlyMemory<char>, IRequestHandler>
            {
                { nameof(LoadDesignTree).AsMemory(), new LoadDesignTree() },
                //DataStore
                { nameof(NewDataStore).AsMemory(), new NewDataStore() },
                { nameof(SaveDataStore).AsMemory(), new SaveDataStore() },
                //Folder
                { nameof(NewFolder).AsMemory(), new NewFolder() },
                //Model
                { nameof(CloseDesigner).AsMemory(), new CloseDesigner() },
                { nameof(SaveModel).AsMemory(), new SaveModel() },
                { nameof(Checkout).AsMemory(), new Checkout()},
                { nameof(GetPendingChanges).AsMemory(), new GetPendingChanges() },
                { nameof(Publish).AsMemory(), new Publish() },
                { nameof(DeleteNode).AsMemory(), new DeleteNode() },
                //Entity
                { nameof(NewEntityModel).AsMemory(), new NewEntityModel() },
                { nameof(GetEntityModel).AsMemory(), new GetEntityModel() },
                { nameof(LoadEntityData).AsMemory(), new LoadEntityData() },
                { nameof(NewEntityMember).AsMemory(), new NewEntityMember() },
                { nameof(DeleteEntityMember).AsMemory(), new DeleteEntityMember() },
                { nameof(ChangeEntity).AsMemory(), new ChangeEntity() },
                { nameof(ChangeEntityMember).AsMemory(), new ChangeEntityMember() },
                { nameof(GetEntityRefModels).AsMemory(), new GetEntityRefModels() },
                //Service
                { nameof(NewServiceModel).AsMemory(), new NewServiceModel() },
                { nameof(OpenServiceModel).AsMemory(), new OpenServiceModel() },
                { nameof(GetServiceMethod).AsMemory(), new GetServiceMethod() },
                { nameof(GenServiceDeclare).AsMemory(), new GenServiceDeclare() },
                { nameof(GetReferences).AsMemory(), new GetReferences() },
                { nameof(UpdateReferences).AsMemory(), new UpdateReferences() },
                { nameof(StartDebugging).AsMemory(), new StartDebugging() },
                { nameof(ContinueBreakpoint).AsMemory(), new ContinueBreakpoint() },
                //View
                { nameof(NewViewModel).AsMemory(), new NewViewModel() },
                { nameof(OpenViewModel).AsMemory(), new OpenViewModel() },
                { nameof(LoadView).AsMemory(), new LoadView() },
                { nameof(ChangeRouteSetting).AsMemory(), new ChangeRouteSetting() },
                //Permission
                { nameof(NewPermissionModel).AsMemory(), new NewPermissionModel() },
                //C# 代码编辑器相关
                { nameof(ChangeBuffer).AsMemory(), new ChangeBuffer() },
                { nameof(GetCompletion).AsMemory(), new GetCompletion() },
                { nameof(CheckCode).AsMemory(), new CheckCode() },
                { nameof(FormatDocument).AsMemory(), new FormatDocument() },
                { nameof(GetHover).AsMemory(), new GetHover() },
                //Blob
                { nameof(GetBlobObjects).AsMemory(), new GetBlobObjects() }
            };
            //handlers.Add(nameof(FindUsages), new FindUsages());
            //handlers.Add(nameof(Rename), new Rename());
        }

        public async ValueTask<AnyValue> InvokeAsync(ReadOnlyMemory<char> method, InvokeArgs args)
        {
            if (!(RuntimeContext.Current.CurrentSession is IDeveloperSession developerSession))
                throw new Exception("Must login as a Developer");

            var desighHub = developerSession.GetDesignHub();
            if (desighHub == null)
                throw new Exception("Cannot get DesignContext");

            if (!handlers.TryGetValue(method, out IRequestHandler handler))
                throw new Exception($"Unknown design request: {method}");

            var res = await handler.Handle(desighHub, args);
            return AnyValue.From(res);
        }

    }
}
