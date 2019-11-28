using System;
using System.Threading.Tasks;
using OmniSharp.Mef;
using appbox.Data;

namespace appbox.Design
{
    sealed class ContinueBreakpoint : IRequestHandler
    {
        public Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            int threadId = args.GetInt32();
            hub.DebugService.Continue(threadId);
            return Task.FromResult<object>(null);
        }
    }
}
