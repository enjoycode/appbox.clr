using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using appbox.Serialization;
using System.Text.Json;
using OmniSharp.Mef;

namespace appbox.Design
{
    sealed class GetPendingChanges : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            var staged = await StagedService.LoadStagedAsync(onlyModelsAndFolders: false); //TODO:暂重新加载
            hub.PendingChanges = staged.Items;
            if (hub.PendingChanges == null || hub.PendingChanges.Length == 0)
                return null;

            var res = new List<ChangedInfo>();
            for (int i = 0; i < hub.PendingChanges.Length; i++)
            {
                //TODO:其他类型处理
                if (hub.PendingChanges[i] is ModelBase model)
                    res.Add(new ChangedInfo { ModelType = model.ModelType.ToString(), ModelID = model.Name });
                else if (hub.PendingChanges[i] is ModelFolder folder)
                    res.Add(new ChangedInfo { ModelType = ModelType.Folder.ToString(), ModelID = folder.TargetModelType.ToString() });
            }
            return res;
        }
    }

    struct ChangedInfo : IJsonSerializable
    {
        public string ModelType;
        public string ModelID;

        public string JsonObjID => string.Empty;

        public PayloadType JsonPayloadType => PayloadType.UnknownType;

        public void WriteToJson(Utf8JsonWriter writer, WritedObjects objrefs)
        {
            writer.WriteString(nameof(ModelType), ModelType);
            writer.WriteString(nameof(ModelID), ModelID);
        }

        public void ReadFromJson(ref Utf8JsonReader reader, ReadedObjects objrefs) => throw new NotSupportedException();
    }
}
