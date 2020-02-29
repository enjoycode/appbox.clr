using System;
using appbox.Serialization;
using System.Text.Json;

namespace appbox.Models
{
    /// <summary>
    /// 视图模型
    /// </summary>
    public sealed class ViewModel : ModelBase, IJsonSerializable
    {

        #region ====Fields & Properties====
        public override ModelType ModelType => ModelType.View;

        public ViewModelFlag Flag { get; set; }

        /// <summary>
        /// 自定义路由的上级，指向视图名称eg: sys.Home
        /// </summary>
        public string RouteParent { get; set; }

        /// <summary>
        /// 自定义路由的路径，未定义则采用默认路径如: /erp/CustomerList
        /// 如设置RouteParent但不定义，则为/Parent/thisViewName
        /// </summary>
        public string RoutePath { get; set; }

        /// <summary>
        /// 列入路由或菜单所对应的权限模型标识
        /// </summary>
        public ulong PermissionID { get; set; }

        /// <summary>
        /// 仅用于模型存储
        /// </summary>
        internal string RouteStoredPath =>
            string.IsNullOrEmpty(RouteParent) ? RoutePath : $"{RouteParent};{RoutePath}";
        #endregion

        #region ====Ctor====
        /// <summary>
        /// Ctor for Serialization
        /// </summary>
        internal ViewModel() { }

        internal ViewModel(ulong id, string name) : base(id, name) { }
        #endregion

        #region ====Serialization Methods====
        public override void WriteObject(BinSerializer bs)
        {
            base.WriteObject(bs);

            bs.Write((byte)Flag, 1);
            bs.Write(PermissionID, 2);
            if (!string.IsNullOrEmpty(RoutePath))
                bs.Write(RoutePath, 3);
            if (!string.IsNullOrEmpty(RouteParent))
                bs.Write(RouteParent, 4);

            bs.Write((uint)0);
        }

        public override void ReadObject(BinSerializer bs)
        {
            base.ReadObject(bs);

            uint propIndex;
            do
            {
                propIndex = bs.ReadUInt32();
                switch (propIndex)
                {
                    case 1: Flag = (ViewModelFlag)bs.ReadByte(); break;
                    case 2: PermissionID = bs.ReadUInt64(); break;
                    case 3: RoutePath = bs.ReadString(); break;
                    case 4: RouteParent = bs.ReadString(); break;
                    case 0: break;
                    default: throw new Exception($"Deserialize_ObjectUnknownFieldIndex: {GetType().Name}");
                }
            } while (propIndex != 0);
        }

        PayloadType IJsonSerializable.JsonPayloadType => PayloadType.ViewModel;

        void IJsonSerializable.WriteToJson(Utf8JsonWriter writer, WritedObjects objrefs)
        {
            writer.WriteBoolean("Route", (Flag & ViewModelFlag.ListInRouter) == ViewModelFlag.ListInRouter);
            writer.WriteString("RouteParent", RouteParent);
            writer.WriteString("RoutePath", RoutePath);
        }

        void IJsonSerializable.ReadFromJson(ref Utf8JsonReader reader, ReadedObjects objrefs) => throw new NotSupportedException();
        #endregion

        #region ====导入方法====
        internal override bool UpdateFrom(ModelBase other)
        {
            var from = (ViewModel)other;
            bool changed = base.UpdateFrom(other);

            //同步属性
            Flag = from.Flag;
            RouteParent = from.RouteParent;
            RoutePath = from.RoutePath;
            PermissionID = from.PermissionID;

            return changed;
        }
        #endregion

    }

    [Flags]
    public enum ViewModelFlag : byte
    {
        None = 0,
        //列入路由
        ListInRouter = 1,
    }
}
