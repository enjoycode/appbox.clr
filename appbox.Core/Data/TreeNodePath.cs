using System;
using System.Collections.Generic;
using appbox.Serialization;

namespace appbox.Data
{
    /// <summary>
    /// 用于描述树型Entity节点的全路径，只读
    /// </summary>
    public sealed class TreeNodePath : IBinSerializable
    {

        private TreeNodeInfo[] nodes;

        public int Level
        {
            get { return nodes.Length; }
        }

        public TreeNodeInfo this[int index]
        {
            get { return nodes[index]; }
            internal set { nodes[index] = value; }
        }

        #region ====Ctor====
        /// <summary>
        /// Ctor for serialization
        /// </summary>
        internal TreeNodePath() { }

        internal TreeNodePath(int levels)
        {
            nodes = new TreeNodeInfo[levels];
        }

        public TreeNodePath(List<TreeNodeInfo> ns)
        {
            nodes = new TreeNodeInfo[ns.Count];
            for (int i = 0; i < ns.Count; i++)
            {
                nodes[i] = new TreeNodeInfo() { ID = ns[i].ID, Text = ns[i].Text };
            }
        }
        #endregion

        #region ====Serialization====
        void IBinSerializable.WriteObject(BinSerializer writer)
        {
            writer.Write(nodes.Length);
            for (int i = 0; i < nodes.Length; i++)
            {
                writer.Write(nodes[i].ID);
                writer.Write(nodes[i].Text);
            }
        }

        void IBinSerializable.ReadObject(BinSerializer reader)
        {
            int len = reader.ReadInt32();
            nodes = new TreeNodeInfo[len];
            for (int i = 0; i < len; i++)
            {
                nodes[i] = new TreeNodeInfo() { ID = reader.ReadGuid(), Text = reader.ReadString() };
            }
        }
        #endregion
    }

    public struct TreeNodeInfo
    {
        public Guid ID;
        public string Text;
    }

}
