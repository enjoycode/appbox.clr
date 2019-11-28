using System;
using System.Collections.Generic;
using appbox.Serialization;

namespace appbox.Models
{
    /// <summary>
    /// 权限模型用于关联受控的资源与授权的组织单元
    /// </summary>
    public sealed class PermissionModel : ModelBase
    {

        public override ModelType ModelType => ModelType.Permission;

        private List<Guid> _orgUnits;
        public List<Guid> OrgUnits
        {
            get
            {
                if (_orgUnits == null)
                    _orgUnits = new List<Guid>();
                return _orgUnits;
            }
        }
        public bool HasOrgUnits => _orgUnits != null && _orgUnits.Count > 0;

        public int SortNum { get; internal set; }

        public string Remark { get; set; }

        internal PermissionModel() { }

        internal PermissionModel(ulong id, string name) : base(id, name) { }

        #region ====Serialization====
        public override void WriteObject(BinSerializer bs)
        {
            base.WriteObject(bs);

            bs.Write(SortNum, 1);
            if (_orgUnits != null && _orgUnits.Count > 0)
            {
                bs.Write((uint)2);
                bs.Write(_orgUnits.Count);
                for (int i = 0; i < _orgUnits.Count; i++)
                {
                    bs.Write(_orgUnits[i]);
                }
            }
            bs.Write(Remark, 3);

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
                    case 1: SortNum = bs.ReadInt32(); break;
                    case 2:
                        {
                            int count = bs.ReadInt32();
                            _orgUnits = new List<Guid>(count);
                            for (int i = 0; i < count; i++)
                            {
                                _orgUnits.Add(bs.ReadGuid());
                            }
                        }
                        break;
                    case 3: Remark = bs.ReadString(); break;
                    case 0: break;
                    default: throw new Exception($"Deserialize_ObjectUnknownFieldIndex: {GetType().Name}");
                }
            } while (propIndex != 0);
        }
        #endregion

        #region ====导入方法====
        //public override bool UpdateFrom(ModelBase other)
        //{
        //    var from = (PermissionModel)other;
        //    bool changed = base.UpdateFrom(other);
        //    this._sortNo = from._sortNo;
        //    //注意：不更新已指派的组织单元
        //    return changed;
        //}
        #endregion
    }
}
