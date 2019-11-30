using System;
using System.Collections.Generic;
using appbox.Data;
using appbox.Serialization;
using Newtonsoft.Json;

namespace appbox.Models
{
    public sealed class EntityRefModel : EntityMemberModel
    {
        #region ====Fields & Properties====
        public override EntityMemberType Type => EntityMemberType.EntityRef;

        /// <summary>
        /// 是否反向引用 eg: A->B , B->A(反向)
        /// </summary>
        public bool IsReverse { get; private set; }

        /// <summary>
        /// 是否聚合引用至不同的实体模型
        /// </summary>
        /// <remarks>不能根据RefModelIds数量判断，因运行时可能再添加聚合引用目标</remarks>
        public bool IsAggregationRef { get; private set; }

        /// <summary>
        /// 是否强制外键约束
        /// </summary>
        public bool IsForeignKeyConstraint { get; private set; } = true;

        /// <summary>
        /// 引用实体模型标识号集合
        /// </summary>
        public List<ulong> RefModelIds { get; private set; }

        public ushort IdMemberId { get; private set; }

        /// <summary>
        /// 聚合引用时的类型字段，存储引用目标的EntityModel.Id
        /// </summary>
        public ushort TypeMemberId { get; private set; }
        #endregion

        #region ====Ctor====
        internal EntityRefModel() { RefModelIds = new List<ulong>(); }

        /// <summary>
        /// 设计时新建非聚合引用成员
        /// </summary>
        internal EntityRefModel(EntityModel owner, string name, ulong refModelId, 
            ushort idMemberId, bool foreignConstraint = true) : base(owner, name)
        {
            RefModelIds = new List<ulong> { refModelId };
            IsAggregationRef = false;
            IsReverse = false;
            IdMemberId = idMemberId;
            IsForeignKeyConstraint = foreignConstraint;
        }

        /// <summary>
        /// 设计时新建聚合引用成员
        /// </summary>
        internal EntityRefModel(EntityModel owner, string name, List<ulong> refModelIds, 
            ushort idMemberId, ushort typeMemberId, bool foreignConstraint = true) : base(owner, name)
        {
            if (refModelIds == null || refModelIds.Count <= 0)
                throw new ArgumentNullException(nameof(refModelIds));
            RefModelIds = refModelIds;
            IsAggregationRef = true;
            IsReverse = false;
            IdMemberId = idMemberId;
            TypeMemberId = typeMemberId;
            IsForeignKeyConstraint = foreignConstraint;
        }
        #endregion

        #region ====Runtime Methods====
        internal override void InitMemberInstance(Entity owner, ref EntityMember member)
        {
            member.Id = MemberId;
            member.MemberType = EntityMemberType.EntityRef;
            member.Flag.AllowNull = AllowNull;
            //member.Flag.HasValue = !AllowNull;
        }
        #endregion

        #region ====Serialization====
        public override void WriteObject(BinSerializer bs)
        {
            base.WriteObject(bs);

            bs.Write(IsReverse, 1);
            bs.Write(IsAggregationRef, 2);
            bs.Write(IsForeignKeyConstraint, 4);
            bs.Write(IdMemberId, 5);
            bs.Write(TypeMemberId, 6);

            bs.Write(3u);
            bs.Write(RefModelIds.Count);
            for (int i = 0; i < RefModelIds.Count; i++)
            {
                bs.Write(RefModelIds[i]);
            }

            bs.Write((uint)0);
        }

        public override void ReadObject(BinSerializer bs)
        {
            base.ReadObject(bs);

            uint propIndex = 0;
            do
            {
                propIndex = bs.ReadUInt32();
                switch (propIndex)
                {
                    case 1: IsReverse = bs.ReadBoolean(); break;
                    case 2: IsAggregationRef = bs.ReadBoolean(); break;
                    case 4: IsForeignKeyConstraint = bs.ReadBoolean(); break;
                    case 5: IdMemberId = bs.ReadUInt16(); break;
                    case 6: TypeMemberId = bs.ReadUInt16(); break;
                    case 3:
                        {
                            int count = bs.ReadInt32();
                            for (int i = 0; i < count; i++)
                            {
                                RefModelIds.Add(bs.ReadUInt64());
                            }
                        }
                        break;
                    case 0: break;
                    default: throw new Exception($"Deserialize_ObjectUnknownFieldIndex: {GetType().Name}");
                }
            } while (propIndex != 0);
        }

        protected override void WriteMembers(JsonTextWriter writer, WritedObjects objrefs)
        {
            writer.WritePropertyName(nameof(IsReverse));
            writer.WriteValue(IsReverse);

            writer.WritePropertyName(nameof(IsAggregationRef));
            writer.WriteValue(IsAggregationRef);

            writer.WritePropertyName(nameof(IsForeignKeyConstraint));
            writer.WriteValue(IsForeignKeyConstraint);

            writer.WritePropertyName(nameof(RefModelIds));
            writer.WriteList(RefModelIds, objrefs);
        }
        #endregion
    }
}
