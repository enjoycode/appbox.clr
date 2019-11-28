using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using appbox.Models;
using appbox.Serialization;

namespace appbox.Data
{
    public sealed class EntityList : Collection<Entity>, IEntityParent, IBinSerializable
    {

        #region ====Fields & Properties====
        private ulong _entityModelId;

        internal EntitySetModel EntitySetMember { get; private set; }

        private List<Entity> _deletedItems;

        /// <summary>
        /// 已删除的实体集合
        /// </summary>
        public List<Entity> DeletedItems
        {
            get
            {
                if (_deletedItems == null)
                    _deletedItems = new List<Entity>();
                return _deletedItems;
            }
        }
        #endregion

        #region ====Ctor===
        internal EntityList() { }

        /// <summary>
        /// 初始化普通实体列表
        /// </summary>
        internal EntityList(ulong entityModelId)
        {
            _entityModelId = entityModelId;
        }

        /// <summary>
        /// 初始化用于EntitySet成员的实体列表
        /// </summary>
        internal EntityList(Entity parent, EntitySetModel entitySetMember)
        {
            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
            EntitySetMember = entitySetMember;
        }

        /// <summary>
        /// 初始化树状结构根目录列表
        /// </summary>
        internal EntityList(EntitySetModel entitySetMember) //TODO:排序表达式
        {
            EntitySetMember = entitySetMember ?? throw new ArgumentNullException(nameof(entitySetMember));
        }
        #endregion

        #region ====Override Methods====
        protected override void InsertItem(int index, Entity item)
        {
            //插入分以下几种情况：
            //1、Add添加新对象
            //2、从查询结果内加载旧对象，包括树状结构查询（排序?）
            //3、移动对象由Move(int,int)方法内处理，这里不用考虑
            //4、树状结构子对象从其他地方移至这里

            //如果属于Entity，则应设置item的相应属性
            if (Parent != null)
            {
                //处理行号,1、树状全部直接加载，根据查询结果是否排序另行处理（先填充再排序，或直接查询排序）；
                //        2、延迟加载无需处理行号，已排序；
                //        3、新建的成员行号处理；
                //        4、树状结构移入的成员处理
                /*if (item.PersistentState == PersistentState.Detached)
                {
                    this.AutoSetRowNumber(index, item, SetMemberModel.RefRowNumberMemberName);
                }
                else*/
                if (item.PersistentState == PersistentState.Deleted) //表示树状结构间的移动
                {
                    //先从原有的Owner的EntityList的已删除列表内移除，表示树状结构移动后重新插入
                    //注意：必须判断item原来是否是根级，如判断item.Parent是否有值
                    ref EntityMember entityRefMember = ref item.GetMember(EntitySetMember.RefMemberId);
                    if (entityRefMember.HasValue) //非根级
                    {
                        Entity entityRef = (Entity)entityRefMember.ObjectValue;
                        entityRef.GetEntitySet(EntitySetMember.MemberId).DeletedItems.Remove(item);
                    }
                    else //item原来即是根级，即从根级移至某一子级下
                    {
                        (item.Parent as EntityList)?.DeletedItems.Remove(item);
                    }
                    item.PersistentState = PersistentState.Modified;
                    //this.AutoSetRowNumber(index, item, SetMemberModel.RefRowNumberMemberName);
                }//结束新建或删除状态判断
                item.Parent = this;
                item.SetEntityRef(EntitySetMember.RefMemberId, (Entity)Parent);
            }
            else //------------可能是树状结构根目录 或 普通实体列表------------------
            {
                /*if (item.PersistentState == PersistentState.Detached)
                {
                    if (SetMemberModel != null)
                        this.AutoSetRowNumber(index, item, SetMemberModel.RefRowNumberMemberName);
                }
                else*/
                if (item.PersistentState == PersistentState.Deleted)
                {
                    if (EntitySetMember != null) //表示树状结构根目录
                    {
                        ref EntityMember entityRefMember = ref item.GetMember(EntitySetMember.RefMemberId);
                        if (entityRefMember.HasValue) //即从某一子级移至根级，当前对象的上级的子级的EntityList列表内已删除的对象
                        {
                            Entity entityRef = (Entity)entityRefMember.ObjectValue;
                            entityRef.GetEntitySet(EntitySetMember.MemberId).DeletedItems.Remove(item);
                        }
                        else //表示根级间移动
                        {
                            (item.Parent as EntityList)?.DeletedItems.Remove(item);
                        }
                    }
                    else //表示普通列表
                    {
                        (item.Parent as EntityList)?.DeletedItems.Remove(item);
                    }//结束判断树状结构根目录

                    item.PersistentState = PersistentState.Modified;
                    //if (SetMemberModel != null)
                        //this.AutoSetRowNumber(index, item, SetMemberModel.RefRowNumberMemberName);
                } //结束新建或删除状态判断

                item.Parent = this;
            }

            //调用基类插入对象
            base.InsertItem(index, item);
            //报告变更
            Parent?.OnChildListChanged(this);
        }

