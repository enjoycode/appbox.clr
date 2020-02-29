using System;
using System.Collections.Generic;
using appbox.Data;
using appbox.Serialization;
using System.Text.Json;

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
        /// 是否强制外键约束
        /// </summary>
        public bool IsForeignKeyConstraint { get; private set; } = true;

        /// <summary>
        /// 引用的实体模型标识号集合，聚合引用有多个
        /// </summary>
        public List<ulong> RefModelIds { get; private set; }

        /// <summary>
        /// 引用的外键成员标识集合，
        /// 1. SysStore只有一个Id, eg: Order->Customer为Order.CustomerId
        /// 2. SqlStore有一或多个，与引用目标的主键的数量、顺序、类型一致
        /// </summary>
        public ushort[] FKMemberIds { get; private set; }

        /// <summary>
        /// 聚合引用时的类型字段，存储引用目标的EntityModel.Id
        /// </summary>
        public ushort TypeMemberId { get; private set; }

        /// <summary>
        /// 是否聚合引用至不同的实体模型
        /// </summary>
        public bool IsAggregationRef => TypeMemberId != 0;

        public EntityRefActionRule UpdateRule { get; private set; } = EntityRefActionRule.Cascade;

        public EntityRefActionRule DeleteRule { get; private set; } = EntityRefActionRule.NoAction;

        #endregion

        #region ====Ctor====
        internal EntityRefModel() { RefModelIds = new List<ulong>(); }

        /// <summary>
        /// 设计时新建非聚合引用成员
        /// </summary>
        internal EntityRefModel(EntityModel owner, string name, ulong refModelId, 
            ushort[] fkMemberIds, bool foreignConstraint = true) : base(owner, name)
        {
            if (fkMemberIds == null || fkMemberIds.Length == 0)
                throw new ArgumentNullException(nameof(fkMemberIds));

            RefModelIds = new List<ulong> { refModelId };
            IsReverse = false;
            FKMemberIds = fkMemberIds;
            TypeMemberId = 0;
            IsForeignKeyConstraint = foreignConstraint;
        }

        /// <summary>
        /// 设计时新建聚合引用成员
        /// </summary>
        internal EntityRefModel(EntityModel owner, string name, List<ulong> refModelIds,
            ushort[] fkMemberIds, ushort typeMemberId, bool foreignConstraint = true) : base(owner, name)
        {
            if (fkMemberIds == null || fkMemberIds.Length == 0)
                throw new ArgumentNullException(nameof(fkMemberIds));
            if (refModelIds == null || refModelIds.Count <= 0)
                throw new ArgumentNullException(nameof(refModelIds));

            RefModelIds = refModelIds;
            IsReverse = false;
            FKMemberIds = fkMemberIds;
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
            bs.Write(IsForeignKeyConstraint, 4);
            bs.Write(TypeMemberId, 6);
            bs.Write((byte)UpdateRule, 7);
            bs.Write((byte)DeleteRule, 8);

            bs.Write(3u);
            bs.Write(RefModelIds.Count);
            for (int i = 0; i < RefModelIds.Count; i++)
            {
                bs.Write(RefModelIds[i]);
            }

            bs.Write(5u);
            bs.Write(FKMemberIds.Length);
            for (int i = 0; i < FKMemberIds.Length; i++)
            {
                bs.Write(FKMemberIds[i]);
            }

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
                    case 1: IsReverse = bs.ReadBoolean(); break;
                    case 4: IsForeignKeyConstraint = bs.ReadBoolean(); break;
                    case 5:
                        {
                            int count = bs.ReadInt32();
                            FKMemberIds = new ushort[count];
                            for (int i = 0; i < count; i++)
                            {
                                FKMemberIds[i] = bs.ReadUInt16();
                            }
                        }
                        break;
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
                    case 7: UpdateRule = (EntityRefActionRule)bs.ReadByte(); break;
                    case 8: DeleteRule = (EntityRefActionRule)bs.ReadByte(); break;
                    case 0: break;
                    default: throw new Exception($"Deserialize_ObjectUnknownFieldIndex: {GetType().Name}");
                }
            } while (propIndex != 0);
        }

        protected override void WriteMembers(Utf8JsonWriter writer, WritedObjects objrefs)
        {
            writer.WriteBoolean(nameof(IsReverse), IsReverse);
            writer.WriteBoolean(nameof(IsAggregationRef), IsAggregationRef);
            writer.WriteBoolean(nameof(IsForeignKeyConstraint), IsForeignKeyConstraint);

            writer.WritePropertyName(nameof(RefModelIds));
            writer.WriteList(RefModelIds, objrefs);
        }
        #endregion

        #region ====导入方法====
        internal override void UpdateFrom(EntityMemberModel from)
        {
            base.UpdateFrom(from);
            //TODO:聚合引用添加或删除处理，以及规则变更等
        }
        #endregion
    }

    public enum EntityRefActionRule : byte
    {
        NoAction = 0,
        Cascade = 1,
        SetNull = 2
    }
}
