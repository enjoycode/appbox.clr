using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using OmniSharp.Mef;

namespace appbox.Design
{
    /// <summary>
    /// 重命名模型或其成员
    /// </summary>
    sealed class Rename : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            var refType = (ModelReferenceType)args.GetInt32();
            var modelID = args.GetString();
            var oldName = args.GetString();
            var newName = args.GetString();

            return await RefactoringService.RenameAsync(hub, refType,
                ulong.Parse(modelID), oldName, newName);
        }
    }
}