        public void Move(int oldIndex, int newIndex)
        {
            Entity item = this[oldIndex];
            base.RemoveItem(oldIndex); //注意：必须调用基类的移除方法

            //if (Parent != null)
            //{
            //    this.AutoSetRowNumber(newIndex, item, SetMemberModel.RefRowNumberMemberName);
            //}
            base.InsertItem(newIndex, item); //注意：必须调用基类的插入方法

            Parent?.OnChildListChanged(this);
        }

        protected override void RemoveItem(int index)
        {
            Entity item = this[index];
            //注意：不能设置当前对象的上级对象引用为Null,因为需要考虑树状结构间的对象移动时，先移除后插入时需要用到

            if (item.PersistentState != PersistentState.Detached)
            {
                item.PersistentState = PersistentState.Deleted;
                DeletedItems.Add(item);
            }
            base.RemoveItem(index);

            Parent?.OnChildListChanged(this);
        }

        protected override void ClearItems()
        {
            //TODO:待优化，暂简单逐个删除
            for (int i = Count - 1; i >= 0; i--)
            {
                RemoveAt(i);
            }
        }

        protected override void SetItem(int index, Entity item)
        {
            throw new NotSupportedException("EntityList not supported SetItem() Method.");

            //如果属于EntitySet，先移除原有Item的相关属性
            //base.SetItem(index, item);
        }
        #endregion

        #region ====IEntityParent Impl====
        public IEntityParent Parent { get; internal set; }

        public void OnChildListChanged(EntityList childList)
        {
            //throw new NotImplementedException();
        }

        public void OnChildPropertyChanged(Entity child, ushort childPropertyId)
        {
            //throw new NotImplementedException();
        }
        #endregion

        #region ====Serialization====
        void IBinSerializable.WriteObject(BinSerializer bs)
        {
            bs.Write(_entityModelId);
            //序列化删除列表
            bs.WriteList(_deletedItems);
            //序列化EntitySet
            //bs.Serialize(this._parent);
            bs.Write(EntitySetMember != null);
            if (EntitySetMember != null)
            {
                bs.Write(EntitySetMember.Owner.Id);
                bs.Write(EntitySetMember.MemberId);
            }
            //序列化内部列表
            bs.Write(Count);
            for (int i = 0; i < Count; i++)
            {
                bs.Serialize(this[i]);
            }
        }

        void IBinSerializable.ReadObject(BinSerializer bs)
        {
            _entityModelId = bs.ReadUInt64();
            _deletedItems = bs.ReadList<Entity>();
            //_parent = (Entity)bs.Deserialize();
            bool hasSetMemberModel = bs.ReadBoolean();
            if (hasSetMemberModel)
            {
                var ownerModel = Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(bs.ReadUInt64()).Result;
                EntitySetMember = (EntitySetModel)ownerModel.GetMember(bs.ReadUInt16(), true);
            }
            int count = bs.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                base.InsertItem(Count, (Entity)bs.Deserialize());
            }
        }
        #endregion
    }
}
