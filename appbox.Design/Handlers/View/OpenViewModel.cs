using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using appbox.Serialization;
using System.Text.Json;
using OmniSharp.Mef;

namespace appbox.Design
{
    sealed class OpenViewModel : IRequestHandler
    {
        public async Task<object> Handle(DesignHub hub, InvokeArgs args)
        {
            var modelID = args.GetString();
            var modelNode = hub.DesignTree.FindModelNode(ModelType.View, ulong.Parse(modelID));
            if (modelNode == null)
                throw new Exception($"Cannot find view model: {modelID}");

            var res = new OpenViewModelResult();
            res.Model = (ViewModel)modelNode.Model;
            bool hasLoadSource = false;
            if (modelNode.IsCheckoutByMe)
            {
                var codes = await StagedService.LoadViewCodeAsync(modelNode.Model.Id);
                if (codes.Item1)
                {
                    hasLoadSource = true;
                    res.Template = codes.Item2;
                    res.Script = codes.Item3;
                    res.Style = codes.Item4;
                }
            }
            if (!hasLoadSource)
            {
                var codes = await Store.ModelStore.LoadViewCodeAsync(modelNode.Model.Id);
                res.Template = codes.Item1;
                res.Script = codes.Item2;
                res.Style = codes.Item3;
            }

            return res;
        }

        struct OpenViewModelResult : IJsonSerializable
        {
            public ViewModel Model;
            public string Template;
            public string Script;
            public string Style;

            public PayloadType JsonPayloadType => PayloadType.UnknownType;

            public void ReadFromJson(ref Utf8JsonReader reader, ReadedObjects objrefs) => throw new NotSupportedException();

            public void WriteToJson(Utf8JsonWriter writer, WritedObjects objrefs)
            {
                ((IJsonSerializable)Model).WriteToJson(writer, objrefs);

                writer.WriteString("Template", Template);
                writer.WriteString("Script", Script);
                writer.WriteString("Style", Style);
            }
        }
    }

}
