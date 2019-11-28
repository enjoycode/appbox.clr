using System;
using System.ComponentModel;
using appbox.Serialization;
using Newtonsoft.Json;

namespace appbox.Models
{
    /// <summary>
    /// 视图模型
    /// </summary>
    public sealed class ViewModel : ModelBase, IJsonSerializable
    {

        #region ====Fields & Properties====
        public override ModelType ModelType => ModelType.View;

        /// <summary>
        /// 编译好的运行时代码，包含样式，以json形式存在
        /// eg: {"Code":"jscode", "Style":"style"}
        /// </summary>
        //public string RuntimeCode { get; set; }

        public ViewModelFlag Flag { get; set; }

        /// <summary>
        /// 自定义路由的路径，未定义则采用默认路径如: /ERP/CustomerList
        /// </summary>
        public string RoutePath { get; set; }

        /// <summary>
        /// 列入路由或菜单所对应的权限模型标识
        /// </summary>
        public ulong PermissionID { get; set; }
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
            bs.Write(RoutePath, 3);

            bs.Write((uint)0);
        }

        public override void ReadObject(BinSerializer bs)
        {
            base.ReadObject(bs);

            uint propIndex = 0;
            do
            {
                propIndex = bs.ReadUInt32();
                switch (propIndex)
                {
                    case 1: Flag = (ViewModelFlag)bs.ReadByte(); break;
                    case 2: PermissionID = bs.ReadUInt64(); break;
                    case 3: RoutePath = bs.ReadString(); break;
                    case 0: break;
                    default: throw new Exception($"Deserialize_ObjectUnknownFieldIndex: {GetType().Name}");
                }
            } while (propIndex != 0);
        }

        PayloadType IJsonSerializable.JsonPayloadType => PayloadType.ViewModel;

        void IJsonSerializable.WriteToJson(JsonTextWriter writer, WritedObjects objrefs)
        {
            writer.WritePropertyName("Route");
            if ((Flag & ViewModelFlag.ListInRouter) == ViewModelFlag.ListInRouter)
                writer.WriteValue(true);
            else
                writer.WriteValue(false);

            writer.WritePropertyName("RoutePath");
            writer.WriteValue(RoutePath);
        }

        void IJsonSerializable.ReadFromJson(JsonTextReader reader, ReadedObjects objrefs)
        {
            throw new NotSupportedException();
        }
        #endregion

        #region ====导入方法====
        //public override bool UpdateFrom(ModelBase other)
        //{
        //    var from = (ViewModel)other;
        //    bool changed = base.UpdateFrom(other);

        //    //同步属性
        //    this.SourceCode = from.SourceCode;
        //    this.RuntimeCode = from.RuntimeCode;
        //    this.Flag = from.Flag;
        //    this.PermissionID = from.PermissionID;
        //    this.RoutePath = from.RoutePath;

        //    return changed;
        //}
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
