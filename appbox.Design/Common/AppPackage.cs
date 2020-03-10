using System;
using System.Collections.Generic;
using appbox.Models;
using appbox.Serialization;
using appbox.Server;

namespace appbox.Design
{
    /// <summary>
    /// 用于导入导出的模型包
    /// </summary>
    sealed class AppPackage : PublishPackage, IAppPackage, IBinSerializable
    {
        public ApplicationModel Application { get; set; }

        /// <summary>
        /// 用于导入时判断相应的数据库是否存在
        /// </summary>
        public List<DataStoreInfo> DataStores { get; private set; } = new List<DataStoreInfo>();

        internal AppPackage() : base() { }

        #region ====Serialization====
        public void WriteObject(BinSerializer bs)
        {
            bs.Serialize(Application, 1);
            bs.WriteList(Folders, 2);
            bs.WriteList(Models, 3);
            bs.WriteDictionary(SourceCodes, 4);
            bs.WriteDictionary(ServiceAssemblies, 5);
            bs.WriteDictionary(ViewAssemblies, 6);
            bs.WriteList(DataStores, 7);

            bs.Write(0u);
        }

        public void ReadObject(BinSerializer bs)
        {
            uint propIndex;
            do
            {
                propIndex = bs.ReadUInt32();
                switch (propIndex)
                {
                    case 1: Application = (ApplicationModel)bs.Deserialize(); break;
                    case 2: Folders = bs.ReadList<ModelFolder>(); break;
                    case 3: Models = bs.ReadList<ModelBase>(); break;
                    case 4: SourceCodes = bs.ReadDictionary<ulong, byte[]>(); break;
                    case 5: ServiceAssemblies = bs.ReadDictionary<string, byte[]>(); break;
                    case 6: ViewAssemblies = bs.ReadDictionary<string, byte[]>(); break;
                    case 7: DataStores = bs.ReadList<DataStoreInfo>(); break;
                    case 0: break;
                    default: throw new Exception($"Deserialize_ObjectUnknownFieldIndex: {GetType().Name} at {propIndex}");
                }
            } while (propIndex != 0);
        }
        #endregion
    }

    /// <summary>
    /// 应用包所用到的数据库信息
    /// </summary>
    sealed class DataStoreInfo : IBinSerializable
    {
        public ulong Id { get; set; }
        public string Name { get; set; }
        public DataStoreKind Kind { get; set; }
        //考虑精确匹配数据库提供者的属性，用于利用某数据库特性的应用(eg:只能用PGSQL)

        public override string ToString()
        {
            return $"Name={Name} Kind={Kind}";
        }

        #region ====Serialization====
        public void WriteObject(BinSerializer bs)
        {
            bs.Write(Id, 1);
            bs.Write(Name, 2);
            bs.Write((byte)Kind, 3);
            bs.Write(0u);
        }

        public void ReadObject(BinSerializer bs)
        {
            uint propIndex;
            do
            {
                propIndex = bs.ReadUInt32();
                switch (propIndex)
                {
                    case 1: Id = bs.ReadUInt64(); break;
                    case 2: Name = bs.ReadString(); break;
                    case 3: Kind = (DataStoreKind)bs.ReadByte(); break;
                    case 0: break;
                    default: throw new Exception($"Deserialize_ObjectUnknownFieldIndex: {GetType().Name} at {propIndex}");
                }
            } while (propIndex != 0);
        }
        #endregion
    }
}
