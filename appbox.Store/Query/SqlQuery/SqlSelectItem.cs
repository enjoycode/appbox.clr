using System;
using appbox.Expressions;

namespace appbox.Store
{

    /// <summary>
    /// 用于包装SelectItemExpression类，以便隐式转换表达式为SelectItem类
    /// </summary>
    public struct SqlSelectItem
    {
        public SqlSelectItemExpression Target { get; }

        public SqlSelectItem(Expression val)
        {
            //Todo: 是否判断val是否已是QuerySelect类型
            Target = new SqlSelectItemExpression(val);
        }

        public SqlSelectItem(Expression val, string aliasName)
        {
            Target = new SqlSelectItemExpression(val, aliasName);
        }

        public static implicit operator SqlSelectItem(Expression val)
        {
            return new SqlSelectItem(val);
        }

    }
}
