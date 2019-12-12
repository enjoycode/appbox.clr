using System;
using appbox.Serialization;
using System.Text.Json;

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
        public override void WriteMembers(Utf8JsonWriter writer, WritedObjects objrefs)
        {
            writer.WriteString("App", ((ApplicationNode)Parent).Model.Name);
            writer.WriteString("Name", Text);
        }
        #endregion

    }
}
