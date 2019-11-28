using System;
using appbox.Serialization;

namespace appbox.Models
{
    /// <summary>
    /// 描述内置存储外的第三方数据源
    /// </summary>
    public sealed class DataStoreModel : ModelBase
    {
        public override ModelType ModelType => ModelType.DataStore;

        public DataStoreKind Kind { get; private set; }

        /// <summary>
        /// 存储的提供者，eg: AppBox.Server.AliOSS;AppBox.Server.AliOSSStore
        /// </summary>
        public string Provider { get; private set; }

        /// <summary>
        /// 用于存储如ConnectionString等相关配置，json格式以方便前端使用
        /// </summary>
        public string Settings { get; set; }

        #region ====Ctor====
        internal DataStoreModel() { }

        internal DataStoreModel(DataStoreKind kind, string provider, string storeName) :
            base((ulong)StringHelper.GetHashCode(storeName), storeName) //注意使用一致性Hash产生Id
        {
            Kind = kind;
            Provider = provider;
        }
        #endregion

        #region ====Serialization====
        public override void WriteObject(BinSerializer bs)
        {
            base.WriteObject(bs);

            bs.Write((byte)Kind, 1);
            bs.Write(Provider, 2);
            bs.Write(Settings, 3);

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
                    case 1: Kind = (DataStoreKind)bs.ReadByte(); break;
                    case 2: Provider = bs.ReadString(); break;
                    case 3: Settings = bs.ReadString(); break;
                    case 0: break;
                    default: throw new Exception("Deserialize_ObjectUnknownFieldIndex: " + GetType().Name);
                }
            } while (propIndex != 0);
        }
        #endregion

    }
}
