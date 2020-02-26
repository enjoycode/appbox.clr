using System;
using System.Collections.Generic;
using appbox.Models;

namespace appbox.Server
{
    interface IAppPackage
    {
        public ApplicationModel Application { get; set; }

        public List<ModelBase> Models { get; }

        public List<ModelFolder> Folders { get; }

        public Dictionary<ulong, byte[]> SourceCodes { get;  } //Key=ModelId

        public Dictionary<string, byte[]> ServiceAssemblies { get;} //Value=null表示重命名后需要删除的

        public Dictionary<string, byte[]> ViewAssemblies { get; } //Value=null同上
    }
}
