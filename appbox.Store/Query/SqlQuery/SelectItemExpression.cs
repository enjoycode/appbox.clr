using appbox.Expressions;

namespace appbox.Store
{
    internal class SelectItemExpression
    {
        private MemberExpression memberExpression;

        public SelectItemExpression(MemberExpression memberExpression)
        {
            this.memberExpression = memberExpression;
        }
    }
}