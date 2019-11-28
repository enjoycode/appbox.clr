using System;
using appbox.Models;
using appbox.Serialization;

namespace appbox.Design
{
    /// <summary>
    /// 用于包装设计器向服务端发送的签出请求
    /// </summary>
    sealed class CheckoutInfo
    {
        public DesignNodeType NodeType { get; private set; }
        public bool IsSingleModel => NodeType >= DesignNodeType.EntityModelNode;
        public string TargetID { get; private set; }
        public uint Version { get; private set; }
        public string DeveloperName { get; private set; }
        public Guid DeveloperOuid { get; private set; }
        public DateTime CheckoutTime { get; set; }

        public CheckoutInfo(DesignNodeType nodeType, string targetID,
                            uint version, string developerName, Guid developerOuID)
        {
            NodeType = nodeType;
            TargetID = targetID;
            Version = version;
            DeveloperName = developerName;
            DeveloperOuid = developerOuID;
            CheckoutTime = DateTime.Now;
        }

        public string GetKey() => MakeKey(NodeType, TargetID);

        internal static string MakeKey(DesignNodeType nodeType, string targetId)
        {
            return $"{(byte)nodeType}|{targetId}";
        }

    }

}
