using System;
using System.Linq.Expressions;

namespace appbox.Store
{
    sealed class KVScanExpressionContext : Expressions.IExpressionContext
    {
        internal static readonly KVScanExpressionContext Default = new KVScanExpressionContext();

        private readonly ParameterExpression vp;
        private readonly ParameterExpression vs;
        private readonly ParameterExpression mv;
        private readonly ParameterExpression ts;

        KVScanExpressionContext()
        {
            vp = Expression.Parameter(typeof(IntPtr), "vp");
            vs = Expression.Parameter(typeof(int), "vs");
            mv = Expression.Parameter(typeof(bool), "mv");
            ts = Expression.Parameter(typeof(ulong), "ts");
        }

        public ParameterExpression GetParameter(string paraName)
        {
            return paraName switch
            {
                "vp" => vp,
                "vs" => vs,
                "mv" => mv,
                "ts" => ts,
                _ => throw new Exception($"unknow parameter name: {paraName}"),
            };
        }
    }
}
