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
        /// 新建指向EntityRef的子级
        /// </summary>
        private SqlIncluder(SqlIncluder parent, EntityExpression entityRef)
        {
            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
            Expression = entityRef;
        }

        /// <summary>
        /// 新建指向EntitySet的子级
        /// </summary>
        private SqlIncluder(SqlIncluder parent, EntitySetExpression entitySet)
        {
            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
            Expression = entitySet;
        }

        /// <summary>
        /// 新建DataField子级
        /// </summary>
        /// <param name="field">可以包含多个层级如t.Customer.Region.Name</param>
        /// <param name="aliasName">自动或手工指定的别名如CustomerRegionName</param>
        private SqlIncluder(SqlIncluder parent, FieldExpression field, string aliasName)
        {
            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
            Expression = new SqlSelectItemExpression(field, aliasName);
        }
        #endregion

        #region ====Include Methods====
        /// <summary>
        /// Include EntityRef
        /// </summary>
        public SqlIncluder Include(EntityExpression entityRef)
        {
            if (MemberExpression.Type == ExpressionType.FieldExpression)
                throw new NotSupportedException();
            if (!ReferenceEquals(entityRef.Owner, MemberExpression.Owner))
                throw new ArgumentException();

            if (Childs == null)
            {
                var res1 = new SqlIncluder(this, entityRef);
                Childs = new List<SqlIncluder> { res1 };
                return res1;
            }

            var found = Childs.FindIndex(t => t.Expression.Type == ExpressionType.EntityExpression
                                         && t.MemberExpression.Name == entityRef.Name);
            if (found >= 0)
                return Childs[found];

            var res = new SqlIncluder(this, entityRef);
            Childs.Add(res);
            return res;
        }

        /// <summary>
        /// Include EntitySet
        /// </summary>
        public SqlIncluder Include(EntitySetExpression entitySet)
        {
            if (MemberExpression.Type == ExpressionType.FieldExpression)
                throw new NotSupportedException();
            if (!ReferenceEquals(entitySet.Owner, MemberExpression.Owner))
                throw new ArgumentException();

            if (Childs == null)
            {
                var res1 = new SqlIncluder(this, entitySet);
                Childs = new List<SqlIncluder> { res1 };
                return res1;
            }

            var found = Childs.FindIndex(t => t.Expression.Type == ExpressionType.EntitySetExpression
                                         && t.MemberExpression.Name == entitySet.Name);
            if (found >= 0)
                return Childs[found];

            var res = new SqlIncluder(this, entitySet);
            Childs.Add(res);
            return res;
        }

        public SqlIncluder Include(string alias, FieldExpression field)
        {
            if (MemberExpression.Type == ExpressionType.FieldExpression)
                throw new NotSupportedException();
            if (!ReferenceEquals(GetTopOnwer(field), MemberExpression.Owner))
                throw new ArgumentException();

            if (Childs == null)
                Childs = new List<SqlIncluder>();
            //TODO:判断重复
            var res = new SqlIncluder(this, field, alias);
            Childs.Add(res);
            return res;
        }

        private EntityExpression GetTopOnwer(MemberExpression member)
        {
            if (Expression.IsNull(member.Owner.Owner))
                return member.Owner;
            return GetTopOnwer(member.Owner);
        }
        #endregion

        #region ====Runtime Methods====
        internal async ValueTask AddSelects(SqlQuery query, EntityModel model)
        {
            if (Parent != null) //排除根级
            {
                if (Expression.Type == ExpressionType.EntityExpression)
                {
                    var exp = (EntityExpression)Expression;
                    var mm = (EntityRefModel)model.GetMember(exp.Name, true);
                    if (mm.IsAggregationRef) //TODO:聚合引用转换为Case表达式
                        throw new NotImplementedException();

                    var refModel = await Runtime.RuntimeContext.Current.GetModelAsync<EntityModel>(mm.RefModelIds[0]);
                    SqlQuery.AddAllSelects(query, refModel, exp, exp.Name);
                }
                else if (Expression.Type == ExpressionType.SelectItemExpression)
                {
                    query.AddSelectItem((SqlSelectItemExpression)Expression);
                }
            }

            if (Childs == null) return;
            for (int i = 0; i < Childs.Count; i++)
            {
                await Childs[i].AddSelects(query, model);
            }
        }

        internal async ValueTask LoadEntitySets(Entity owner)
        {
            if (Parent != null && Expression.Type == ExpressionType.EntitySetExpression)
            {
                //TODO:
            }

            if (Childs == null) return;
            for (int i = 0; i < Childs.Count; i++)
            {
                await Childs[i].LoadEntitySets(owner);
            }
        }
        #endregion
    }
}
