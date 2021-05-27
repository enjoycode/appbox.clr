using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using appbox.Store;
using appbox.Runtime;
using System.Linq;

namespace appbox.Host.obd
{
	/// <summary>
	/// 终端数据收到后的处理服务
	/// </summary>
    public static class ObdDataReceiveService
    {
		const ulong VehicleEntityId = 3951134303332597777;
		// 设备数据库实体的Id
		const ulong DeviceEntityId = 3951134303332597797;
		// OBD数据实体的Id
		const ulong ObdDataEntityId = 3951134303332597901;
		// 实时数据流的实体Id
		const ulong EngDataEntityId = 3951134303332597909;
		const ulong ElectronicFenceVehicleEntityId = 3951134303332598109;
		// public static OBDGateway.VINInfo GetVINInfo(string vin)
		// {
		// 	try
		// 	{
		// 		var q = new SqlQuery(VehicleEntityId);
		// 		q.Where(q.T["VIN"] == vin);
		// 		var entity = q.ToSingleAsync().Result;
		// 		if(entity == null)
		// 			return null;
		// 		var vid = entity.GetMember("Id");
		// 		var supervisorId = entity.GetMember("SuperviseId");
		// 		var deviceId = entity.GetMember("DeviceId");
		// 		var vehicleOwnerId = entity.GetMember("VehicleOwnerId");
		// 		var orgUnitId = entity.GetMember("OrgUnitId");
		// 		if(!supervisorId.HasValue || !deviceId.HasValue)
		// 			return null;

		// 		// 获取设置的电子围栏信息
		// 		var qe = new SqlQuery(ElectronicFenceVehicleEntityId);
		// 		qe.Where(q.T["VehicleId"] == vid.GuidValue);
		// 		var efs = qe.ToListAsync().Result;
		// 		return new OBDGateway.VINInfo
		// 		{
		// 			//VIN = entity.GetMember("VIN").ToString(),
		// 			SupervisorId = supervisorId.GuidValue,
		// 			DeviceId = deviceId.GuidValue,
		// 			OrgUnitId = orgUnitId.GuidValue,
		// 			VehicleOwnerId = vehicleOwnerId.GuidValue,
		// 			ElectronicFenceIds = efs.Select(t=>t.GetMember("ElectronicFenceId").GuidValue).ToList()
		// 		};
		// 	}
		// 	catch (System.Exception ex)
		// 	{
		// 		Log.Error($"车辆上线，获取车辆信息失败{ex}");
		// 		return null;
		// 	}
		// }

		public static void VehicleUpdateState(OBDGateway.VehicleLastData data)
		{
			// 车辆状态发生变化
			// 处理事件信息
		}

		public static void VehicleOnline(string vin)
		{
			appbox.Server.Channel.WebSocketManager.PublishEvent(new
			{
				vin,
				eventName = "VehicleOnline"
			});
		}

		public static void VehicleOffline(string vin)
		{
			appbox.Server.Channel.WebSocketManager.PublishEvent(new
			{
				vin,
				eventName = "VehicleOffline"
			});
		}

		public static void VehicleAlarm(OBDGateway.AlarmNotify alarmNotify)
		{
			appbox.Server.Channel.WebSocketManager.PublishEvent(new
			{
				eventName = "VehicleAlarm",
				data = alarmNotify
			});
		}
		
