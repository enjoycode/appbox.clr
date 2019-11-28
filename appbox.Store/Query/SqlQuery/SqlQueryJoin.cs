using System;
using appbox.Expressions;

namespace appbox.Store
{
    public sealed class SqlQueryJoin : SqlQueryBase, ISqlQueryJoin
    {

        /// <summary>
        /// Query Target
        /// </summary>
        public EntityExpression T { get; }


        /// <summary>
        /// Ctor for Serialization
        /// </summary>
        internal SqlQueryJoin() { }

        public SqlQueryJoin(ulong entityModelID)
        {
            T = new EntityExpression(entityModelID, this);
        }

    }
}
