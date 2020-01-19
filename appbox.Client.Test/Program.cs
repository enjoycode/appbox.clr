using System;
using System.Threading.Tasks;
using appbox.Client.Channel;

namespace appbox.Client.Test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var channel = new WSChannel("10.211.55.3:5000");
            await channel.LoginAsync("Admin", "760wb");
            for (int i = 0; i < 100; i++)
            {
                var res = await channel.InvokeAsync<string>("sys.HelloService.SayHello", "[]");
                Console.WriteLine($"Invoke done, res = {res}");
            }
        }
    }
}
