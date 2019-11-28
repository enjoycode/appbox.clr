using System;
using System.Runtime.InteropServices;

namespace appbox.Server
{
    internal enum ConfChangeType : byte
    {
        ConfChangeAddNode = 0,
        ConfChangeRemoveNode = 1,
        ConfChangeUpdateNode = 2,
        ConfChangeAddLearnerNode = 3
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct IteratorKV
    {
        internal IntPtr KeyPtr;
        internal IntPtr KeySize;
        internal IntPtr ValuePtr;
        internal IntPtr ValueSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PartitionInfo
    {
        internal IntPtr KeyPtr;
        internal IntPtr KeySize;
        internal byte Flags; //2bit RaftType + 1bit MvccFlag + 1bit OrderFlag
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ClrScanRequire
    {
        internal ulong RaftGroupId;
        internal IntPtr BeginKeyPtr;
        internal IntPtr BeginKeySize;
        internal IntPtr EndKeyPtr;
        internal IntPtr EndKeySize;
        internal IntPtr FilterPtr;
        internal uint Skip;
        internal uint Take;
        internal int DataCF;
        internal bool ToIndexTarget;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ClrInsertRequire
    {
        internal ulong RaftGroupId;
        internal IntPtr KeyPtr;
        internal IntPtr KeySize;
        internal IntPtr RefsPtr;
        internal IntPtr RefsSize;
        internal IntPtr DataPtr;
        internal uint SchemaVersion;
        internal sbyte DataCF;
        internal bool OverrideIfExists;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ClrUpdateRequire
    {
        internal ulong RaftGroupId;
        internal IntPtr KeyPtr;
        internal IntPtr KeySize;
        internal IntPtr RefsPtr;
        internal IntPtr RefsSize;
        internal IntPtr DataPtr;
        internal uint SchemaVersion;
        internal sbyte DataCF;
        internal bool Merge;
        internal bool ReturnExists;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ClrDeleteRequire
    {
        internal ulong RaftGroupId;
        internal IntPtr KeyPtr;
        internal IntPtr KeySize;
        internal IntPtr RefsPtr;
        internal IntPtr RefsSize;
        internal uint SchemaVersion;
        internal sbyte DataCF;
        internal bool ReturnExists;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ClrAddRefRequire
    {
        internal ulong TargetRaftGroupId;
        internal ulong FromRaftGroupId;
        internal IntPtr KeyPtr;
        internal IntPtr KeySize;
        internal uint FromTableId;
        internal int Diff;
    }
}
