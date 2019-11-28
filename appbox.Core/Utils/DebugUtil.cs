using System.Diagnostics;
using appbox.Caching;

namespace appbox
{
    public static class DebugUtil
    {

        //Usage: DebugUtil.DumpStacks(new StackTrace().GetFrames());
        public static void DumpStacks(StackFrame[] stacks)
        {
            var sb = StringBuilderCache.Acquire();
            System.Console.WriteLine("========Stacks Begin========");
            foreach (StackFrame stack in stacks)
            {
                sb.AppendLine($"{stack.GetFileName()} {stack.GetFileLineNumber()} {stack.GetFileColumnNumber()} {stack.GetMethod().ToString()}");
            }
            System.Console.WriteLine(StringBuilderCache.GetStringAndRelease(sb));
            System.Console.WriteLine("========Stacks  End ========");
        }
    }
}
