using System;
using appbox.Data;
using System.Threading;
using appbox.Runtime;
using appbox.Serialization;

namespace appbox.Models
{

    public sealed class ApplicationModel : IBinSerializable
    {

        //const uint MaxModelIDSeq = 2097152;

        #region ====Fields & Properties====
        public uint Id { get; private set; }

        public string Owner { get; private set; }

        public string Name { get; private set; }

        /// <summary>
        /// 映射至存储的编号，由EntityStore生成
        /// </summary>
        internal byte StoreId { get; set; }
        internal uint DevModelIdSeq { get; set; } //仅用于导入导出，注意导出前需要从存储刷新

        //private string _originalName;
        //internal string OriginalName => string.IsNullOrEmpty(_originalName) ? this.Name : _originalName;
        //internal bool IsNameChanged => !string.IsNullOrEmpty(_originalName) && _originalName != Name;
        #endregion

        #region ====Ctor====
        /// <summary>
        /// Ctor for serialization
        /// </summary>
        internal ApplicationModel() { }

        internal ApplicationModel(string owner, string name)
        {
            Name = name;
            Owner = owner;
            //生成Id,参考编码规则
            var hash = StringHelper.GetHashCode(owner) ^ StringHelper.GetHashCode(name);
            Id = (uint)hash;
        }

        /// <summary>
        /// 仅用于单元测试，因为NetCore的Hash不一致
        /// </summary>
        internal ApplicationModel(string owner, string name, uint id)
        {
            Name = name;
            Owner = owner;
            Id = id;
        }
        #endregion

        #region ====IBinSerializable====
        void IBinSerializable.WriteObject(BinSerializer bs)
        {
            bs.Write(Id, 1);
            bs.Write(DevModelIdSeq, 2);
            bs.Write(Owner, 3);
            bs.Write(Name, 4);
            bs.Write((uint)0);
        }

        void IBinSerializable.ReadObject(BinSerializer bs)
        {
            uint propIndex = 0;
            do
            {
                propIndex = bs.ReadUInt32();
                switch (propIndex)
                {
                    case 1: Id = bs.ReadUInt32(); break;
                    case 2: DevModelIdSeq = bs.ReadUInt32(); break;
                    case 3: Owner = bs.ReadString(); break;
                    case 4: Name = bs.ReadString(); break;
                    case 0: break;
                    default: throw new Exception($"Deserialize_ObjectUnknownFieldIndex: {GetType()}");
                }
            } while (propIndex != 0);
        }
        #endregion
    }

}
