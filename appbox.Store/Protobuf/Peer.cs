// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: peer.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
/// <summary>Holder for reflection information generated from peer.proto</summary>
public static partial class PeerReflection {

  #region Descriptor
  /// <summary>File descriptor for peer.proto</summary>
  public static pbr::FileDescriptor Descriptor {
    get { return descriptor; }
  }
  private static pbr::FileDescriptor descriptor;

  static PeerReflection() {
    byte[] descriptorData = global::System.Convert.FromBase64String(
        string.Concat(
          "CgpwZWVyLnByb3RvIi0KDFBlZXJFbmRwb2ludBIPCgdBZGRyZXNzGAEgASgH",
          "EgwKBFBvcnQYAiABKA0iQwoMUmFmdE5vZGVJbmZvEhIKClJhZnROb2RlSWQY",
          "ASABKAYSHwoIRW5kcG9pbnQYAiABKAsyDS5QZWVyRW5kcG9pbnQiSAoNUmFm",
          "dEdyb3VwSW5mbxIZChFCb290U2NoZW1hVmVyc2lvbhgBIAEoDRIcCgVOb2Rl",
          "cxgCIAMoCzINLlJhZnROb2RlSW5mbyJtChFKb2luQ2x1c3RlclJlc3VsdBIQ",
          "CghFcnJvck1zZxgBIAEoCRIkCg1Mb2NhbEVuZHBvaW50GAIgASgLMg0uUGVl",
          "ckVuZHBvaW50EiAKCU1ldGFOb2RlcxgDIAMoCzINLlJhZnROb2RlSW5mbyJz",
          "CgpQZWVyQ29uZmlnEg4KBlBlZXJJZBgBIAEoBxIPCgdXZWJQb3J0GAIgASgN",
          "EiIKC1JwY0VuZHBvaW50GAMgASgLMg0uUGVlckVuZHBvaW50EiAKCU1ldGFO",
          "b2RlcxgEIAMoCzINLlJhZnROb2RlSW5mbyJDCglQZWVyU3RhdGUSHwoIRW5k",
          "cG9pbnQYASABKAsyDS5QZWVyRW5kcG9pbnQSFQoNUmFmdE5vZGVDb3VudBgC",
          "IAEoDSouCgxSYWZ0Tm9kZVR5cGUSDwoLRW50aXR5U3RvcmUQABINCglGaWxl",
          "U3RvcmUQAWIGcHJvdG8z"));
    descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
        new pbr::FileDescriptor[] { },
        new pbr::GeneratedClrTypeInfo(new[] {typeof(global::RaftNodeType), }, new pbr::GeneratedClrTypeInfo[] {
          new pbr::GeneratedClrTypeInfo(typeof(global::PeerEndpoint), global::PeerEndpoint.Parser, new[]{ "Address", "Port" }, null, null, null),
          new pbr::GeneratedClrTypeInfo(typeof(global::RaftNodeInfo), global::RaftNodeInfo.Parser, new[]{ "RaftNodeId", "Endpoint" }, null, null, null),
          new pbr::GeneratedClrTypeInfo(typeof(global::RaftGroupInfo), global::RaftGroupInfo.Parser, new[]{ "BootSchemaVersion", "Nodes" }, null, null, null),
          new pbr::GeneratedClrTypeInfo(typeof(global::JoinClusterResult), global::JoinClusterResult.Parser, new[]{ "ErrorMsg", "LocalEndpoint", "MetaNodes" }, null, null, null),
          new pbr::GeneratedClrTypeInfo(typeof(global::PeerConfig), global::PeerConfig.Parser, new[]{ "PeerId", "WebPort", "RpcEndpoint", "MetaNodes" }, null, null, null),
          new pbr::GeneratedClrTypeInfo(typeof(global::PeerState), global::PeerState.Parser, new[]{ "Endpoint", "RaftNodeCount" }, null, null, null)
        }));
  }
  #endregion

}
#region Enums
public enum RaftNodeType {
  [pbr::OriginalName("EntityStore")] EntityStore = 0,
  [pbr::OriginalName("FileStore")] FileStore = 1,
}

