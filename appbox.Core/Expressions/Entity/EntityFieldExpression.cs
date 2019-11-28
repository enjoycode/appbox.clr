using System;
using System.Text;

namespace appbox.Expressions
{
    public sealed class FieldExpression : MemberExpression //TODO: rename
    {

        public override ExpressionType Type => ExpressionType.FieldExpression;

        public override MemberExpression this[string name] => throw new InvalidOperationException();

        /// <summary>
        /// Ctor for Serialization
        /// </summary>
        internal FieldExpression() { }

        internal FieldExpression(string name, EntityExpression owner) : base(name, owner) { }

        #region ====Overrides====
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            FieldExpression target = obj as FieldExpression;
            if (Equals(null, target))
                return false;

            return Equals(target.Owner, Owner) && target.Name == Name;
        }

        public override int GetHashCode()
        {
            return Owner.GetHashCode() ^ Name.GetHashCode();
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
