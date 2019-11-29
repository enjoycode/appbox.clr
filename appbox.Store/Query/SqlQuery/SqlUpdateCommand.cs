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

        public List<Expression> UpdateItems { get; }

        private List<MemberExpression> _outputItems;
        public List<MemberExpression> OutputItems
        {
            get
            {
                if (_outputItems == null)
                    _outputItems = new List<MemberExpression>();
                return _outputItems;
            }
        }

        public bool HasOutputItems
        {
            get { return _outputItems != null && _outputItems.Count > 0; }
        }

        private object[] _outputValues;
        public object[] OutputValues
        {
            get
            {
                if (!HasOutputItems)
                    throw new NotSupportedException("Has no output items.");
                if (_outputValues == null)
                    _outputValues = new object[_outputItems.Count];
                return _outputValues;
            }
        }
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
            UpdateItems.Add(assignment);
            return this;
        }

        public SqlUpdateCommand Update(MemberExpression target, Expression value)
        {
            UpdateItems.Add(target == value);
            return this;
        }

        public SqlUpdateCommand Output(MemberExpression target)
        {
            OutputItems.Add(target);
            return this;
        }

        public SqlUpdateCommand Where(Expression filter)
        {
            Filter = filter;
            return this;
        }
        #endregion

    }
}
