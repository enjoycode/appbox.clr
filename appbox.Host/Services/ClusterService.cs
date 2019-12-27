#if FUTURE

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using appbox.Data;
using appbox.Runtime;
using appbox.Server;
using appbox.Store;
using appbox.Models;

namespace appbox.Services
{
    /// <summary>
    /// 集群管理服务
    /// </summary>
    sealed class ClusterService : IService
    {

        public async Task<object> GetGauges()
        {
            EnsureIsAdmin();
            //读取Nodes数量
            int nodes = 0;
            await ScanNodes((kp, ks, vp, vs) => nodes++);
            //读取分区数量
            int parts = 0;
            await ScanParts((kp, ks, vp, vs) => parts++);
            //TODO:others
            return new Gauges { Nodes = nodes, Parts = parts, Sessions = 1 };
        }

        public async Task<object> GetNodes()
        {
            EnsureIsAdmin();
            var ls = new Dictionary<uint, NodeInfo>(); //key=ip

            //1.扫描节点
            var peerStateParser = new Google.Protobuf.MessageParser<PeerState>(() => new PeerState());
            await ScanNodes((kp, ks, vp, vs) =>
            {
                unsafe
                {
                    uint peerId = 0;
                    byte* peerIdPtr = (byte*)&peerId;
                    byte* keyPtr = (byte*)kp.ToPointer();
                    peerIdPtr[0] = keyPtr[2];
                    peerIdPtr[1] = keyPtr[1];

                    var stream = new System.IO.UnmanagedMemoryStream((byte*)vp.ToPointer(), vs);
                    var peerState = peerStateParser.ParseFrom(stream);
                    stream.Dispose();

                    var ip = peerState.Endpoint.Address;
                    byte* ipPtr = (byte*)&ip;
                    var ipAddress = $"{ipPtr[3]}.{ipPtr[2]}.{ipPtr[1]}.{ipPtr[0]}";
                    var nodeInfo = new NodeInfo
                    {
                        PeerId = peerId.ToString("X2"),
                        IPAddress = ipAddress,
                        Port = (ushort)peerState.Endpoint.Port
                    };
                    ls.Add(ip, nodeInfo);
                }
            });

            //2.获取MetaNodes
            var peerConfigParser = new Google.Protobuf.MessageParser<PeerConfig>(() => new PeerConfig());
            IntPtr configData = NativeApi.GetPeerConfigData();
            var configDataPtr = NativeApi.GetStringData(configData);
            var configDataSize = NativeApi.GetStringSize(configData);
            PeerConfig peerConfig;
            unsafe
            {
                var stream = new System.IO.UnmanagedMemoryStream((byte*)configDataPtr.ToPointer(), (long)configDataSize);
                peerConfig = peerConfigParser.ParseFrom(stream);
                stream.Dispose();
            }
            NativeApi.FreeNativeString(configData);
            foreach (var meta in peerConfig.MetaNodes)
            {
                ls[meta.Endpoint.Address].IsMeta = true;
                ls[meta.Endpoint.Address].RaftNodes += 1;
            }

            //3.扫描RaftGroups
            var groupParser = new Google.Protobuf.MessageParser<RaftGroupInfo>(() => new RaftGroupInfo());
            await ScanRaftGroups((kp, ks, vp, vs) =>
            {
                unsafe
                {
                    var stream = new System.IO.UnmanagedMemoryStream((byte*)vp.ToPointer(), vs);
                    var groupInfo = groupParser.ParseFrom(stream);
                    stream.Dispose();

                    foreach (var peer in groupInfo.Nodes)
                    {
                        ls[peer.Endpoint.Address].RaftNodes += 1;
                    }
                }
            });
            return ls.Values.ToArray();
        }

