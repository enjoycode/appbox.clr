using System;
using System.Collections.Generic;
using appbox.Data;
using appbox.Serialization;
using Newtonsoft.Json;

namespace appbox.Models
{
    public sealed class EntitySetModel : EntityMemberModel
    {
        #region ====Fields & Properties====
        public override EntityMemberType Type => EntityMemberType.EntitySet;

        /// <summary>
        /// 引用的实体模型标识号，如Order->OrderDetail，则指向OrderDetail的模型标识
        /// </summary>
        public ulong RefModelId { get; private set; }

        /// <summary>
        /// 引用的EntityRef成员标识，如Order->OrderDetail，则指向OrderDetail.Order成员标识
        /// </summary>
        public ushort RefMemberId { get; private set; }
        #endregion

        #region ====Ctor====
        internal EntitySetModel() { }

        /// <summary>
        /// 设计时新建EntitySet成员
        /// </summary>
        internal EntitySetModel(EntityModel owner, string name, ulong refModelId, ushort refMemberId) : base(owner, name)
        {
            RefModelId = refModelId;
            RefMemberId = refMemberId;
            AllowNull = true; //always true
        }
        #endregion

        #region ====Runtime Methods====
        internal override void InitMemberInstance(Entity owner, ref EntityMember member)
        {
            member.Id = MemberId;
            member.MemberType = EntityMemberType.EntitySet;
        }
        #endregion

        #region ====Serialization====
        public override void WriteObject(BinSerializer bs)
        {
            base.WriteObject(bs);

            bs.Write(RefModelId, 1);
            bs.Write(RefMemberId, 2);

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
                    case 1: RefModelId = bs.ReadUInt64(); break;
                    case 2: RefMemberId = bs.ReadUInt16(); break;
                    case 0: break;
                    default: throw new Exception($"Deserialize_ObjectUnknownFieldIndex: {GetType().Name}");
                }
            } while (propIndex != 0);
        }

        protected override void WriteMembers(JsonTextWriter writer, WritedObjects objrefs)
        {
            writer.WritePropertyName(nameof(RefModelId));
            writer.WriteValue(RefModelId);

            writer.WritePropertyName(nameof(RefMemberId));
            writer.WriteValue(RefMemberId);
        }
        #endregion
    }
}