#endregion

#region Messages
public sealed partial class PeerEndpoint : pb::IMessage<PeerEndpoint> {
  private static readonly pb::MessageParser<PeerEndpoint> _parser = new pb::MessageParser<PeerEndpoint>(() => new PeerEndpoint());
  private pb::UnknownFieldSet _unknownFields;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public static pb::MessageParser<PeerEndpoint> Parser { get { return _parser; } }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public static pbr::MessageDescriptor Descriptor {
    get { return global::PeerReflection.Descriptor.MessageTypes[0]; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  pbr::MessageDescriptor pb::IMessage.Descriptor {
    get { return Descriptor; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public PeerEndpoint() {
    OnConstruction();
  }

  partial void OnConstruction();

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public PeerEndpoint(PeerEndpoint other) : this() {
    address_ = other.address_;
    port_ = other.port_;
    _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public PeerEndpoint Clone() {
    return new PeerEndpoint(this);
  }

  /// <summary>Field number for the "Address" field.</summary>
  public const int AddressFieldNumber = 1;
  private uint address_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public uint Address {
    get { return address_; }
    set {
      address_ = value;
    }
  }

  /// <summary>Field number for the "Port" field.</summary>
  public const int PortFieldNumber = 2;
  private uint port_;
  /// <summary>
  ///TODO:加入ipv6支持
  /// </summary>
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public uint Port {
    get { return port_; }
    set {
      port_ = value;
    }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override bool Equals(object other) {
    return Equals(other as PeerEndpoint);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public bool Equals(PeerEndpoint other) {
    if (ReferenceEquals(other, null)) {
      return false;
    }
    if (ReferenceEquals(other, this)) {
      return true;
    }
    if (Address != other.Address) return false;
    if (Port != other.Port) return false;
    return Equals(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override int GetHashCode() {
    int hash = 1;
    if (Address != 0) hash ^= Address.GetHashCode();
    if (Port != 0) hash ^= Port.GetHashCode();
    if (_unknownFields != null) {
      hash ^= _unknownFields.GetHashCode();
    }
    return hash;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override string ToString() {
    return pb::JsonFormatter.ToDiagnosticString(this);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void WriteTo(pb::CodedOutputStream output) {
    if (Address != 0) {
      output.WriteRawTag(13);
      output.WriteFixed32(Address);
    }
    if (Port != 0) {
      output.WriteRawTag(16);
      output.WriteUInt32(Port);
    }
    if (_unknownFields != null) {
      _unknownFields.WriteTo(output);
    }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public int CalculateSize() {
    int size = 0;
    if (Address != 0) {
      size += 1 + 4;
    }
    if (Port != 0) {
      size += 1 + pb::CodedOutputStream.ComputeUInt32Size(Port);
    }
    if (_unknownFields != null) {
      size += _unknownFields.CalculateSize();
    }
    return size;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void MergeFrom(PeerEndpoint other) {
    if (other == null) {
      return;
    }
    if (other.Address != 0) {
      Address = other.Address;
    }
    if (other.Port != 0) {
      Port = other.Port;
    }
    _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void MergeFrom(pb::CodedInputStream input) {
    uint tag;
    while ((tag = input.ReadTag()) != 0) {
      switch(tag) {
        default:
          _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
          break;
        case 13: {
          Address = input.ReadFixed32();
          break;
        }
        case 16: {
          Port = input.ReadUInt32();
          break;
        }
      }
    }
  }

}

public sealed partial class RaftNodeInfo : pb::IMessage<RaftNodeInfo> {
  private static readonly pb::MessageParser<RaftNodeInfo> _parser = new pb::MessageParser<RaftNodeInfo>(() => new RaftNodeInfo());
  private pb::UnknownFieldSet _unknownFields;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public static pb::MessageParser<RaftNodeInfo> Parser { get { return _parser; } }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public static pbr::MessageDescriptor Descriptor {
    get { return global::PeerReflection.Descriptor.MessageTypes[1]; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  pbr::MessageDescriptor pb::IMessage.Descriptor {
    get { return Descriptor; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public RaftNodeInfo() {
    OnConstruction();
  }

  partial void OnConstruction();

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public RaftNodeInfo(RaftNodeInfo other) : this() {
    raftNodeId_ = other.raftNodeId_;
    Endpoint = other.endpoint_ != null ? other.Endpoint.Clone() : null;
    _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public RaftNodeInfo Clone() {
    return new RaftNodeInfo(this);
  }

  /// <summary>Field number for the "RaftNodeId" field.</summary>
  public const int RaftNodeIdFieldNumber = 1;
  private ulong raftNodeId_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public ulong RaftNodeId {
    get { return raftNodeId_; }
    set {
      raftNodeId_ = value;
    }
  }

  /// <summary>Field number for the "Endpoint" field.</summary>
  public const int EndpointFieldNumber = 2;
  private global::PeerEndpoint endpoint_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public global::PeerEndpoint Endpoint {
    get { return endpoint_; }
    set {
      endpoint_ = value;
    }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override bool Equals(object other) {
    return Equals(other as RaftNodeInfo);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public bool Equals(RaftNodeInfo other) {
    if (ReferenceEquals(other, null)) {
      return false;
    }
    if (ReferenceEquals(other, this)) {
      return true;
    }
    if (RaftNodeId != other.RaftNodeId) return false;
    if (!object.Equals(Endpoint, other.Endpoint)) return false;
    return Equals(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override int GetHashCode() {
    int hash = 1;
    if (RaftNodeId != 0UL) hash ^= RaftNodeId.GetHashCode();
    if (endpoint_ != null) hash ^= Endpoint.GetHashCode();
    if (_unknownFields != null) {
      hash ^= _unknownFields.GetHashCode();
    }
    return hash;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override string ToString() {
    return pb::JsonFormatter.ToDiagnosticString(this);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void WriteTo(pb::CodedOutputStream output) {
    if (RaftNodeId != 0UL) {
      output.WriteRawTag(9);
      output.WriteFixed64(RaftNodeId);
    }
    if (endpoint_ != null) {
      output.WriteRawTag(18);
      output.WriteMessage(Endpoint);
    }
    if (_unknownFields != null) {
      _unknownFields.WriteTo(output);
    }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public int CalculateSize() {
    int size = 0;
    if (RaftNodeId != 0UL) {
      size += 1 + 8;
    }
    if (endpoint_ != null) {
      size += 1 + pb::CodedOutputStream.ComputeMessageSize(Endpoint);
    }
    if (_unknownFields != null) {
      size += _unknownFields.CalculateSize();
    }
    return size;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void MergeFrom(RaftNodeInfo other) {
    if (other == null) {
      return;
    }
    if (other.RaftNodeId != 0UL) {
      RaftNodeId = other.RaftNodeId;
    }
    if (other.endpoint_ != null) {
      if (endpoint_ == null) {
        endpoint_ = new global::PeerEndpoint();
      }
      Endpoint.MergeFrom(other.Endpoint);
    }
    _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void MergeFrom(pb::CodedInputStream input) {
    uint tag;
    while ((tag = input.ReadTag()) != 0) {
      switch(tag) {
        default:
          _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
          break;
        case 9: {
          RaftNodeId = input.ReadFixed64();
          break;
        }
        case 18: {
          if (endpoint_ == null) {
            endpoint_ = new global::PeerEndpoint();
          }
          input.ReadMessage(endpoint_);
          break;
        }
      }
    }
  }

}

public sealed partial class RaftGroupInfo : pb::IMessage<RaftGroupInfo> {
  private static readonly pb::MessageParser<RaftGroupInfo> _parser = new pb::MessageParser<RaftGroupInfo>(() => new RaftGroupInfo());
  private pb::UnknownFieldSet _unknownFields;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public static pb::MessageParser<RaftGroupInfo> Parser { get { return _parser; } }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public static pbr::MessageDescriptor Descriptor {
    get { return global::PeerReflection.Descriptor.MessageTypes[2]; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  pbr::MessageDescriptor pb::IMessage.Descriptor {
    get { return Descriptor; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public RaftGroupInfo() {
    OnConstruction();
  }

  partial void OnConstruction();

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public RaftGroupInfo(RaftGroupInfo other) : this() {
    bootSchemaVersion_ = other.bootSchemaVersion_;
    nodes_ = other.nodes_.Clone();
    _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public RaftGroupInfo Clone() {
    return new RaftGroupInfo(this);
  }

  /// <summary>Field number for the "BootSchemaVersion" field.</summary>
  public const int BootSchemaVersionFieldNumber = 1;
  private uint bootSchemaVersion_;
  /// <summary>
  ///创建时的SchemaVersion, 仅用于TableRaftGroup引导启动时确定版本号
  /// </summary>
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public uint BootSchemaVersion {
    get { return bootSchemaVersion_; }
    set {
      bootSchemaVersion_ = value;
    }
  }

  /// <summary>Field number for the "Nodes" field.</summary>
  public const int NodesFieldNumber = 2;
  private static readonly pb::FieldCodec<global::RaftNodeInfo> _repeated_nodes_codec
      = pb::FieldCodec.ForMessage(18, global::RaftNodeInfo.Parser);
  private readonly pbc::RepeatedField<global::RaftNodeInfo> nodes_ = new pbc::RepeatedField<global::RaftNodeInfo>();
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public pbc::RepeatedField<global::RaftNodeInfo> Nodes {
    get { return nodes_; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override bool Equals(object other) {
    return Equals(other as RaftGroupInfo);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public bool Equals(RaftGroupInfo other) {
    if (ReferenceEquals(other, null)) {
      return false;
    }
    if (ReferenceEquals(other, this)) {
      return true;
    }
    if (BootSchemaVersion != other.BootSchemaVersion) return false;
    if(!nodes_.Equals(other.nodes_)) return false;
    return Equals(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override int GetHashCode() {
    int hash = 1;
    if (BootSchemaVersion != 0) hash ^= BootSchemaVersion.GetHashCode();
    hash ^= nodes_.GetHashCode();
    if (_unknownFields != null) {
      hash ^= _unknownFields.GetHashCode();
    }
    return hash;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override string ToString() {
    return pb::JsonFormatter.ToDiagnosticString(this);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void WriteTo(pb::CodedOutputStream output) {
    if (BootSchemaVersion != 0) {
      output.WriteRawTag(8);
      output.WriteUInt32(BootSchemaVersion);
    }
    nodes_.WriteTo(output, _repeated_nodes_codec);
    if (_unknownFields != null) {
      _unknownFields.WriteTo(output);
    }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public int CalculateSize() {
    int size = 0;
    if (BootSchemaVersion != 0) {
      size += 1 + pb::CodedOutputStream.ComputeUInt32Size(BootSchemaVersion);
    }
    size += nodes_.CalculateSize(_repeated_nodes_codec);
    if (_unknownFields != null) {
      size += _unknownFields.CalculateSize();
    }
    return size;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void MergeFrom(RaftGroupInfo other) {
    if (other == null) {
      return;
    }
    if (other.BootSchemaVersion != 0) {
      BootSchemaVersion = other.BootSchemaVersion;
    }
    nodes_.Add(other.nodes_);
    _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void MergeFrom(pb::CodedInputStream input) {
    uint tag;
    while ((tag = input.ReadTag()) != 0) {
      switch(tag) {
        default:
          _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
          break;
        case 8: {
          BootSchemaVersion = input.ReadUInt32();
          break;
        }
        case 18: {
          nodes_.AddEntriesFrom(input, _repeated_nodes_codec);
          break;
        }
      }
    }
  }

}

/// <summary>
/// 仅用于普通节点加入集群时的返回值
/// </summary>
public sealed partial class JoinClusterResult : pb::IMessage<JoinClusterResult> {
  private static readonly pb::MessageParser<JoinClusterResult> _parser = new pb::MessageParser<JoinClusterResult>(() => new JoinClusterResult());
  private pb::UnknownFieldSet _unknownFields;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public static pb::MessageParser<JoinClusterResult> Parser { get { return _parser; } }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public static pbr::MessageDescriptor Descriptor {
    get { return global::PeerReflection.Descriptor.MessageTypes[3]; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  pbr::MessageDescriptor pb::IMessage.Descriptor {
    get { return Descriptor; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public JoinClusterResult() {
    OnConstruction();
  }

  partial void OnConstruction();

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public JoinClusterResult(JoinClusterResult other) : this() {
    errorMsg_ = other.errorMsg_;
    LocalEndpoint = other.localEndpoint_ != null ? other.LocalEndpoint.Clone() : null;
    metaNodes_ = other.metaNodes_.Clone();
    _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public JoinClusterResult Clone() {
    return new JoinClusterResult(this);
  }

  /// <summary>Field number for the "ErrorMsg" field.</summary>
  public const int ErrorMsgFieldNumber = 1;
  private string errorMsg_ = "";
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public string ErrorMsg {
    get { return errorMsg_; }
    set {
      errorMsg_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
    }
  }

  /// <summary>Field number for the "LocalEndpoint" field.</summary>
  public const int LocalEndpointFieldNumber = 2;
  private global::PeerEndpoint localEndpoint_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public global::PeerEndpoint LocalEndpoint {
    get { return localEndpoint_; }
    set {
      localEndpoint_ = value;
    }
  }

  /// <summary>Field number for the "MetaNodes" field.</summary>
  public const int MetaNodesFieldNumber = 3;
  private static readonly pb::FieldCodec<global::RaftNodeInfo> _repeated_metaNodes_codec
      = pb::FieldCodec.ForMessage(26, global::RaftNodeInfo.Parser);
  private readonly pbc::RepeatedField<global::RaftNodeInfo> metaNodes_ = new pbc::RepeatedField<global::RaftNodeInfo>();
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public pbc::RepeatedField<global::RaftNodeInfo> MetaNodes {
    get { return metaNodes_; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override bool Equals(object other) {
    return Equals(other as JoinClusterResult);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public bool Equals(JoinClusterResult other) {
    if (ReferenceEquals(other, null)) {
      return false;
    }
    if (ReferenceEquals(other, this)) {
      return true;
    }
    if (ErrorMsg != other.ErrorMsg) return false;
    if (!object.Equals(LocalEndpoint, other.LocalEndpoint)) return false;
    if(!metaNodes_.Equals(other.metaNodes_)) return false;
    return Equals(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override int GetHashCode() {
    int hash = 1;
    if (ErrorMsg.Length != 0) hash ^= ErrorMsg.GetHashCode();
    if (localEndpoint_ != null) hash ^= LocalEndpoint.GetHashCode();
    hash ^= metaNodes_.GetHashCode();
    if (_unknownFields != null) {
      hash ^= _unknownFields.GetHashCode();
    }
    return hash;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override string ToString() {
    return pb::JsonFormatter.ToDiagnosticString(this);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void WriteTo(pb::CodedOutputStream output) {
    if (ErrorMsg.Length != 0) {
      output.WriteRawTag(10);
      output.WriteString(ErrorMsg);
    }
    if (localEndpoint_ != null) {
      output.WriteRawTag(18);
      output.WriteMessage(LocalEndpoint);
    }
    metaNodes_.WriteTo(output, _repeated_metaNodes_codec);
    if (_unknownFields != null) {
      _unknownFields.WriteTo(output);
    }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public int CalculateSize() {
    int size = 0;
    if (ErrorMsg.Length != 0) {
      size += 1 + pb::CodedOutputStream.ComputeStringSize(ErrorMsg);
    }
    if (localEndpoint_ != null) {
      size += 1 + pb::CodedOutputStream.ComputeMessageSize(LocalEndpoint);
    }
    size += metaNodes_.CalculateSize(_repeated_metaNodes_codec);
    if (_unknownFields != null) {
      size += _unknownFields.CalculateSize();
    }
    return size;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void MergeFrom(JoinClusterResult other) {
    if (other == null) {
      return;
    }
    if (other.ErrorMsg.Length != 0) {
      ErrorMsg = other.ErrorMsg;
    }
    if (other.localEndpoint_ != null) {
      if (localEndpoint_ == null) {
        localEndpoint_ = new global::PeerEndpoint();
      }
      LocalEndpoint.MergeFrom(other.LocalEndpoint);
    }
    metaNodes_.Add(other.metaNodes_);
    _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void MergeFrom(pb::CodedInputStream input) {
    uint tag;
    while ((tag = input.ReadTag()) != 0) {
      switch(tag) {
        default:
          _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
          break;
        case 10: {
          ErrorMsg = input.ReadString();
          break;
        }
        case 18: {
          if (localEndpoint_ == null) {
            localEndpoint_ = new global::PeerEndpoint();
          }
          input.ReadMessage(localEndpoint_);
          break;
        }
        case 26: {
          metaNodes_.AddEntriesFrom(input, _repeated_metaNodes_codec);
          break;
        }
      }
    }
  }

}

/// <summary>
/// 当前节点的配置信息
/// </summary>
public sealed partial class PeerConfig : pb::IMessage<PeerConfig> {
  private static readonly pb::MessageParser<PeerConfig> _parser = new pb::MessageParser<PeerConfig>(() => new PeerConfig());
  private pb::UnknownFieldSet _unknownFields;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public static pb::MessageParser<PeerConfig> Parser { get { return _parser; } }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public static pbr::MessageDescriptor Descriptor {
    get { return global::PeerReflection.Descriptor.MessageTypes[4]; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  pbr::MessageDescriptor pb::IMessage.Descriptor {
    get { return Descriptor; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public PeerConfig() {
    OnConstruction();
  }

  partial void OnConstruction();

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public PeerConfig(PeerConfig other) : this() {
    peerId_ = other.peerId_;
    webPort_ = other.webPort_;
    RpcEndpoint = other.rpcEndpoint_ != null ? other.RpcEndpoint.Clone() : null;
    metaNodes_ = other.metaNodes_.Clone();
    _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public PeerConfig Clone() {
    return new PeerConfig(this);
  }

  /// <summary>Field number for the "PeerId" field.</summary>
  public const int PeerIdFieldNumber = 1;
  private uint peerId_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public uint PeerId {
    get { return peerId_; }
    set {
      peerId_ = value;
    }
  }

  /// <summary>Field number for the "WebPort" field.</summary>
  public const int WebPortFieldNumber = 2;
  private uint webPort_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public uint WebPort {
    get { return webPort_; }
    set {
      webPort_ = value;
    }
  }

  /// <summary>Field number for the "RpcEndpoint" field.</summary>
  public const int RpcEndpointFieldNumber = 3;
  private global::PeerEndpoint rpcEndpoint_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public global::PeerEndpoint RpcEndpoint {
    get { return rpcEndpoint_; }
    set {
      rpcEndpoint_ = value;
    }
  }

  /// <summary>Field number for the "MetaNodes" field.</summary>
  public const int MetaNodesFieldNumber = 4;
  private static readonly pb::FieldCodec<global::RaftNodeInfo> _repeated_metaNodes_codec
      = pb::FieldCodec.ForMessage(34, global::RaftNodeInfo.Parser);
  private readonly pbc::RepeatedField<global::RaftNodeInfo> metaNodes_ = new pbc::RepeatedField<global::RaftNodeInfo>();
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public pbc::RepeatedField<global::RaftNodeInfo> MetaNodes {
    get { return metaNodes_; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override bool Equals(object other) {
    return Equals(other as PeerConfig);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public bool Equals(PeerConfig other) {
    if (ReferenceEquals(other, null)) {
      return false;
    }
    if (ReferenceEquals(other, this)) {
      return true;
    }
    if (PeerId != other.PeerId) return false;
    if (WebPort != other.WebPort) return false;
    if (!object.Equals(RpcEndpoint, other.RpcEndpoint)) return false;
    if(!metaNodes_.Equals(other.metaNodes_)) return false;
    return Equals(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override int GetHashCode() {
    int hash = 1;
    if (PeerId != 0) hash ^= PeerId.GetHashCode();
    if (WebPort != 0) hash ^= WebPort.GetHashCode();
    if (rpcEndpoint_ != null) hash ^= RpcEndpoint.GetHashCode();
    hash ^= metaNodes_.GetHashCode();
    if (_unknownFields != null) {
      hash ^= _unknownFields.GetHashCode();
    }
    return hash;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override string ToString() {
    return pb::JsonFormatter.ToDiagnosticString(this);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void WriteTo(pb::CodedOutputStream output) {
    if (PeerId != 0) {
      output.WriteRawTag(13);
      output.WriteFixed32(PeerId);
    }
    if (WebPort != 0) {
      output.WriteRawTag(16);
      output.WriteUInt32(WebPort);
    }
    if (rpcEndpoint_ != null) {
      output.WriteRawTag(26);
      output.WriteMessage(RpcEndpoint);
    }
    metaNodes_.WriteTo(output, _repeated_metaNodes_codec);
    if (_unknownFields != null) {
      _unknownFields.WriteTo(output);
    }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public int CalculateSize() {
    int size = 0;
    if (PeerId != 0) {
      size += 1 + 4;
    }
    if (WebPort != 0) {
      size += 1 + pb::CodedOutputStream.ComputeUInt32Size(WebPort);
    }
    if (rpcEndpoint_ != null) {
      size += 1 + pb::CodedOutputStream.ComputeMessageSize(RpcEndpoint);
    }
    size += metaNodes_.CalculateSize(_repeated_metaNodes_codec);
    if (_unknownFields != null) {
      size += _unknownFields.CalculateSize();
    }
    return size;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void MergeFrom(PeerConfig other) {
    if (other == null) {
      return;
    }
    if (other.PeerId != 0) {
      PeerId = other.PeerId;
    }
    if (other.WebPort != 0) {
      WebPort = other.WebPort;
    }
    if (other.rpcEndpoint_ != null) {
      if (rpcEndpoint_ == null) {
        rpcEndpoint_ = new global::PeerEndpoint();
      }
      RpcEndpoint.MergeFrom(other.RpcEndpoint);
    }
    metaNodes_.Add(other.metaNodes_);
    _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void MergeFrom(pb::CodedInputStream input) {
    uint tag;
    while ((tag = input.ReadTag()) != 0) {
      switch(tag) {
        default:
          _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
          break;
        case 13: {
          PeerId = input.ReadFixed32();
          break;
        }
        case 16: {
          WebPort = input.ReadUInt32();
          break;
        }
        case 26: {
          if (rpcEndpoint_ == null) {
            rpcEndpoint_ = new global::PeerEndpoint();
          }
          input.ReadMessage(rpcEndpoint_);
          break;
        }
        case 34: {
          metaNodes_.AddEntriesFrom(input, _repeated_metaNodes_codec);
          break;
        }
      }
    }
  }

}

/// <summary>
//// 节点状态
/// </summary>
public sealed partial class PeerState : pb::IMessage<PeerState> {
  private static readonly pb::MessageParser<PeerState> _parser = new pb::MessageParser<PeerState>(() => new PeerState());
  private pb::UnknownFieldSet _unknownFields;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public static pb::MessageParser<PeerState> Parser { get { return _parser; } }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public static pbr::MessageDescriptor Descriptor {
    get { return global::PeerReflection.Descriptor.MessageTypes[5]; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  pbr::MessageDescriptor pb::IMessage.Descriptor {
    get { return Descriptor; }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public PeerState() {
    OnConstruction();
  }

  partial void OnConstruction();

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public PeerState(PeerState other) : this() {
    Endpoint = other.endpoint_ != null ? other.Endpoint.Clone() : null;
    raftNodeCount_ = other.raftNodeCount_;
    _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public PeerState Clone() {
    return new PeerState(this);
  }

  /// <summary>Field number for the "Endpoint" field.</summary>
  public const int EndpointFieldNumber = 1;
  private global::PeerEndpoint endpoint_;
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public global::PeerEndpoint Endpoint {
    get { return endpoint_; }
    set {
      endpoint_ = value;
    }
  }

  /// <summary>Field number for the "RaftNodeCount" field.</summary>
  public const int RaftNodeCountFieldNumber = 2;
  private uint raftNodeCount_;
  /// <summary>
  /// 当前节点分配的RaftNode总数, TODO: 考虑存在必要性
  /// </summary>
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public uint RaftNodeCount {
    get { return raftNodeCount_; }
    set {
      raftNodeCount_ = value;
    }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override bool Equals(object other) {
    return Equals(other as PeerState);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public bool Equals(PeerState other) {
    if (ReferenceEquals(other, null)) {
      return false;
    }
    if (ReferenceEquals(other, this)) {
      return true;
    }
    if (!object.Equals(Endpoint, other.Endpoint)) return false;
    if (RaftNodeCount != other.RaftNodeCount) return false;
    return Equals(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override int GetHashCode() {
    int hash = 1;
    if (endpoint_ != null) hash ^= Endpoint.GetHashCode();
    if (RaftNodeCount != 0) hash ^= RaftNodeCount.GetHashCode();
    if (_unknownFields != null) {
      hash ^= _unknownFields.GetHashCode();
    }
    return hash;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public override string ToString() {
    return pb::JsonFormatter.ToDiagnosticString(this);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void WriteTo(pb::CodedOutputStream output) {
    if (endpoint_ != null) {
      output.WriteRawTag(10);
      output.WriteMessage(Endpoint);
    }
    if (RaftNodeCount != 0) {
      output.WriteRawTag(16);
      output.WriteUInt32(RaftNodeCount);
    }
    if (_unknownFields != null) {
      _unknownFields.WriteTo(output);
    }
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public int CalculateSize() {
    int size = 0;
    if (endpoint_ != null) {
      size += 1 + pb::CodedOutputStream.ComputeMessageSize(Endpoint);
    }
    if (RaftNodeCount != 0) {
      size += 1 + pb::CodedOutputStream.ComputeUInt32Size(RaftNodeCount);
    }
    if (_unknownFields != null) {
      size += _unknownFields.CalculateSize();
    }
    return size;
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void MergeFrom(PeerState other) {
    if (other == null) {
      return;
    }
    if (other.endpoint_ != null) {
      if (endpoint_ == null) {
        endpoint_ = new global::PeerEndpoint();
      }
      Endpoint.MergeFrom(other.Endpoint);
    }
    if (other.RaftNodeCount != 0) {
      RaftNodeCount = other.RaftNodeCount;
    }
    _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
  }

  [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
  public void MergeFrom(pb::CodedInputStream input) {
    uint tag;
    while ((tag = input.ReadTag()) != 0) {
      switch(tag) {
        default:
          _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
          break;
        case 10: {
          if (endpoint_ == null) {
            endpoint_ = new global::PeerEndpoint();
          }
          input.ReadMessage(endpoint_);
          break;
        }
        case 16: {
          RaftNodeCount = input.ReadUInt32();
          break;
        }
      }
    }
  }

}

#endregion


#endregion Designer generated code
