using System;
using System.Collections.Generic;
using appbox.Expressions;

namespace appbox.Store
{
    /// <summary>
    /// 用于更新满足指定条件的记录，支持同时返回指定字段值
    /// </summary>
    public class SqlUpdateCommand : SqlQueryBase, ISqlQuery
    {
        #region ====Fields====
        /// <summary>
        /// Query Target
        /// </summary>
        public EntityExpression T { get; }

        /// <summary>
        /// 筛选器
        /// </summary>
        public Expression Filter { get; set; }

        /// <summary>
        /// 更新表达式 TODO:使用BlockExpression支持多个t=>{t.V1=t.V1+1; t.V2=t.V2+2}
        /// </summary>
        public List<Expression> UpdateItems { get; }

        /// <summary>
        /// 更新同时输出的成员
        /// </summary>
        public MemberExpression[] OutputItems { get; private set; }

        /// <summary>
        /// 用于回调设置输出结果
        /// </summary>
        internal Action<SqlRowReader> SetOutputs;
        #endregion

        #region ====Ctor====
        public SqlUpdateCommand(ulong entityModelID)
        {
            UpdateItems = new List<Expression>();
            T = new EntityExpression(entityModelID, this);
        }
        #endregion

        #region ====Methods====
        /// <summary>
        /// 仅用于虚拟代码直接生成的表达式
        /// </summary>
        public SqlUpdateCommand Update(Expression assignment)
        {
            //TODO:验证
            UpdateItems.Add(assignment);
            return this;
        }

        public SqlUpdateCommand Update(MemberExpression target, Expression value)
        {
            //TODO:验证
            UpdateItems.Add(target == value);
            return this;
        }

        public UpdateOutputs<TResult> Output<TResult>(Func<SqlRowReader, TResult> selector,
            params MemberExpression[] selectItem)
        {
            //TODO:验证Selected members
            OutputItems = selectItem;
            var res = new UpdateOutputs<TResult>(selector);
            SetOutputs = res.OnResults;
            return res;
        }

        public SqlUpdateCommand Where(Expression filter)
        {
            Filter = filter;
            return this;
        }
        #endregion

        public sealed class UpdateOutputs<T>
        {
            private readonly Func<SqlRowReader, T> selector;
            private readonly IList<T> values = new List<T>();

            public T this[int index] => values[index];

            public int Count => values.Count;

            internal UpdateOutputs(Func<SqlRowReader, T> selector)
            {
                this.selector = selector;
            }

            internal void OnResults(SqlRowReader reader)
            {
                values.Add(selector(reader));
            }
        }
    }
}
