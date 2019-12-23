using System;
using appbox.Store;
using appbox.Expressions;
using System.Diagnostics;

//no namespace，省得转换

public static class DbFuncs
{
    public static DbFuncExpression Sum(MemberExpression field)
    {
        Debug.Assert(field.Type == ExpressionType.FieldExpression);
        return new DbFuncExpression(DbFuncName.Sum, field);
    }
}
