using System;
using System.Collections.Generic;
using appbox.Serialization;

namespace appbox.Models
{
    /// <summary>
    /// 枚举模型
    /// </summary>
    public sealed class EnumModel : ModelBase
    {
        #region ====Fields & Properties====
        public override ModelType ModelType => ModelType.Enum;

        public bool IsFlag { get; set; }

        public string Comment { get; set; }

        public List<EnumModelItem> Items { get; private set; } = new List<EnumModelItem>();
        #endregion

        #region ====Ctor====
        internal EnumModel() { }

        internal EnumModel(ulong id, string name) : base(id, name) { }
        #endregion

        #region ====Serialization====
        public override void WriteObject(BinSerializer bs)
        {
            base.WriteObject(bs);

            bs.Write(IsFlag, 1);
            if (!string.IsNullOrEmpty(Comment))
                bs.Write(Comment, 2);
            if (Items != null && Items.Count > 0)
                bs.WriteList(Items, 3);

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
                    case 1: IsFlag = bs.ReadBoolean(); break;
                    case 2: Comment = bs.ReadString(); break;
                    case 3: Items = bs.ReadList<EnumModelItem>(); break;
                    case 0: break;
                    default: throw new Exception($"Deserialize_ObjectUnknownFieldIndex: {GetType().Name}");
                }
            } while (propIndex != 0);
        }
        #endregion

        #region ====导入方法====
        internal override bool UpdateFrom(ModelBase other)
        {
            var from = (EnumModel)other;
            bool changed = base.UpdateFrom(other);

            IsFlag = from.IsFlag;
            Comment = from.Comment;
            //暂简单实现，清空并重新添加枚举项
            Items.Clear();
            Items.AddRange(from.Items);
            return changed;
        }
        #endregion
    }
}
