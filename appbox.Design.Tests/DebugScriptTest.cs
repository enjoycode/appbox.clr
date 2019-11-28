using System;
using System.Collections.Generic;
using Xunit;
using appbox.Design;
using appbox.Runtime;
using appbox.Models;
using System.Threading.Tasks;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace appbox.Design.Tests
{
    public class DebugScriptTest
    {
        [Fact]
        public void DebugExpressionTest()
        {
            //string expr = "emp.ToString()";
            string expr = "Console.WriteLine()";

            SyntaxAnalyzer sa = new SyntaxAnalyzer(expr);
            StringBuilder scriptText = new StringBuilder("#line hidden\n");

            // Generate prefix with variables assignment to __context members
            foreach (string us in sa.unresolvedSymbols)
                scriptText.AppendFormat("var {0} = __context.{0};\n", us);

            //手工添加
            //scriptText.AppendFormat("var {0} = __context.{0};\n", "emp");

            scriptText.Append("#line 1\n");
            scriptText.Append(expr);

            try
            {
                var scriptOptions = ScriptOptions.Default
                    .WithImports("System")
                    .WithReferences(typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo).Assembly);
                var script = CSharpScript.Create(scriptText.ToString(), scriptOptions, globalsType: typeof(Globals));
                script.Compile();
                //编译没有问题
                var gbl = new Globals();
                //gbl.__context.emp = "Hello"; //不行
                //gbl.emp = "Hello";
                var returnValue = script.RunAsync(gbl).Result.ReturnValue;
                if (returnValue == null)
                    Console.WriteLine("Result is null");
                else
                    Console.WriteLine(returnValue.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    public class Globals
    {
        public dynamic __context;
        public string emp;
    }

    // Stores unresolved symbols, now only variables are supported
    // Symbols are unique in list
    class SyntaxAnalyzer
    {
        class FrameVars
        {
            Stack<string> vars = new Stack<string>();
            Stack<int> varsInFrame = new Stack<int>();
            int curFrameVars = 0;

            public void Add(string name)
            {
                vars.Push(name);
                curFrameVars++;
            }

            public void NewFrame()
            {
                varsInFrame.Push(curFrameVars);
                curFrameVars = 0;
            }

            public void ExitFrame()
            {
                for (int i = 0; i < curFrameVars; i++)
                    vars.Pop();

                curFrameVars = varsInFrame.Pop();
            }

            public bool Contains(string name)
            {
                return vars.Contains(name);
            }
        }

        enum ParsingState
        {
            Common,
            InvocationExpression,
            GenericName
        };

        public List<string> unresolvedSymbols { get; private set; } = new List<string>();
        FrameVars frameVars = new FrameVars();
        SyntaxTree tree;

        public SyntaxAnalyzer(string expression)
        {
            tree = CSharpSyntaxTree.ParseText(expression, options: new CSharpParseOptions(kind: SourceCodeKind.Script));
            var root = tree.GetCompilationUnitRoot();
            foreach (SyntaxNode sn in root.ChildNodes())
                ParseNode(sn, ParsingState.Common);
        }

        void ParseAccessNode(SyntaxNode sn, ParsingState state)
        {
            SyntaxNodeOrToken snt = sn.ChildNodesAndTokens().First();

            if (snt.Kind().Equals(SyntaxKind.SimpleMemberAccessExpression))
                ParseAccessNode(snt.AsNode(), state);
            else if (snt.IsNode)
                ParseNode(snt.AsNode(), state);
            else if (snt.IsToken)
                ParseCommonToken(snt.AsToken(), state);
        }

        void ParseBlock(SyntaxNode sn, ParsingState state)
        {
            frameVars.NewFrame();
            foreach (SyntaxNode snc in sn.ChildNodes())
                ParseNode(sn, ParsingState.Common);
            frameVars.ExitFrame();
        }


        void ParseNode(SyntaxNode sn, ParsingState state)
        {
            if (sn.Kind().Equals(SyntaxKind.InvocationExpression))
                state = ParsingState.InvocationExpression;
            else if (sn.Kind().Equals(SyntaxKind.GenericName))
                state = ParsingState.GenericName;
            else if (sn.Kind().Equals(SyntaxKind.ArgumentList))
                state = ParsingState.Common;

            foreach (SyntaxNodeOrToken snt in sn.ChildNodesAndTokens())
            {
                if (snt.IsNode)
                {
                    if (snt.Kind().Equals(SyntaxKind.SimpleMemberAccessExpression))
                        ParseAccessNode(snt.AsNode(), state);
                    //else if (snt.Kind().Equals(SyntaxKind.InvocationExpression)) //尝试解决a.Invoke()不解析a
                    //    ParseNode(((InvocationExpressionSyntax)snt.AsNode()).Expression, state);
                    else if (snt.Kind().Equals(SyntaxKind.Block))
                        ParseBlock(snt.AsNode(), state);
                    else
                        ParseNode(snt.AsNode(), state);
                }
                else
                {
                    if (sn.Kind().Equals(SyntaxKind.VariableDeclarator))
                        ParseDeclarator(snt.AsToken());
                    else
                        ParseCommonToken(snt.AsToken(), state);
                }
            }
        }

        void ParseCommonToken(SyntaxToken st, ParsingState state)
        {
            if (state == ParsingState.InvocationExpression ||
                state == ParsingState.GenericName)
                return;

            if (st.Kind().Equals(SyntaxKind.IdentifierToken) &&
                !unresolvedSymbols.Contains(st.Value.ToString()) &&
                !frameVars.Contains(st.Value.ToString()))
                unresolvedSymbols.Add(st.Value.ToString());
        }

        void ParseDeclarator(SyntaxToken st)
        {
            if (st.Kind().Equals(SyntaxKind.IdentifierToken) && !frameVars.Contains(st.Value.ToString()))
                frameVars.Add(st.Value.ToString());
        }
    };
}
