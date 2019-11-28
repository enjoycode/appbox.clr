using System;
using System.Diagnostics;
using System.Collections.Generic;
using appbox.Data;
using appbox.Models;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace appbox.Store
{
    /// <summary>
    /// 用于Eager or Explicit loading实体Navigation属性
    /// </summary>
    public sealed class Includer
    {

        public ulong EntityModelId { get; private set; }

        public EntityMemberType MemberType { get; private set; }
        public ushort MemberId1 { get; private set; }
        public ushort MemberId2 { get; private set; } //仅用于直接包含引用字段
        public ushort MemberId3 { get; private set; } //仅用于直接包含引用字段
        public string AliasName { get; private set; } //导航成员为null

        /// <summary>
        /// 上级，根级为null
        /// </summary>
        public Includer Parent { get; private set; }

        internal List<Includer> Childs { get; private set; }

        /// <summary>
        /// 新建根级
        /// </summary>
        internal Includer(ulong entityModelId)
        {
            EntityModelId = entityModelId;
            MemberType = EntityMemberType.EntityRef;
        }

        /// <summary>
        /// 新建子级
        /// </summary>
        private Includer(Includer parent, EntityMemberType memberType, ushort memberId)
        {
            EnsureIsNavigationMember(memberType);

            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
            MemberType = memberType;
            MemberId1 = memberId;
        }

        private Includer(Includer parent, string alias, ushort mid1, ushort mid2, ushort mid3 = 0)
        {
            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
            MemberType = EntityMemberType.DataField; //仅DataField
            AliasName = alias;
            MemberId1 = mid1;
            MemberId2 = mid2;
            MemberId3 = mid3;
        }

        /// <summary>
        /// 包含导航属性（EntityRef or EntitySet）
        /// </summary>
        public Includer Include(EntityMemberType memberType, ushort memberId)
        {
            EnsureIsNavigationMember(MemberType);

            if (Childs == null)
            {
                var res1 = new Includer(this, memberType, memberId);
                Childs = new List<Includer> { res1 };
                return res1;
            }

            var found = Childs.FindIndex(t => t.MemberId1 == memberId);
            if (found >= 0)
                return Childs[found];

            var res = new Includer(this, memberType, memberId);
            Childs.Add(res);
            return res;
        }

        /// <summary>
        /// 直接包含引用对象的字段,如order=>order.Customer.Name, 执行时作为Entity实例的附加成员
        /// </summary>
        /// <param name="alias">引用对象的成员的别名</param>
        public Includer Include(string alias, ushort mid1, ushort mid2, ushort mid3 = 0)
        {
            EnsureIsNavigationMember(MemberType);

            if (Childs == null)
                Childs = new List<Includer>();
            //TODO:考虑判断重复
            var res = new Includer(this, alias, mid1, mid2, mid3);
            Childs.Add(res);
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnsureIsNavigationMember(EntityMemberType memberType)
        {
            if (memberType != EntityMemberType.EntityRef && memberType != EntityMemberType.EntitySet)
                throw new InvalidOperationException("Only EntityRef and EntitySet can include other");
        }

        internal async ValueTask LoadAsync(Entity owner, ReadonlyTransaction txn)
        {
            Debug.Assert(owner != null);
            if (MemberType == EntityMemberType.EntityRef)
            {
                if (Parent == null) //表示根级
                {
                    if (Childs != null && Childs.Count > 0)
                    {
                        for (int i = 0; i < Childs.Count; i++) //TODO:并发执行
                        {
                            await Childs[i].LoadAsync(owner, txn);
                        }
                    }
                }
                else
                {
                    await LoadEntityRefAsync(owner, txn);
                }
            }
            else if (MemberType == EntityMemberType.EntitySet)
            {
                throw new NotImplementedException();
            }
            else
            {
                await LoadFieldAsync(owner, txn);
            }
        }

        private async ValueTask LoadEntityRefAsync(Entity owner, ReadonlyTransaction txn)
        {
            var target = await LoadFieldPath(owner, MemberId1, txn);
            owner.InitEntityRefForLoad(MemberId1, target);

            if (target != null && Childs != null && Childs.Count > 0)
            {
                for (int i = 0; i < Childs.Count; i++) //TODO:并发执行
                {
                    await Childs[i].LoadAsync(target, txn);
                }
            }
        }

        private async ValueTask LoadFieldAsync(Entity owner, ReadonlyTransaction txn)
        {
            //TODO:*****暂简单实现，待存储引擎实现Select指定字段集后修改
            var path1 = await LoadFieldPath(owner, MemberId1, txn);
            if (path1 == null)
            {
                owner.AddAttached(AliasName, null);
                return;
            }

            var mm2 = path1.Model.GetMember(MemberId2, true);
            if (mm2.Type != EntityMemberType.EntityRef)
            {
                owner.AddAttached(AliasName, path1.GetMember(MemberId2).BoxedValue);
                return;
            }

            var path2 = await LoadFieldPath(path1, MemberId2, txn);
            if (path2 == null)
            {
                owner.AddAttached(AliasName, null);
                return;
            }

            Debug.Assert(path2.Model.GetMember(MemberId3, true).Type != EntityMemberType.EntityRef);
            owner.AddAttached(AliasName, path2.GetMember(MemberId3).BoxedValue);
        }

        private static async ValueTask<Entity> LoadFieldPath(Entity owner, ushort memberId, ReadonlyTransaction txn)
        {
            //TODO:从事务缓存内先查找是否存在
            var refModel = (EntityRefModel)owner.Model.GetMember(memberId, true);
            var refId = owner.GetEntityId(refModel.IdMemberId);
            if (refId == null) return null;

            ulong refModelId = refModel.RefModelIds[0];
            if (refModel.IsAggregationRef)
                refModelId = owner.GetUInt64(refModel.TypeMemberId);

            return await EntityStore.LoadAsync(refModelId, refId);
        }
    }
}
