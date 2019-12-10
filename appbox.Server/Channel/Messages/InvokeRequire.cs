using System;
using appbox.Data;
using appbox.Runtime;
using appbox.Serialization;

namespace appbox.Server
{
    /// <summary>
    /// Invoke require.
    /// </summary>
    /// <remarks>
    /// 1. (Client->)Host进程 -> App进程
    /// 2. Host进程 -> App进程
    /// 3. Debug进程 -> Host进程 -> App进程
    /// </remarks>
    public struct InvokeRequire : IMessage
    {
        public InvokeSource Source { get; private set; }
        public InvokeProtocol ContentType { get; private set; }
        /// <summary>
        /// 异步等待句柄
        /// </summary>
        public IntPtr WaitHandle { get; private set; }
        /// <summary>
        /// 请求发起者的消息标识号
        /// </summary>
        public int SourceMsgId { get; private set; }

        /// <summary>
        /// 服务全名称 eg: "sys.HelloService.SayHello"
        /// </summary>
        public string Service { get; private set; }
        public InvokeArgs Args { get; private set; }
        /// <summary>
        /// 发起调用的会话信息
        /// </summary>
        public ISessionInfo Session { get; private set; }

        public MessageType Type { get { return MessageType.InvokeRequire; } }

        public PayloadType PayloadType { get { return PayloadType.InvokeRequire; } }

        public InvokeRequire(InvokeSource source, InvokeProtocol contentType,
                             IntPtr waitHandle, string service, InvokeArgs args, int sourceMsgId,
                             ISessionInfo session = null)
        {
            Source = source;
            ContentType = contentType;
            WaitHandle = waitHandle;
            SourceMsgId = sourceMsgId;
            Service = service;
            Args = args;
            WaitHandle = waitHandle;
            Session = session;
        }

        #region ====Serialization====
        public void WriteObject(BinSerializer bs)
        {
            bs.Write((byte)Source);
            bs.Write((byte)ContentType);
            bs.Write(WaitHandle.ToInt64());
            bs.Write(SourceMsgId);
            bs.Write(Service);
            //注意不同类型的Session相同的序列化方式
            bs.Write(Session != null);
            if (Session != null)
            {
                bs.Write(Session.SessionID); //TODO:检查是否需要
                bs.Write(Session.Levels);
                for (int i = 0; i < Session.Levels; i++)
                {
                    bs.Write(Session[i].ID);
                    bs.Write(Session[i].Text);
                }
                bs.Write(Session.IsExternal);
                if (!Session.IsExternal)
                    bs.Write(Session.EmploeeID);
                bs.Write(Session.Tag);
            }
            //注意最后序列化参数
            Args.WriteObject(bs);
        }

        public void ReadObject(BinSerializer bs)
        {
            Source = (InvokeSource)bs.ReadByte();
            ContentType = (InvokeProtocol)bs.ReadByte();
            WaitHandle = new IntPtr(bs.ReadInt64());
            SourceMsgId = bs.ReadInt32();
            Service = bs.ReadString();
            //注意统一转换为RoutedSession
            if (bs.ReadBoolean())
            {
                ulong id = bs.ReadUInt64();
                int levels = bs.ReadInt32();
                var path = new TreeNodePath(levels);
                for (int i = 0; i < levels; i++)
                {
                    path[i] = new TreeNodeInfo { ID = bs.ReadGuid(), Text = bs.ReadString() };
                }
                Guid? empID = null;
                bool isExternal = bs.ReadBoolean();
                if (!isExternal)
                    empID = bs.ReadGuid();
                string tag = bs.ReadString();
                Session = new RoutedSession(id, path, empID, tag);
            }
            var args = new InvokeArgs();
            args.ReadObject(bs);
            Args = args; //Don't use Args.ReadObject(bs)
        }
        #endregion
    }

}
