using System;
using appbox.Data;
using appbox.Serialization;
using System.Text.Json;

namespace appbox.Models
{
    /// <summary>
    /// 仅用于上层标识索引构建状态，底层各分区状态机异步变更完后保存状态标记
    /// </summary>
    /// <remarks>
    /// 注意与存储层IndexState记录的编码一致
    /// </remarks>
    public enum EntityIndexState : byte
    {
        Ready = 0,        //索引已构建完
        Building = 1,     //索引正在构建中
        BuildFailed = 2,  //比如违反惟一性约束或超出长度限制导致异步生成失败
    }

    /// <summary>
    /// 二级索引
    /// </summary>
    public sealed class EntityIndexModel : IndexModelBase //rename to SysIndexModel?
    {
        /// <summary>
        /// 是否全局索引,不支持非Mvcc表全局索引无法保证一致性
        /// </summary>
        public bool Global { get; private set; }
        /// <summary>
        /// 索引异步构建状态
        /// </summary>
        public EntityIndexState State { get; internal set; } 

        //TODO: property IncludeNull

        #region ====Ctor====
        internal EntityIndexModel() { }

        internal EntityIndexModel(EntityModel owner, string name, bool unique,
            FieldWithOrder[] fields, ushort[] storingFields = null)
            : base (owner, name, unique, fields, storingFields)
        {
            Global = false;
            State = owner.PersistentState == PersistentState.Detached ? EntityIndexState.Ready : EntityIndexState.Building;
        }
        #endregion

        #region ====Serialization====
        public override void WriteObject(BinSerializer bs)
        {
            base.WriteObject(bs);

            bs.Write(Global, 1);
            bs.Write((byte)State, 2);
            bs.Write(0u);
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
                    case 1: Global = bs.ReadBoolean(); break;
                    case 2: State = (EntityIndexState)bs.ReadByte(); break;
                    case 0: break;
                    default: throw new Exception($"Deserialize_ObjectUnknownFieldIndex: {GetType().Name} at {propIndex} ");
                }
            } while (propIndex != 0);
        }

        public override void WriteToJson(Utf8JsonWriter writer, WritedObjects objrefs)
        {
            base.WriteToJson(writer, objrefs);

            writer.WriteBoolean(nameof(Global), Global);
            writer.WriteString(nameof(State), State.ToString());
        }
        #endregion
    }
}