		public static void PublishEventTest()
		{
			Task.Run(async () =>
			{
				await Task.Delay(30000);
				while (true)
				{
					await Task.Delay(30000);
					VehicleOnline("wuxiaofei");
				}
			});
		}
    }
	// 用于主动调用网关，获取相关数据
	public class ObdService : IService
	{
		/// <summary>
		/// 获取指定监管方下的所有在线设备的车架号
		/// </summary>
		private async Task<List<string>> GetOnlineBySupervisorId(Guid supervisorId, List<Guid> ouids, string vehicleOwnerId, string maintenanceId)
		{
			var ons = OBDGateway.Server.GetOnlines(supervisorId, ouids, vehicleOwnerId, maintenanceId);
			Log.Info($"当前监管方{supervisorId}共有在线车辆{ons.Count}辆");
			return await Task.FromResult(ons);
		}
		/// <summary>
		/// 获取指定监管方下的所有在线设备的数量
		/// </summary>
		private async Task<int> GetOnlineCount(Guid supervisorId, List<Guid> ouids, string vehicleOwnerId, string maintenanceId)
		{
			var vins = await Task.FromResult(OBDGateway.Server.GetOnlines(supervisorId, ouids, vehicleOwnerId, maintenanceId));
			return vins.Count;
		}
		/// <summary>
		/// 获取指定监管方下在线设备故障车辆数
		/// </summary>
		private async Task<int> GetOnlineFaultCount(Guid supervisorId)
		{
			var datas = OBDGateway.Server.GetLastDatas(supervisorId);
			return await Task.FromResult(datas.Count(t => t.OBD != null && t.OBD.Value.DataLen > 0));
		}
		/// <summary>
		/// 获取指定监管方下在线异常车辆数(国5&国6)
		/// {five: 20, six: 10}
		/// </summary>
		private async Task<object> GetOnlineAnomalousCount(Guid supervisorId, List<Guid> ouids, string vehicleOwnerId, string maintenanceId)
		{
			var count = OBDGateway.Server.GetOnlineAnomalousCount(supervisorId, ouids, vehicleOwnerId, maintenanceId);
			return await Task.FromResult(count);
		}
		/// <summary>
		/// 批量获取指定监管方下的最新数据信息
		/// </summary>
		private async Task<object[]> GetLastDataBySuperviseId(Guid supervisorId)
		{
			var vds = await Task.FromResult(OBDGateway.Server.GetLastDatas(supervisorId));
			var list = new object[vds.Length];
			for (int i = 0; i < vds.Length; i++)
			{
				var item = vds[i];
				list[i] = new
				{
					VIN = item.VIN,
					OBD = ConvertObdData(item.OBD),
					ENG = ConvertEngData(item.ENG)
				};
			}
			Log.Info($"车辆最新信息{vds.Length}");
			return list;
		}

		private async Task<List<object>> GetLastDatas(List<string> vins)
		{
			if(vins == null || vins.Count == 0)
				throw new Exception("请传入车架号");
			List<object> list = new List<object>();
			foreach (var vin in vins)
			{
				var v = OBDGateway.Server.GetLastData(vin);
				if(string.IsNullOrWhiteSpace(v.VIN))
					continue;
				list.Add(new
				{
					VIN = v.VIN,
					OBD = ConvertObdData(v.OBD),
					ENG = ConvertEngData(v.ENG)
				});
			}
			return await Task.FromResult(list);
		}

		/// <summary>
		/// 获取单一车架号对应的最新数据信息
		/// </summary>
		private async Task<object> GetLastData(string vin)
		{
			if(string.IsNullOrWhiteSpace(vin))
				throw new Exception("请传入车架号");
			var ds = OBDGateway.Server.GetLastData(vin);
			// if(ds == null || ds.Length == 0)
			// {
			// 	// 如果网关端没有数据，则表示可能1.传入的车架号的车辆在网关启动后没有链接2.传入的车架号不正确
			// 	// 对于第一种情况是否需要从数据库中读取最后一条
			// 	return new object();
			// }
			if(string.IsNullOrWhiteSpace(ds.VIN))
				return null;
			return await Task.FromResult(new{
				VIN = ds.VIN,
				OBD = ConvertObdData(ds.OBD),
				ENG = ConvertEngData(ds.ENG)
			});
		}

		private object ConvertObdData(OBDGateway.OBDData? data)
		{
			if(data == null)
				return null;
			var d = data.Value;
			return new
			{
				Time = d.Time.ToDateTime(),
				d.IUPR,
				d.MILStat,
				d.Protocol,
				d.Ready,
				d.SCN,
				d.Supported,
				d.VIN
			};
		}
		private object ConvertEngData(OBDGateway.ENGData? data)
		{
			if(data == null)
				return null;
			var d = data.Value;
			return new
			{
				d.AirInput,
				d.AirPressure,
				d.CoolingTemp,
				d.DataLen,
				d.DPF,
				d.EngNM,
				d.FriNM,
				d.GpsStat,
				d.Lat,
				d.Lng,
				d.Miles,
				d.OilFlux,
				d.OilPos,
				d.Reactant,
				d.RSpeed,
				d.ScrInNOx,
				d.ScrInTemp,
				d.ScrOutNOx,
				d.ScrOutTemp,
				Time = d.Time.ToDateTime(),
				d.VSpeed
			};
		}
		// 客户端更改系统配置时通知网关设置已更改
		private void SystemSettingChanged(string settingJson)
		{
			var setting = System.Text.Json.JsonSerializer.Deserialize<OBDGateway.SystemSetting>(settingJson);
			OBDGateway.Server.Settings.SystemSettingChanged?.Invoke(setting);
			// Console.WriteLine("SystemSettingChanged");
		}
		// 客户端更改系统配置时通知网关设置已更改
		private void AlarmSettingChanged(string settingJson)
		{
			var setting = System.Text.Json.JsonSerializer.Deserialize<OBDGateway.Models.AlarmSetting>(settingJson);
			OBDGateway.Server.Settings.AlarmSettingChanged?.Invoke(setting);
			// Console.WriteLine("SystemSettingChanged");
		}
		// 客户端更改电子围栏时通知网关
		private void ElectronicFenceChanged(string electronicFenceJson)
		{
			var electronicFence = System.Text.Json.JsonSerializer.Deserialize<OBDGateway.ElectronicFence>(electronicFenceJson);
			OBDGateway.Server.Settings.ElectronicFenceChanged?.Invoke(electronicFence);
		}
		// 客户端更改电子围栏与车辆的绑定关系时通知网关
		private void VehicleElectronicFenceChanged(Guid electronicFenceId, string vinsJson)
		{
			List<string> vins = new List<string>();
			if(!string.IsNullOrWhiteSpace(vinsJson))
				vins = vinsJson.Split(',').ToList();
			OBDGateway.Server.Settings.VehicleElectronicFenceChanged?.Invoke(electronicFenceId, vins);
		}

