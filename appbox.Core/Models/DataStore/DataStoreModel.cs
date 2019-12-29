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

        /// <summary>
        /// 适用于结构化存储的表或字段的命名规则
        /// 另外每个表的自定义前缀由SqlStoreOptions设置, 方便DbFirst导入如Base_XXXX等Prefix的表名
        /// </summary>
        public DataStoreNameRules NameRules { get; set; }

        #region ====Ctor====
        internal DataStoreModel() { }

        internal DataStoreModel(DataStoreKind kind, string provider, string storeName) :
            base(unchecked((ulong)StringHelper.GetHashCode(storeName)), storeName) //注意使用一致性Hash产生Id
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
            bs.Write((byte)NameRules, 4);

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
                    case 4: NameRules = (DataStoreNameRules)bs.ReadByte(); break;
                    case 0: break;
                    default: throw new Exception("Deserialize_ObjectUnknownFieldIndex: " + GetType().Name);
                }
            } while (propIndex != 0);
        }
        #endregion

    }

    /// <summary>
    /// 用于表名称及列名称等命名规则
    /// </summary>
    [Flags]
    public enum DataStoreNameRules : byte
    {
        None = 0,
        /// <summary>
        /// 使用标识号作为名称(保留)
        /// </summary>
        UseIdAsName = 1,
        /// <summary>
        /// 表名称前附加App名称或标识, eg: sys.TableName
        /// 默认Sql存储使用此规则
        /// </summary>
        AppPrefixForTable = 2,
    }
}
