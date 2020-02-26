using System;
using System.Collections.Generic;
using appbox.Models;
using appbox.Serialization;
using appbox.Server;

namespace appbox.Design
{
    /// <summary>
    /// 用于导入与导出的模型包
    /// </summary>
    sealed class AppPackage : PublishPackage, IAppPackage, IBinSerializable
    {
        public ApplicationModel Application { get; set; }

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
                    case 0: break;
                    default: throw new Exception($"Deserialize_ObjectUnknownFieldIndex: {GetType().Name} at {propIndex}");
                }
            } while (propIndex != 0);
        }
        #endregion
    }
}
