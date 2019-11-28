using System;

namespace appbox.Expressions
{
    public interface IExpressionContext
    {
        System.Linq.Expressions.ParameterExpression GetParameter(string paraName);
    }
}
