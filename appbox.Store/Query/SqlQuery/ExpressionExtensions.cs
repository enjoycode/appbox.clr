using appbox.Store;
using appbox.Expressions;

//注意：no namespace以方便服务代码编译
public static class ExpressionExtensions
{
    public static SqlSelectItem SelectAs(this Expression exp, string aliasName)
    {
        return new SqlSelectItem(exp, aliasName);
    }
}
