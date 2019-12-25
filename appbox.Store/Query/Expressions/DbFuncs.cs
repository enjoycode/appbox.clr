using System;
using appbox.Store;
using appbox.Expressions;
using System.Diagnostics;

//no namespace，省得转换

public static class DbFuncs
{
    public static DbFuncExpression Sum(Expression field)
    {
        return new DbFuncExpression(DbFuncName.Sum, field);
    }

    public static DbFuncExpression Avg(Expression field)
    {
        return new DbFuncExpression(DbFuncName.Avg, field);
    }
}
