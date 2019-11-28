using System;
using System.Collections.Generic;
using appbox.Expressions;

namespace appbox.Store
{
    public interface ISqlQueryJoin
    {
        bool HasJoins { get; }

        List<SqlJoin> Joins { get; }

        ISqlQueryJoin InnerJoin(ISqlQueryJoin target, Expression onCondition);

        ISqlQueryJoin LeftJoin(ISqlQueryJoin target, Expression onCondition);

        ISqlQueryJoin RightJoin(ISqlQueryJoin target, Expression onCondition);

        ISqlQueryJoin FullJoin(ISqlQueryJoin target, Expression onCondition);

    }
}