        /// <summary>
        /// 加载所有的RaftGroup信息
        /// </summary>
        public async Task<object> GetParts()
        {
            EnsureIsAdmin();

            //先从MetaCF读取所有EntityModel
            var apps = await ModelStore.LoadAllApplicationAsync();
            //TODO:待优化只加载EntityModel
            var models = await ModelStore.LoadAllModelAsync();
            var ms = models.Where(t => t.ModelType == ModelType.Entity).Select(t => (EntityModel)t).ToArray();

            //再从PartCF读取所有RaftGroupId
            var ls = new List<PartitionInfo>();
            await ScanParts((kp, ks, vp, vs) =>
            {
                unsafe
                {
                    ulong* groupIdPtr = (ulong*)vp.ToPointer();
                    var appId = ((byte*)kp.ToPointer())[0];
                    var app = apps.Single(t => t.StoreId == appId);

                    int* tableIdPtr = (int*)kp.ToPointer();
                    var tableId = (uint)(System.Net.IPAddress.NetworkToHostOrder(*tableIdPtr) & 0xFFFFFF);
                    var groupInfo = new PartitionInfo
                    {
                        Id = (*groupIdPtr).ToString("X2"),
                        ModelName = app.Name + "." + ms.SingleOrDefault(t => t.AppId == app.Id && t.TableId == tableId)?.Name
                        //TODO:其他信息
                    };
                    ls.Add(groupInfo);
                }
            });
            return ls;
        }

        /// <summary>
        /// 提议将指定Peer设为MetaNode
        /// </summary>
        /// <param name="peerId">十六进制编码字串</param>
        /// <param name="ipAddress">eg: "10.211.55.4"</param>
        /// <returns>true = propose ok</returns>
        public async Task<object> SetAsMeta(string peerId, string ipAddress, int port)
        {
            EnsureIsAdmin();

            ushort pid = ushort.Parse(peerId, System.Globalization.NumberStyles.HexNumber);
            //根据编码规则构建TargetRaftNodeId
            ulong nodeId = ((ulong)pid) << 4;
            var ip = System.Net.IPAddress.Parse(ipAddress);
            var ipBytes = ip.GetAddressBytes(); //TODO: ipv6
            uint ipValue = 0;
            unsafe
            {
                byte* ipValuePtr = (byte*)&ipValue;
                ipValuePtr[0] = ipBytes[3];
                ipValuePtr[1] = ipBytes[2];
                ipValuePtr[2] = ipBytes[1];
                ipValuePtr[3] = ipBytes[0];
            }

            var res = await ((HostStoreApi)StoreApi.Api).ProposeConfChangeAsync(
                (byte)ConfChangeType.ConfChangeAddNode, nodeId, ipValue, (ushort)port);
            return res;
        }

        /// <summary>
        /// 提升集群的副本因子
        /// </summary>
        public async Task<object> PromoteReplFactor()
        {
            EnsureIsAdmin();

            await ((HostStoreApi)StoreApi.Api).PromoteReplFactorAsync(3); //TODO:暂factor=3
            return null;
        }

        public async ValueTask<AnyValue> InvokeAsync(ReadOnlyMemory<char> method, InvokeArgs args)
        {
            switch (method)
            {
                case ReadOnlyMemory<char> s when s.Span.SequenceEqual(nameof(GetGauges)):
                    return AnyValue.From(await GetGauges());
                case ReadOnlyMemory<char> s when s.Span.SequenceEqual(nameof(GetNodes)):
                    return AnyValue.From(await GetNodes());
                case ReadOnlyMemory<char> s when s.Span.SequenceEqual(nameof(GetParts)):
                    return AnyValue.From(await GetParts());
                case ReadOnlyMemory<char> s when s.Span.SequenceEqual(nameof(SetAsMeta)):
                    return AnyValue.From(await SetAsMeta(args.GetString(), args.GetString(), args.GetInt32()));
                case ReadOnlyMemory<char> s when s.Span.SequenceEqual(nameof(PromoteReplFactor)):
                    return AnyValue.From(await PromoteReplFactor());
                default:
                    throw new Exception($"Can't find method: {method}");
            }
        }

        #region ====Static Methods====
        private static void EnsureIsAdmin() //TODO:或者运维权限
        {
            if (!RuntimeContext.HasPermission(Consts.SYS_PERMISSION_ADMIN_ID))
                throw new Exception("不具备权限");
        }

