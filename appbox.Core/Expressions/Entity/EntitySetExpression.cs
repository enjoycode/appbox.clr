﻿using System;
using System.Linq.Expressions;
using System.Text;

namespace appbox.Expressions
{
    public sealed class EntitySetExpression : MemberExpression
    {

        public override ExpressionType Type => ExpressionType.EntitySetExpression;
        public override MemberExpression this[string name] => throw new InvalidOperationException();

        private readonly ulong _setModelId; //用于ThenInclude
        private EntityExpression _root; //用于ThenInclude
        internal EntityExpression RootEntityExpression
        {
            get
            {
                if (IsNull(_root))
                    _root = new EntityExpression(_setModelId, null/*必须null，后面设置*/);
                return _root;
            }
        }

        /// <summary>
        /// Ctor for Serialization
        /// </summary>
        internal EntitySetExpression() { }

        internal EntitySetExpression(string name, EntityExpression owner, ulong setModelId)
            : base(name, owner)
        {
            _setModelId = setModelId;
        }

        #region ====Overrides Methods====
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            EntitySetExpression target = obj as EntitySetExpression;
            if (Equals(null, target))
                return false;

            return Equals(target.Owner, Owner) && target.Name == Name;
        }

        public override string ToString()
        {
            return $"{Owner.ToString()}.{Name}";
        }

        public override void ToCode(StringBuilder sb, string preTabs)
        {
            Owner.ToCode(sb, preTabs);
            sb.Append(".");
            sb.Append(Name);
        }

        public override System.Linq.Expressions.Expression ToLinqExpression(IExpressionContext ctx)
        {
            throw new NotImplementedException();
        }
        #endregion

    }
}
