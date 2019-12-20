using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Expressions;
using appbox.Models;

namespace appbox.Store
{
    /// <summary>
    /// 用于Eager or Explicit loading实体Navigation属性
    /// </summary>
    public sealed class SqlIncluder
    {
        /// <summary>
        /// Only EntityExpression | EntitySetExpression | SqlSelectItemExpression
        /// </summary>
        public Expression Expression { get; private set; }

        private MemberExpression MemberExpression
        {
            get
            {
                if (Expression.Type == ExpressionType.EntityExpression
                    || Expression.Type == ExpressionType.EntitySetExpression)
                {
                    return (MemberExpression)Expression;
                }
                return (MemberExpression)((SqlSelectItemExpression)Expression).Expression;
            }
        }

        /// <summary>
        /// 上级，根级为null
        /// </summary>
        public SqlIncluder Parent { get; private set; }

        public List<SqlIncluder> Childs { get; private set; }

        #region ====Ctor====
        /// <summary>
        /// 新建根级
        /// </summary>
        internal SqlIncluder(EntityExpression root)
        {
            Debug.Assert(Expression.IsNull(root.Owner));
            Expression = root;
        }

        /// <summary>
        /// 新建子级
        /// </summary>
        private SqlIncluder(SqlIncluder parent, Expression exp)
        {
            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
            Expression = exp;
        }
        #endregion

        #region ====Include Methods====
        public SqlIncluder Include(Func<EntityExpression, MemberExpression> selector, string alias = null)
        {
            var root = GetRoot();
            return root.IncludeInternal(selector((EntityExpression)root.Expression), alias);
        }

        //public SqlIncluder Include(MemberExpression member, string alias = null)
        //{
        //    return GetRoot().IncludeInternal(member, alias);
        //}

        public SqlIncluder ThenInclude(Func<EntityExpression, MemberExpression> selector, string alias = null)
        {
            if (Expression.Type == ExpressionType.EntitySetExpression)
                return IncludeInternal(selector(((EntitySetExpression)Expression).RootEntityExpression), alias);
            return IncludeInternal(selector((EntityExpression)Expression), alias);
        }

        //public SqlIncluder ThenInclude(MemberExpression member, string alias = null)
        //{
        //    return IncludeInternal(member, alias);
        //}

        private SqlIncluder IncludeInternal(MemberExpression member, string alias = null)
        {
            //检查当前是否Field，是则不再允许Include其他
            if (MemberExpression.Type == ExpressionType.FieldExpression)
                throw new NotSupportedException();
            if (member.Type == ExpressionType.FieldExpression)
            {
                //可以包含多个层级如t.Customer.Region.Name
                //if (!ReferenceEquals(GetTopOnwer(member), MemberExpression))
                //    throw new ArgumentException("Owner not same");
                if (ReferenceEquals(member.Owner, MemberExpression))
                    throw new ArgumentException("Can't include field");
                //TODO:判断alias空，是则自动生成eg:t.Customer.Region.Name => CustomerRegionName
                //TODO:判断重复
                if (Childs == null) Childs = new List<SqlIncluder>();
                var res = new SqlIncluder(this, new SqlSelectItemExpression(member, alias));
                Childs.Add(res);
                return res;
            }
            else //EntityRef or EntitySet
            {
                Debug.Assert(member.Type == ExpressionType.EntityExpression
                    || member.Type == ExpressionType.EntitySetExpression);
                //判断Include的Owner是否相同
                //if (!ReferenceEquals(member.Owner, MemberExpression))
                //    throw new ArgumentException("Owner not same");
                if (Childs == null)
                {
                    var child = new SqlIncluder(this, member);
                    Childs = new List<SqlIncluder> { child };
                    return child;
                }

                var found = Childs.FindIndex(t => t.Expression.Type == member.Type
                                             && t.MemberExpression.Name == member.Name);
                if (found >= 0)
                    return Childs[found];
                var res = new SqlIncluder(this, member);
                Childs.Add(res);
                return res;
            }
        }

        private EntityExpression GetTopOnwer(MemberExpression member)
        {
            if (Expression.IsNull(member.Owner.Owner))
                return member.Owner;
            return GetTopOnwer(member.Owner);
        }

        private SqlIncluder GetRoot()
        {
            return Parent == null ? this : Parent.GetRoot();
        }
        #endregion

        #region ====Runtime Methods====
        internal async ValueTask AddSelects(SqlQuery query, EntityModel model, string path = null)
        {
            if (Childs == null) return;
            for (int i = 0; i < Childs.Count; i++)
            {
                await Childs[i].LoopAddSelects(query, model, path);
            }
        }

        private async ValueTask LoopAddSelects(SqlQuery query, EntityModel model, string path)
        {
            if (Expression.Type == ExpressionType.EntityExpression)
            {
                var exp = (EntityExpression)Expression;
                var mm = (EntityRefModel)model.GetMember(exp.Name, true);
                if (mm.IsAggregationRef) //TODO:聚合引用转换为Case表达式
                    throw new NotImplementedException();

                //注意替换入参支持多级
                model = await Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(mm.RefModelIds[0]);
                path = path == null ? exp.Name : $"{path}.{exp.Name}";
                SqlQuery.AddAllSelects(query, model, exp, path);
            }
            else if (Expression.Type == ExpressionType.SelectItemExpression)
            {
                query.AddSelectItem((SqlSelectItemExpression)Expression);
            }
            else
            {
                return;
            }

            if (Childs == null) return;
            for (int i = 0; i < Childs.Count; i++)
            {
                await Childs[i].LoopAddSelects(query, model, path);
            }
        }

        internal async ValueTask LoadEntitySets(SqlStore db, Entity owner, System.Data.Common.DbTransaction txn)
        {
            if (Childs == null) return;
            for (int i = 0; i < Childs.Count; i++)
            {
                await Childs[i].LoopLoadEntitySets(db, owner, txn);
            }
        }

        private async ValueTask LoopLoadEntitySets(SqlStore db, Entity owner, System.Data.Common.DbTransaction txn)
        {
            if (Expression.Type == ExpressionType.EntitySetExpression)
            {
                var list = await db.LoadEntitySet(owner, (EntitySetExpression)Expression, this, txn);
                //继续加载子级
                if (Childs == null) return;
                for (int i = 0; i < Childs.Count; i++)
                {
                    if (Childs[i].Expression.Type == ExpressionType.EntitySetExpression)
                    {
                        for (int j = 0; j < list.Count; j++)
                        {
                            await Childs[i].LoopLoadEntitySets(db, list[j], txn);
                        }
                    }
                }
            }
        }
        #endregion
    }
}