        private static async Task ScanRaftGroups(Action<IntPtr, int, IntPtr, int> action)
        {
            IntPtr bkPtr;
            IntPtr ekPtr;
            byte bkey = 2;
            byte ekey = 3;
            unsafe
            {
                bkPtr = new IntPtr(&bkey);
                ekPtr = new IntPtr(&ekey);
            }

            var req = new ClrScanRequire
            {
                RaftGroupId = KeyUtil.META_RAFTGROUP_ID,
                BeginKeyPtr = bkPtr,
                BeginKeySize = new IntPtr(1),
                EndKeyPtr = ekPtr,
                EndKeySize = new IntPtr(1),
                FilterPtr = IntPtr.Zero,
                Skip = 0,
                Take = uint.MaxValue,
                DataCF = KeyUtil.METACF_INDEX
            };
            IntPtr reqPtr;
            unsafe { reqPtr = new IntPtr(&req); }
            var scanRes = await StoreApi.Api.ReadIndexByScanAsync(reqPtr);
            if (scanRes == null) return;
            scanRes.ForEachRow(action);
            scanRes.Dispose();
        }

        private static async Task ScanNodes(Action<IntPtr, int, IntPtr, int> action)
        {
            IntPtr bkPtr;
            IntPtr ekPtr;
            byte bkey = 1;
            byte ekey = 2;
            unsafe
            {
                bkPtr = new IntPtr(&bkey);
                ekPtr = new IntPtr(&ekey);
            }

            var req = new ClrScanRequire
            {
                RaftGroupId = KeyUtil.META_RAFTGROUP_ID,
                BeginKeyPtr = bkPtr,
                BeginKeySize = new IntPtr(1),
                EndKeyPtr = ekPtr,
                EndKeySize = new IntPtr(1),
                FilterPtr = IntPtr.Zero,
                Skip = 0,
                Take = uint.MaxValue,
                DataCF = KeyUtil.METACF_INDEX
            };
            IntPtr reqPtr;
            unsafe { reqPtr = new IntPtr(&req); }
            var scanRes = await StoreApi.Api.ReadIndexByScanAsync(reqPtr);
            if (scanRes == null) return;
            scanRes.ForEachRow(action);
            scanRes.Dispose();
        }

        private static async Task ScanParts(Action<IntPtr, int, IntPtr, int> action)
        {
            IntPtr beginKeyPtr;
            IntPtr endKeyPtr;
            int keySize = 4;
            uint beginKey = 0;
            uint endKey = 0xFFFFFFFF;
            unsafe
            {
                beginKeyPtr = new IntPtr(&beginKey);
                endKeyPtr = new IntPtr(&endKey);
            }

            var req = new ClrScanRequire
            {
                RaftGroupId = KeyUtil.META_RAFTGROUP_ID,
                BeginKeyPtr = beginKeyPtr,
                BeginKeySize = new IntPtr(keySize),
                EndKeyPtr = endKeyPtr,
                EndKeySize = new IntPtr(keySize),
                FilterPtr = IntPtr.Zero,
                Skip = 0,
                Take = uint.MaxValue,
                DataCF = KeyUtil.PARTCF_INDEX
            };
            IntPtr reqPtr;
            unsafe { reqPtr = new IntPtr(&req); }
            var scanRes = await StoreApi.Api.ReadIndexByScanAsync(reqPtr);
            if (scanRes == null) return;
            scanRes.ForEachRow(action);
            scanRes.Dispose();
        }
        #endregion
    }

    #region ====Structs====
    public struct Gauges
    {
        public int Nodes { get; internal set; }
        public int Parts { get; internal set; }
        public int Sessions { get; internal set; }
    }

    public sealed class NodeInfo
    {
        public string PeerId { get; internal set; }
        public string IPAddress { get; internal set; }
        public ushort Port { get; internal set; }
        public bool IsMeta { get; internal set; }
        public int RaftNodes { get; internal set; }
    }

    public struct PartitionInfo
    {
        public string Id { get; internal set; }
        public string ModelName { get; internal set; }
        public string Partition { get; internal set; }
        public int RaftNodes { get; internal set; }
    }
    #endregion
}

#endif