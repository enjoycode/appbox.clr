using System;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Models;
using appbox.Serialization;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

namespace appbox.Design
{
    sealed class DataStoreNode : DesignNode
    {
        public override DesignNodeType NodeType => DesignNodeType.DataStoreNode;

        internal DocumentId RoslynDocumentId { get; private set; }

        public override string ID => Model.Id.ToString();

        public override string Text
        {
            get { return Model.Name; }
            set { throw new NotSupportedException(); }
        }

        public override string CheckoutInfoTargetID => Model.Id.ToString();

        internal DataStoreModel Model { get; set; }

        internal DataStoreNode(DataStoreModel model, DesignHub hub)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            RoslynDocumentId = DocumentId.CreateNewId(hub.TypeSystem.ServiceBaseProjectId);
        }

        internal async Task SaveAsync()
        {
            if (!IsCheckoutByMe)
                throw new Exception("StoreNode has not checkout");

            //保存节点模型
            await StagedService.SaveModelAsync(Model);
        }

        #region ====Serialization====
        public override void WriteMembers(JsonTextWriter writer, WritedObjects objrefs)
        {
            writer.WritePropertyName("Kind");
            writer.WriteValue((int)Model.Kind);

            writer.WritePropertyName("Provider");
            writer.WriteValue(Model.Provider);

            writer.WritePropertyName("Settings");
            writer.WriteValue(Model.Settings);
        }
        #endregion
    }
}
