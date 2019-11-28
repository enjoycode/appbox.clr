using System;
using System.Collections.Generic;
using appbox.Serialization;

namespace appbox.Models
{
    public class ServiceModel : ModelBase
    {

        #region ====Fields & Properties====
        public override ModelType ModelType => ModelType.Service;

        private List<string> _references;
        /// <summary>
        /// 服务依赖项列表，不包含扩展名
        /// eg: System.Net.Http
        /// </summary>
        public List<string> References
        {
            get
            {
                if (_references == null)
                    _references = new List<string>();
                return _references;
            }
        }

        public bool HasReference => _references != null && _references.Count > 0;
        #endregion

        #region ====Ctor====
        internal ServiceModel() { }

        internal ServiceModel(ulong id, string name) : base(id, name) { }
        #endregion

        #region ====Serialization====
        public override void WriteObject(BinSerializer bs)
        {
            base.WriteObject(bs);

            if (HasReference)
                bs.WriteList(_references, 1);

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
                    case 1: _references = bs.ReadList<string>(); break;
                    case 0: break;
                    default: throw new Exception($"Deserialize_ObjectUnknownFieldIndex: {GetType().Name}");
                }
            } while (propIndex != 0);
        }
        #endregion

        #region ====导入方法====
        //public override bool UpdateFrom(ModelBase other)
        //{
        //    var from = (ServiceModel)other;
        //    bool changed = base.UpdateFrom(other);

        //    //同步属性
        //    this._dummyCode = from._dummyCode;
        //    this._sourceCode = from._sourceCode;
        //    this._references = from._references;
        //    this._assembly = from._assembly;

        //    return changed;
        //}
        #endregion

    }
}
