using System;
using System.IO;
using System.Buffers;
using System.Diagnostics;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json;

namespace appbox.Core.Tests
{
    public class Utf8JsonWriterTest
    {

        private readonly ITestOutputHelper output;

        public Utf8JsonWriterTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void VSNewtonJson()
        {
            var count = 1000000;
            var ms = new MemoryStream(1024);

            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                ms.Position = 0;
                using (var sw = new StreamWriter(ms, System.Text.Encoding.UTF8, 1024, true))
                {
                    //TODO:抽象公共部分至InvokeHelper
                    //注意: 不catch异常，序列化错误由Channel发送处理
                    using (var jw = new JsonTextWriter(sw))
                    {
                        //jw.DateTimeZoneHandling = DateTimeZoneHandling.Local;
                        jw.WriteStartObject();
                        jw.WritePropertyName("I");
                        jw.WriteValue(1);
                        jw.WritePropertyName("E");
                        jw.WriteValue("This is a exception");
                        jw.WriteEndObject();
                    }
                }
            }
            stopwatch.Stop();
            output.WriteLine($"Newtonsoft:\t{stopwatch.ElapsedMilliseconds}"); //884

            stopwatch = Stopwatch.StartNew();
            //var ujw = new Utf8JsonWriter(ms);
            for (int i = 0; i < count; i++)
            {
                ms.Position = 0;
                //ujw.Reset(ms);
                using (var ujw = new Utf8JsonWriter(ms))
                {
                    ujw.WriteStartObject();
                    ujw.WritePropertyName("I");
                    ujw.WriteNumberValue(1);
                    ujw.WritePropertyName("E");
                    ujw.WriteStringValue("This is a exception");
                    ujw.WriteEndObject();
                    ujw.Flush();
                }
            }
            stopwatch.Stop();
            output.WriteLine($"Utf8Json:\t{stopwatch.ElapsedMilliseconds}"); //393
        }

    }
}
