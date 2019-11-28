using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace sys.ServiceLogic
{
	public class MetricService
	{
		private static readonly HttpClient http = new HttpClient()
		{
			//请修改指向Prometheus地址
			BaseAddress = new Uri("http://10.211.55.2:9090/api/v1/"),
			Timeout = TimeSpan.FromSeconds(2)
		};

		#region ====NodeExporter====
		public async Task<object> GetCpuUsages(string node, DateTime start, DateTime end)
		{
			var promql = $"100-irate(node_cpu{{instance='{node}:9100',mode='idle'}}[5m])*100";
			return await QueryRange(promql, start, end, 20, 2);
		}

		public async Task<object> GetMemUsages(string node, DateTime start, DateTime end)
		{
			var promql = $"(1-(node_memory_MemAvailable{{instance='{node}:9100'}}/(node_memory_MemTotal{{instance='{node}:9100'}})))*100";
			return await QueryRange(promql, start, end, 20, 2);
		}

		public async Task<object> GetNetTraffic(string node, DateTime start, DateTime end)
		{
			var downql = $"irate(node_network_receive_bytes{{instance='{node}:9100',device!~'tap.*|veth.*|br.*|docker.*|virbr*|lo*'}}[5m])";
			var ls = await QueryRange(downql, start, end, 15/*4*/, 0);
			var upql = $"irate(node_network_transmit_bytes{{instance='{node}:9100',device!~'tap.*|veth.*|br.*|docker.*|virbr*|lo*'}}[5m])";
			ls.AddRange(await QueryRange(upql, start, end, 15/*4*/, 0));
			return ls;
		}

		public async Task<object> GetDiskIO(string node, DateTime start, DateTime end)
		{
			var readql = $"irate(node_disk_bytes_read{{instance='{node}:9100'}}[1m])";
			var ls = await QueryRange(readql, start, end, 15/*10*/, 0);
			var writeql = $"irate(node_disk_bytes_written{{instance='{node}:9100'}}[1m])";
			ls.AddRange(await QueryRange(writeql, start, end, 15/*10*/, 0));
			return ls;
		}
		#endregion

		#region ====InvokeMetrics====
		/// <summary>
		/// 获取服务方法调用排名(次数或耗时)
		/// </summary>
		public async Task<object> GetTopInvoke(bool count, DateTime startTime, DateTime endTime, int top)
		{
			var seconds = (int)(endTime - startTime).TotalSeconds;
			var ts = (int)(endTime.ToUniversalTime() - DateTime.UnixEpoch).TotalSeconds;
			var type = count ? "count" : "sum";
			var round = count ? "1" : "0.001";
			var promql = $"topk({top},sort_desc(sum by (method) (round(increase(invoke_duration_seconds_{type}[{seconds}s]),{round}))))";
			var res = await http.GetAsync($"query?query={promql}&time={ts}");
			//TODO:暂不在后端处理，由前端处理
			return await res.Content.ReadAsStringAsync();
		}
		#endregion

		#region ====Parse PromQL====
		private static async Task<List<object>> QueryRange(string promql, DateTime start, DateTime end, int step, int round)
		{
			if (start >= end) throw new ArgumentOutOfRangeException();
			var ts1 = (int)(start.ToUniversalTime() - DateTime.UnixEpoch).TotalSeconds;
			var ts2 = (int)(end.ToUniversalTime() - DateTime.UnixEpoch).TotalSeconds;
			var res = await http.GetAsync($"query_range?query={promql}&start={ts1}&end={ts2}&step={step}s");
			var stream = await res.Content.ReadAsStreamAsync();
			using (var sr = new System.IO.StreamReader(stream))
			using (var jr = new JsonTextReader(sr))
			{
				return ParseToSeries(jr, round);
			}
		}

		private static List<object> ParseToSeries(JsonTextReader jr, int round)
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

			return ReadResultArray(jr, round);
			//No need read others
		}

		private static List<object> ReadResultArray(JsonTextReader jr, int round)
		{
			if (!jr.Read() || jr.TokenType != JsonToken.StartArray) throw new Exception();

			var ls = new List<object>();
			do
			{
				if (!jr.Read()) throw new Exception();
				if (jr.TokenType == JsonToken.EndArray) break;
				if (jr.TokenType != JsonToken.StartObject) throw new Exception();
				ls.Add(ReadResultItem(jr, round));
			} while (true);
			return ls;
		}

		private static List<double[]> ReadResultItem(JsonTextReader jr, int round)
		{
			//已读取StartObject标记
			if (!jr.Read() || jr.TokenType != JsonToken.PropertyName || (string)jr.Value != "metric")
				throw new Exception();
			ReadMetric(jr);

			if (!jr.Read() || jr.TokenType != JsonToken.PropertyName || (string)jr.Value != "values")
				throw new Exception();
			var values = ReadValues(jr, round);
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

		private static List<double[]> ReadValues(JsonTextReader jr, int round)
		{
			if (!jr.Read() || jr.TokenType != JsonToken.StartArray) throw new Exception();

			var ls = new List<double[]>();
			do
			{
				if (!jr.Read()) throw new Exception();
				if (jr.TokenType == JsonToken.EndArray) break;
				if (jr.TokenType != JsonToken.StartArray) throw new Exception();
				var ts = jr.ReadAsDouble().Value * 1000; //PromQL时间*1000
				var value = Math.Round(double.Parse(jr.ReadAsString()), round, MidpointRounding.ToEven); //PromQL值为字符串
				ls.Add(new double[] { ts, value });
				if (!jr.Read() || jr.TokenType != JsonToken.EndArray) throw new Exception();
			} while (true);
			return ls;
		}
		#endregion

	}

}