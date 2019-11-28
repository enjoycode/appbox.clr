using System;
using appbox.Data;
using appbox.Runtime;

namespace appbox.Server
{
    /// <summary>
    /// 用于跨进程调用的会话
    /// </summary>
    public sealed class RoutedSession : ISessionInfo
    {
        private readonly Guid? _emploeeID;

        public Guid EmploeeID => _emploeeID ?? Guid.Empty;

        internal TreeNodePath TreeNodePath { get; }

        public ulong SessionID { get; }

        public string Tag { get; }

        int ISessionInfo.Levels => TreeNodePath.Level;

        public bool IsExternal => !_emploeeID.HasValue;

        Guid ISessionInfo.LeafOrgUnitID => _emploeeID.HasValue ? TreeNodePath[0].ID : TreeNodePath[1].ID;

        Guid ISessionInfo.ExternalID => _emploeeID.HasValue ? Guid.Empty : TreeNodePath[0].ID;

        public TreeNodeInfo this[int index] => TreeNodePath[index];

        ulong ISessionInfo.SessionID => SessionID;

        string ISessionInfo.Name => TreeNodePath[0].Text;

        string ISessionInfo.FullName => this.GetFullName();

        public RoutedSession(ulong id, TreeNodePath path, Guid? empID, string tag)
        {
            SessionID = id;
            _emploeeID = empID;
            TreeNodePath = path;
            Tag = tag;
        }
    }
}
