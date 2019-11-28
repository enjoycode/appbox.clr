using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using Xunit;
using Newtonsoft.Json;

namespace appbox.Core.Tests
{
    public class PromQLReaderTest
    {


        [Fact]
        public void ParsePromQLResult()
        {
            var json = Resources.GetString("Resources.PromQLResult.json");
            using (var sr = new System.IO.StringReader(json))
            using (var jr = new JsonTextReader(sr))
            {
                var series = ParseToSeries(jr);
                Console.WriteLine(series.Count);
                var seriesJson = JsonConvert.SerializeObject(series);
                Console.WriteLine(seriesJson);
            }
        }

        private static List<List<double[]>> ParseToSeries(JsonTextReader jr)
        {
            if (!jr.Read() || jr.TokenType != JsonToken.StartObject) throw new Exception();
            if (!jr.Read() || jr.TokenType != JsonToken.PropertyName || (string)jr.Value != "status")
                throw new Exception();
            var status = jr.ReadAsString();
            if (status != "success") throw new Exception();
            if (!jr.Read() || jr.TokenType != JsonToken.PropertyName || (string)jr.Value != "data")
                throw new Exception();

            if (!jr.Read() || jr.TokenType != JsonToken.StartObject) throw new Exception();
            if (!jr.Read() || jr.TokenType != JsonToken.PropertyName || (string)jr.Value != "resultType")
                throw new Exception();
            var resultType = jr.ReadAsString();
            if (!jr.Read() || jr.TokenType != JsonToken.PropertyName || (string)jr.Value != "result")
                throw new Exception();

            return ReadResultArray(jr);
            //No need read others
        }

        private static List<List<double[]>> ReadResultArray(JsonTextReader jr)
        {
            if (!jr.Read() || jr.TokenType != JsonToken.StartArray) throw new Exception();

            var ls = new List<List<double[]>>();
            do
            {
                if (!jr.Read()) throw new Exception();
                if (jr.TokenType == JsonToken.EndArray) break;
                if (jr.TokenType != JsonToken.StartObject) throw new Exception();
                ls.Add(ReadResultItem(jr));
            } while (true);
            return ls;
        }

        private static List<double[]> ReadResultItem(JsonTextReader jr)
        {
            //已读取StartObject标记
            if (!jr.Read() || jr.TokenType != JsonToken.PropertyName || (string)jr.Value != "metric")
                throw new Exception();
            ReadMetric(jr);

            if (!jr.Read() || jr.TokenType != JsonToken.PropertyName || (string)jr.Value != "values")
                throw new Exception();
            var values = ReadValues(jr);
            if (!jr.Read() || jr.TokenType != JsonToken.EndObject) throw new Exception();
            return values;
        }

        private static void ReadMetric(JsonTextReader jr)
        {
            if (!jr.Read() || jr.TokenType != JsonToken.StartObject) throw new Exception();
            do
            {
                //PropertyName or EndObject
                if (!jr.Read()) throw new Exception();
                if (jr.TokenType == JsonToken.EndObject) return;
                //PropertyValue
                jr.Read();
            } while (true);
        }

        private static List<double[]> ReadValues(JsonTextReader jr)
        {
            if (!jr.Read() || jr.TokenType != JsonToken.StartArray) throw new Exception();

            var ls = new List<double[]>();
            do
            {
                if (!jr.Read()) throw new Exception();
                if (jr.TokenType == JsonToken.EndArray) break;
                if (jr.TokenType != JsonToken.StartArray) throw new Exception();
                var ts = jr.ReadAsDouble().Value * 1000; //PromQL时间*1000
                var value = double.Parse(jr.ReadAsString()); //PromQL值为字符串
                ls.Add(new double[] { ts, value });
                if (!jr.Read() || jr.TokenType != JsonToken.EndArray) throw new Exception();
            } while (true);
            return ls;
        }
    }
}
