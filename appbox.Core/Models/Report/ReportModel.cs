using System;
using appbox.Serialization;

namespace appbox.Models
{
    public sealed class ReportModel : ModelBase
    {

        public override ModelType ModelType => ModelType.Report;

        /// <summary>
        /// 报表定义的版本号（目前保留用)
        /// </summary>
        public int ReportXmlVersion { get; private set; } = 0;

        #region ====Ctor====
        internal ReportModel() { }

        internal ReportModel(ulong id, string name) : base(id, name) { }
        #endregion

        #region ====Serialization====
        public override void WriteObject(BinSerializer bs)
        {
            base.WriteObject(bs);

            bs.Write(ReportXmlVersion, 1);

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
                    case 1: ReportXmlVersion = bs.ReadInt32(); break;
                    case 0: break;
                    default: throw new Exception($"Deserialize_ObjectUnknownFieldIndex: {GetType().Name}");
                }
            } while (propIndex != 0);
        }
        #endregion

        #region ====导入方法====
        internal override bool UpdateFrom(ModelBase other)
        {
            var from = (ReportModel)other;
            bool changed = base.UpdateFrom(other);

            //同步属性
            ReportXmlVersion = from.ReportXmlVersion;

            return changed;
        }
        #endregion


    }
}
