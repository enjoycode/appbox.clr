﻿using System;
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
        private static readonly Dictionary<string, IRequestHandler> handlers;

        static DesignService()
        {
            handlers = new Dictionary<string, IRequestHandler>
            {
                { nameof(LoadDesignTree), new LoadDesignTree() },
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
                //Entity
                { nameof(NewEntityModel), new NewEntityModel() },
                { nameof(GetEntityModel), new GetEntityModel() },
                { nameof(LoadEntityData), new LoadEntityData() },
                { nameof(NewEntityMember), new NewEntityMember() },
                { nameof(DeleteEntityMember), new DeleteEntityMember() },
                { nameof(ChangeEntity), new ChangeEntity() },
                { nameof(ChangeEntityMember), new ChangeEntityMember() },
                { nameof(GetEntityRefModels), new GetEntityRefModels() },
                //Service
                { nameof(NewServiceModel), new NewServiceModel() },
                { nameof(OpenServiceModel), new OpenServiceModel() },
                { nameof(GetServiceMethod), new GetServiceMethod() },
                { nameof(GenServiceDeclare), new GenServiceDeclare() },
                { nameof(GetReferences), new GetReferences() },
                { nameof(UpdateReferences), new UpdateReferences() },
                { nameof(StartDebugging), new StartDebugging() },
                { nameof(ContinueBreakpoint), new ContinueBreakpoint() },
                //View
                { nameof(NewViewModel), new NewViewModel() },
                { nameof(OpenViewModel), new OpenViewModel() },
                { nameof(LoadView), new LoadView() },
                { nameof(ChangeRouteSetting), new ChangeRouteSetting() },
                //Permission
                { nameof(NewPermissionModel), new NewPermissionModel() },
                //C# 代码编辑器相关
                { nameof(ChangeBuffer), new ChangeBuffer() },
                { nameof(GetCompletion), new GetCompletion() },
                { nameof(CheckCode), new CheckCode() },
                { nameof(FormatDocument), new FormatDocument() },
                { nameof(GetHover), new GetHover() },
                //Blob
                { nameof(GetBlobObjects), new GetBlobObjects() }
            };
            //handlers.Add(nameof(FindUsages), new FindUsages());
            //handlers.Add(nameof(Rename), new Rename());
        }

        public Task<object> InvokeAsync(string method, InvokeArgs args)
        {
            if (!(RuntimeContext.Current.CurrentSession is IDeveloperSession developerSession))
                throw new Exception("Must login as a Developer");

            var desighHub = developerSession.GetDesignHub();
            if (desighHub == null)
                throw new Exception("Cannot get DesignContext");

            if (!handlers.TryGetValue(method, out IRequestHandler handler))
                throw new Exception($"Unknown design request: {method}");

            return handler.Handle(desighHub, args);
        }

    }
}