		public async ValueTask<Data.AnyValue> InvokeAsync(ReadOnlyMemory<char> method, Data.InvokeArgs args)
		{
			switch (method)
			{
				case ReadOnlyMemory<char> s when s.Span.SequenceEqual(nameof(GetOnlineCount)):
					var sid = args.GetGuid();
					var ouobjs = args.GetObjectArray();
					var ous = ouobjs.Select(t => Guid.Parse(t.ToString())).ToList();
					var voidstr = args.GetString();
					var mid = args.GetString();
					return Data.AnyValue.From(await GetOnlineCount(sid, ous, voidstr, mid));
				case ReadOnlyMemory<char> s when s.Span.SequenceEqual(nameof(GetOnlineBySupervisorId)):
					sid = args.GetGuid();
					ouobjs = args.GetObjectArray();
					ous = ouobjs.Select(t => Guid.Parse(t.ToString())).ToList();
					voidstr = args.GetString();
					mid = args.GetString();
					return Data.AnyValue.From(await GetOnlineBySupervisorId(sid, ous, voidstr, mid));
				case ReadOnlyMemory<char> s when s.Span.SequenceEqual(nameof(GetOnlineFaultCount)):
					return Data.AnyValue.From(await GetOnlineFaultCount(args.GetGuid()));
				case ReadOnlyMemory<char> s when s.Span.SequenceEqual(nameof(GetLastDataBySuperviseId)):
					return Data.AnyValue.From(await GetLastDataBySuperviseId(args.GetGuid()));
				case ReadOnlyMemory<char> s when s.Span.SequenceEqual(nameof(GetLastDatas)):
					var arr = args.GetObjectArray();
					var vins = new List<string>();
					if(arr == null || arr.Count == 0)
						throw new Exception("请传入车架号");
					vins = arr.Select(t => t.ToString()).ToList();
					return Data.AnyValue.From(await GetLastDatas(vins));
				case ReadOnlyMemory<char> s when s.Span.SequenceEqual(nameof(GetLastData)):
					return Data.AnyValue.From(await GetLastData(args.GetString()));
				case ReadOnlyMemory<char> s when s.Span.SequenceEqual(nameof(SystemSettingChanged)):
					SystemSettingChanged(args.GetString());
					return Data.AnyValue.Empty;
				case ReadOnlyMemory<char> s when s.Span.SequenceEqual(nameof(AlarmSettingChanged)):
					AlarmSettingChanged(args.GetString());
					return Data.AnyValue.Empty;
				case ReadOnlyMemory<char> s when s.Span.SequenceEqual(nameof(ElectronicFenceChanged)):
					ElectronicFenceChanged(args.GetString());
					return Data.AnyValue.Empty;
				case ReadOnlyMemory<char> s when s.Span.SequenceEqual(nameof(VehicleElectronicFenceChanged)):
					// arr = args.GetObjectArray();
					// vins = new List<string>();
					// vins = arr.Select(t => t.ToString()).ToList();
					VehicleElectronicFenceChanged(args.GetGuid(), args.GetString());
					return Data.AnyValue.Empty;
				case ReadOnlyMemory<char> s when s.Span.SequenceEqual(nameof(GetOnlineAnomalousCount)):
					sid = args.GetGuid();
					ouobjs = args.GetObjectArray();
					ous = ouobjs.Select(t => Guid.Parse(t.ToString())).ToList();
					voidstr = args.GetString();
					mid = args.GetString();
					return Data.AnyValue.From(await GetOnlineAnomalousCount(sid, ous, voidstr, mid));
				default:
					throw new Exception($"Can't find method: {method}");
			}
		}
	}
}