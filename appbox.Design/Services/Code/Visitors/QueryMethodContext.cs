using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace appbox.Design
{
    /// <summary>
    /// 用于映射Lambda表达式的参数至相应的QueryMethod的变量名
    /// </summary>
    internal sealed class QueryMethod
    {

        public string MethodName;

        public bool IsSystemQuery; //标明是否系统存储查询，否则表示其他如Sql查询

        public int ArgsCount; //参数数量，用于确定是ToEntityList还是ToDynamicList

        public IdentifierNameSyntax[] Identifiers; //实际指向的参数目标

        public string[] LambdaParameters; // (t, j1, j2) => {}

        public bool InLambdaExpression;

        //保留参数，仅Join及Include相关
        public bool HoldLambdaArgs => MethodName == "LeftJoin" || MethodName == "RightJoin"
                || MethodName == "InnerJoin" || MethodName == "FullJoin" || IsIncludeMethod;

        internal bool IsIncludeMethod => MethodName == "Include" || MethodName == "ThenInclude";

        internal bool IsDynamicMethod => ArgsCount > 0
            && (MethodName == TypeHelper.SqlQueryToListMethod
                || MethodName == TypeHelper.SqlUpdateOutputMethod);

        internal IdentifierNameSyntax ReplaceLambdaParameter(IdentifierNameSyntax identifier)
        {
            //Include不用处理
            if (IsIncludeMethod) return identifier;

            var index = Array.IndexOf(LambdaParameters, identifier.Identifier.ValueText);
            if (index >= 0)
            {
                return Identifiers[index]; //替换的目标
            }

            return null;
        }

    }

    internal sealed class QueryMethodContext
    {
        private readonly Stack<QueryMethod> methodsStack = new Stack<QueryMethod>();

        internal bool HasAny => methodsStack.Count > 0;

        internal QueryMethod Current => methodsStack.Peek();

        internal void Push(QueryMethod method) => methodsStack.Push(method);

        internal void Pop() => methodsStack.Pop();

    }
}
