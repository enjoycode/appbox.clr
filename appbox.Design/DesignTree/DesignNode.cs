using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using appbox.Runtime;
using appbox.Serialization;
using System.Text.Json;

namespace appbox.Design
{
    public abstract class DesignNode : IJsonSerializable
    {

        public DesignNode Parent { get; internal set; }

        public virtual DesignTree DesignTree
        {
            get
            {
                var root = GetRootNode(this);
                if (root is ITopNode)
                    return root.DesignTree;
                return null;
            }
        }

        /// <summary>
        /// 用于前端回传时识别是哪个节点
        /// </summary>
        public virtual string ID => Text;

        public NodeCollection Nodes { get; }

        public abstract DesignNodeType NodeType { get; }
        internal virtual int SortNo { get { return int.MaxValue; } }

        public virtual string Text { get; set; }

        #region ====Checkout相关属性====
        public virtual uint Version { get; set; }

        /// <summary>
        /// 是否允许签出
        /// </summary>
        internal virtual bool AllowCheckout
        {
            get
            {
                if (NodeType == DesignNodeType.ModelRootNode
                    || NodeType >= DesignNodeType.EntityModelNode
                    || NodeType == DesignNodeType.DataStoreNode)
                    return true;
                //TODO:根据证书判断
                return false;
            }
        }

        private CheckoutInfo _checkoutInfo;
        /// <summary>
        /// 节点的签出信息
        /// </summary>
        internal virtual CheckoutInfo CheckoutInfo
        {
            get { return _checkoutInfo; }
            set
            {
                if (!Equals(_checkoutInfo, value))
                {
                    _checkoutInfo = value;
                    //this.OnPropertyChanged("CheckoutInfo");
                    //this.OnPropertyChanged("CheckoutImageVisibility");
                }
            }
        }

        /// <summary>
        /// 节点签出信息的标识
        /// </summary>
        public virtual string CheckoutInfoTargetID => Text;

        /// <summary>
        /// 设计节点是否被当前用户签出
        /// </summary>
        public virtual bool IsCheckoutByMe => _checkoutInfo != null && _checkoutInfo.DeveloperOuid == RuntimeContext.Current.CurrentSession.LeafOrgUnitID;
        #endregion

        public DesignNode()
        {
            Nodes = new NodeCollection(this);
        }

        /// <summary>
        /// 目前仅支持签出ModelRootNode及ModelNode
        /// </summary>
        public virtual async Task<bool> Checkout() //TODO:考虑加入参数允许签出所有下属节点
        {
            //判断是否已签出或者能否签出
            if (!AllowCheckout)
                return false;
            if (IsCheckoutByMe)
                return true;

            //调用签出服务
            List<CheckoutInfo> infos = new List<CheckoutInfo>();
            CheckoutInfo info = new CheckoutInfo(NodeType, CheckoutInfoTargetID, Version,
                                                 DesignTree.DesignHub.Session.Name,
                                                 DesignTree.DesignHub.Session.LeafOrgUnitID);
            infos.Add(info);
            CheckoutResult result = await CheckoutService.CheckoutAsync(infos);
            if (result.Success)
            {
                //签出成功则将请求的签出信息添加至当前的已签出列表
                DesignTree.AddCheckoutInfos(infos);
                //如果签出的是单个模型，且具备更新的版本，则更新
                ModelNode modelNode = this as ModelNode;
                if (modelNode != null && result.ModelWithNewVersion != null)
                {
                    modelNode.Model = result.ModelWithNewVersion; //替换旧模型
                    await DesignTree.DesignHub.TypeSystem.UpdateModelDocumentAsync(modelNode); //更新为新模型的RoslynDocument
                }
                //更新当前节点的签出信息
                CheckoutInfo = infos[0];
            }

            return result.Success;
        }

        private static DesignNode GetRootNode(DesignNode current)
        {
            return current.Parent == null ? current : GetRootNode(current.Parent);
        }

        internal int CompareTo(DesignNode other)
        {
            if (NodeType == other.NodeType)
                return string.Compare(Text, other.Text, StringComparison.Ordinal);
            return ((int)NodeType).CompareTo((int)other.NodeType);
        }

        #region ====JsonSerialization====
        // string IJsonSerializable.JsonObjID => this.ID;

        PayloadType IJsonSerializable.JsonPayloadType => PayloadType.UnknownType;

        void IJsonSerializable.WriteToJson(Utf8JsonWriter writer, WritedObjects objrefs)
        {
            writer.WriteString(nameof(ID), ID);
            writer.WriteNumber("Type", (int)NodeType);
            writer.WriteString("Text", Text);
            if (!(this is ModelNode))
            {
                writer.WritePropertyName("Nodes");
                writer.WriteStartArray();
                for (int i = 0; i < Nodes.Count; i++)
                {
                    writer.Serialize(Nodes[i], objrefs);
                }
                writer.WriteEndArray();
            }

            if (_checkoutInfo != null)
            {
                writer.WritePropertyName("CheckoutBy");
                if (IsCheckoutByMe)
                    writer.WriteStringValue("Me");
                else
                    writer.WriteStringValue(_checkoutInfo.DeveloperName);
            }

            WriteMembers(writer, objrefs);
        }

        /// <summary>
        /// 用于继承类重写。来写入子类的成员
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="objrefs"></param>
        public virtual void WriteMembers(Utf8JsonWriter writer, WritedObjects objrefs)
        { }

        void IJsonSerializable.ReadFromJson(ref Utf8JsonReader reader,
            ReadedObjects objrefs) => throw new NotSupportedException();
        #endregion

    }

    /// <summary>
    /// 子节点，添加时自动排序
    /// </summary>
    public sealed class NodeCollection
    {

        private DesignNode owner;
        private readonly List<DesignNode> nodes;

        public int Count => nodes.Count;

        public DesignNode this[int index] => nodes[index];

        public NodeCollection(DesignNode owner)
        {
            this.owner = owner;
            nodes = new List<DesignNode>();
        }

        public int Add(DesignNode item)
        {
            item.Parent = owner;
            //特定owner找到插入点
            if (owner != null && (
                owner.NodeType == DesignNodeType.ModelRootNode
             || owner.NodeType == DesignNodeType.FolderNode))
            {
                var index = -1;
                for (var i = 0; i < nodes.Count; i++)
                {
                    if (item.CompareTo(nodes[i]) < 0)
                    {
                        index = i;
                        break;
                    }
                }
                if (index != -1)
                {
                    nodes.Insert(index, item);
                    return index;
                }

                nodes.Add(item);
                return nodes.Count - 1;
            }

            nodes.Add(item);
            return nodes.Count - 1;
        }

        public void Remove(DesignNode item)
        {
            var index = this.nodes.IndexOf(item);
            if (index >= 0)
            {
                item.Parent = null;
                nodes.RemoveAt(index);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < this.nodes.Count; i++)
            {
                this.nodes[i].Parent = null;
            }
            this.nodes.Clear();
        }

        public DesignNode Find(Predicate<DesignNode> match)
        {
            return nodes.Find(match);
        }

        public bool Exists(Predicate<DesignNode> match)
        {
            return nodes.Exists(match);
        }

        public DesignNode[] ToArray()
        {
            return nodes.ToArray();
        }

    }
}
