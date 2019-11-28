using System;
using System.Collections.Generic;
using appbox.Models;
using appbox.Serialization;
using Newtonsoft.Json;

namespace appbox.Data
{
    /// <summary>
    /// 用于运行时权限分配时包装PermissionModel形成权限树
    /// </summary>
    public sealed class PermissionNode : IJsonSerializable
    {
        #region ====Fields & Properties====
        public string Name { get; private set; }
        public PermissionModel Model { get; private set; }
        private List<PermissionNode> _childs;
        public List<PermissionNode> Childs
        {
            get
            {
                if (_childs == null)
                    _childs = new List<PermissionNode>();
                return _childs;
            }
        }

        public bool IsFolder => Model == null;
        #endregion

        #region ====Ctor====
        internal PermissionNode() { }

        internal PermissionNode(string folderName)
        {
            Name = folderName;
            Model = null;
        }

        internal PermissionNode(PermissionModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            Name = model.Name; //model.LocalizedName;
            Model = model;
        }
        #endregion

        #region ====Serialization====
        public PayloadType JsonPayloadType => PayloadType.UnknownType; //PayloadType.PermissionNode;

        public void WriteToJson(JsonTextWriter writer, WritedObjects objrefs)
        {
            writer.WritePropertyName("Id");
            writer.WriteValue(Model == null ? /*随机*/ Guid.NewGuid().ToString() : Model.Id.ToString());

            writer.WritePropertyName(nameof(Name));
            writer.WriteValue(Name);

            if (_childs != null && _childs.Count > 0)
            {
                writer.WritePropertyName(nameof(Childs));
                writer.WriteList(_childs, objrefs);
            }

            if (Model != null) //注意不管有无OrgUnits都需要序列化，因为前端用于判断是否权限节点
            {
                writer.WritePropertyName("OrgUnits");
                writer.WriteStartArray();
                if (Model.HasOrgUnits)
                {
                    for (int i = 0; i < Model.OrgUnits.Count; i++)
                    {
                        writer.WriteValue(Model.OrgUnits[i]);
                    }
                }
                writer.WriteEndArray();
            }
        }

        public void ReadFromJson(JsonTextReader reader, ReadedObjects objrefs)
        {
            throw new NotSupportedException();
        }
        #endregion

    }
}
