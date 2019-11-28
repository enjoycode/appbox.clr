using System;
using System.Threading.Tasks;

namespace appbox
{
    public static class SimplePerfTest
    {

        public static async Task<string> Run(int taskCount, int loopCount, Func<int, int, ValueTask> action)
        {
            var tasks = new Task[taskCount];
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            for (int i = 0; i < taskCount; i++)
            {
                var taskId = i;
                tasks[i] = Task.Run(async () =>
                {
                    for (int j = 0; j < loopCount; j++)
                    {
                        await action(taskId, j);
                    }
                });
            }
            await Task.WhenAll(tasks);
            sw.Stop();

            var countPerSecond = (int)(taskCount * loopCount * 1000 / sw.ElapsedMilliseconds);
            return $"调用{taskCount * loopCount}次共耗时: {sw.ElapsedMilliseconds}毫秒 平均每秒调用: {countPerSecond}\n";
        }

    }
}
