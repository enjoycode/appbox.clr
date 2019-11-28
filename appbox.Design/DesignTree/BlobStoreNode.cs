using System;
using appbox.Serialization;
using Newtonsoft.Json;

namespace appbox.Design
{
    public sealed class BlobStoreNode : DesignNode
    {
        public override DesignNodeType NodeType => DesignNodeType.BlobStoreNode;

        internal uint AppID => ((ApplicationNode)Parent).Model.Id;

        public override string ID => $"{AppID}-Blob";

        public BlobStoreNode()
        {
            Text = "BlobStore";
        }

        #region ====Serialization====
        public override void WriteMembers(JsonTextWriter writer, WritedObjects objrefs)
        {
            writer.WritePropertyName("App");
            writer.WriteValue(((ApplicationNode)Parent).Model.Name);

            writer.WritePropertyName("Name");
            writer.WriteValue(Text);
        }
        #endregion

    }
}
