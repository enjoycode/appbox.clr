﻿using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Runtime;
using OmniSharp.Mef;

namespace appbox.Design
{
    sealed class GetBlobObjects : IRequestHandler
    {

#if FUTURE
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            var appName = args.GetString();
            var path = args.GetString();

            var app = await RuntimeContext.Current.GetApplicationModelAsync(appName);
            return await Store.BlobStore.ListAsync(app.StoreId, path);
        }
#else
        public Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            throw new NotImplementedException();
        }
#endif
    }
}
