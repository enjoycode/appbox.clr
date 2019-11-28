using System;
using System.Collections.Generic;
using appbox.Data;
using appbox.Runtime;

namespace appbox.Design.Tests
{
    sealed class MockDeveloperSession : IDeveloperSession
    {

        readonly TreeNodePath _treeNodePath;
        readonly Guid? _emploeeID;
        DesignHub _ctx;

        public MockDeveloperSession()
        {
            var dummyOuid = new Guid("11111111-1111-1111-1111-111111111111");
            var nodes = new List<TreeNodeInfo>() { new TreeNodeInfo() { ID = dummyOuid, Text = "Admin" } };
            _treeNodePath = new TreeNodePath(nodes);
            _emploeeID = new Guid("11111111-1111-1111-1111-222222222222");
        }

        public TreeNodeInfo this[int index] => _treeNodePath[index];

        public bool IsExternal => !_emploeeID.HasValue;

        public string Tag => null;

        public ulong SessionID => throw new NotImplementedException();

        public int Levels => _treeNodePath.Level;

        public Guid LeafOrgUnitID => _emploeeID.HasValue ? _treeNodePath[0].ID : _treeNodePath[1].ID;

        public Guid EmploeeID => _emploeeID ?? Guid.Empty;

        public Guid ExternalID => _emploeeID.HasValue ? Guid.Empty : _treeNodePath[0].ID;

        public string Name => _treeNodePath[0].Text;

        public string FullName => _treeNodePath[0].Text;

        public DesignHub GetDesignHub()
        {
            if (_ctx != null)
                return _ctx;

            lock (this)
            {
                if(_ctx == null)
                    _ctx = new DesignHub(this);
            }
            return _ctx;
        }

        public void SendEvent(int source, string body)
        {
            throw new NotImplementedException();
        }
    }
}
