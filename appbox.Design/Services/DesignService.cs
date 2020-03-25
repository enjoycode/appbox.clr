using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Caching;
using appbox.Runtime;
using OmniSharp.Mef;
using OmniSharp.Roslyn.CSharp.Services;

namespace appbox.Design
{
    public sealed class DesignService : IService
    {
        private static readonly Dictionary<CharsKey, IRequestHandler> handlers;

        static DesignService()
        {
            handlers = new Dictionary<CharsKey, IRequestHandler>
            {
                { nameof(LoadDesignTree), new LoadDesignTree() },
                { nameof(NewApplication), new NewApplication() },
                //DataStore
                { nameof(NewDataStore), new NewDataStore() },
                { nameof(SaveDataStore), new SaveDataStore() },
                //Folder
                { nameof(NewFolder), new NewFolder() },
                //Model
                { nameof(CloseDesigner), new CloseDesigner() },
                { nameof(SaveModel), new SaveModel() },
                { nameof(Checkout), new Checkout()},
                { nameof(GetPendingChanges), new GetPendingChanges() },
                { nameof(Publish), new Publish() },
                { nameof(DeleteNode), new DeleteNode() },
                { nameof(DragDropNode), new DragDropNode() },
                { nameof(FindUsages), new FindUsages() },
                //Entity
                { nameof(NewEntityModel), new NewEntityModel() },
                { nameof(GetEntityModel), new GetEntityModel() },
                { nameof(LoadEntityData), new LoadEntityData() },
                { nameof(NewEntityMember), new NewEntityMember() },
                { nameof(DeleteEntityMember), new DeleteEntityMember() },
                { nameof(ChangeEntity), new ChangeEntity() },
                { nameof(ChangeEntityMember), new ChangeEntityMember() },
                { nameof(GetEntityRefModels), new GetEntityRefModels() },
                { nameof(GenEntityDeclare), new GenEntityDeclare() },
                //Service
                { nameof(NewServiceModel), new NewServiceModel() },
                { nameof(OpenServiceModel), new OpenServiceModel() },
                { nameof(GetServiceMethod), new GetServiceMethod() },
                { nameof(GenServiceDeclare), new GenServiceDeclare() },
                { nameof(GetReferences), new GetReferences() },
                { nameof(UpdateReferences), new UpdateReferences() },
                { nameof(StartDebugging), new StartDebugging() },
                { nameof(ContinueBreakpoint), new ContinueBreakpoint() },
                { nameof(Validate3rdLib), new Validate3rdLib() },
                { nameof(Upload3rdLib), new Upload3rdLib() },
                //View
                { nameof(NewViewModel), new NewViewModel() },
                { nameof(OpenViewModel), new OpenViewModel() },
                { nameof(LoadView), new LoadView() },
                { nameof(ChangeRouteSetting), new ChangeRouteSetting() },
                //Enum
                { nameof(NewEnumModel), new NewEnumModel() },
                { nameof(GetEnumItems), new GetEnumItems() },
                { nameof(NewEnumItem), new NewEnumItem() },
                //Permission
                { nameof(NewPermissionModel), new NewPermissionModel() },
                //C# 代码编辑器相关
                { nameof(ChangeBuffer), new ChangeBuffer() },
                { nameof(GetCompletion), new GetCompletion() },
                { nameof(CheckCode), new CheckCode() },
                { nameof(FormatDocument), new FormatDocument() },
                { nameof(GetHover), new GetHover() },
                { nameof(SignatureHelp), new SignatureHelp() },
                { nameof(GetDocSymbol), new GetDocSymbol() },
                //Blob
                { nameof(GetBlobObjects), new GetBlobObjects() }
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
