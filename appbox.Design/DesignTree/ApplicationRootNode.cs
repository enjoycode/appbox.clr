using System;

namespace appbox.Design
{
    public sealed class ApplicationRootNode : DesignNode, ITopNode
    {
        public override DesignNodeType NodeType => DesignNodeType.ApplicationRoot;

        internal override int SortNo { get { return 1; } }

        private readonly DesignTree _treeView;
        public override DesignTree DesignTree => _treeView;

        public ApplicationRootNode(DesignTree treeView)
        {
            _treeView = treeView;
            Text = "Applications";
        }

    }
}
