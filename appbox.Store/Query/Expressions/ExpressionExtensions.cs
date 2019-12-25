using System.Collections.Generic;
using System.Linq;
using appbox.Store;
using appbox.Expressions;

//注意：no namespace以方便服务代码编译
public static class ExpressionExtensions
{
    public static SqlSelectItem SelectAs(this Expression exp, string aliasName)
    {
        return new SqlSelectItem(exp, aliasName);
    }

    //----以下两个暂放在这里----
    public static bool In<T>(this T source, IEnumerable<T> list)
    {
        return list.Contains(source);
    }

    public static bool NotIn<T>(this T source, IEnumerable<T> list)
    {
        return !list.Contains(source);
    }
}
