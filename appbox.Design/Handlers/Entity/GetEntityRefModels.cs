using System;
using System.Linq;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    sealed class GetEntityRefModels : IRequestHandler
    {
        public Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            var targetEntityModelID = args.GetString();
            var refModels = hub.DesignTree.FindEntityRefModels(ulong.Parse(targetEntityModelID));
            object res = refModels.Select(t => new
            {
                Path = $"{hub.DesignTree.FindApplicationNode(t.Owner.AppId).Model.Name}.{t.Owner.Name}.{t.Name}",
                EntityID = t.Owner.Id.ToString(), //ulong转换为string
                MemberID = t.MemberId,
            }).ToArray();
            return Task.FromResult(res);
        }
    }
}
