using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using appbox.Serialization;

namespace appbox.Server
{
    /// <summary>
    /// 应用或调试进程的通讯通道，每个实例包含两个单向消息队列
    /// </summary>
    /// <remarks>
    /// 统一由Host进程负责创建，各子进程打开已创建的
    /// </remarks>
    public sealed class SharedMemoryChannel : IMessageChannel
    {
        #region ====Static Ctor for register serializer====
        static SharedMemoryChannel()
        {
            BinSerializer.RegisterKnownType(new UserSerializer(PayloadType.InvokeRequire, typeof(InvokeRequire), () => new InvokeRequire()));
            BinSerializer.RegisterKnownType(new UserSerializer(PayloadType.InvokeResponse, typeof(InvokeResponse), () => new InvokeResponse()));
            BinSerializer.RegisterKnownType(new UserSerializer(PayloadType.InvalidModelsCache, typeof(InvalidModelsCache), () => new InvalidModelsCache()));
            BinSerializer.RegisterKnownType(new UserSerializer(PayloadType.MetricRequire, typeof(MetricRequire), () => new MetricRequire()));
#if FUTURE
            BinSerializer.RegisterKnownType(new UserSerializer(PayloadType.KVGetRequire, typeof(KVGetRequire), () => new KVGetRequire()));
            BinSerializer.RegisterKnownType(new UserSerializer(PayloadType.KVScanRequire, typeof(KVScanRequire), () => new KVScanRequire()));
            BinSerializer.RegisterKnownType(new UserSerializer(PayloadType.BeginTranRequire, typeof(BeginTranRequire), () => new BeginTranRequire()));
            BinSerializer.RegisterKnownType(new UserSerializer(PayloadType.CommitTranRequire, typeof(CommitTranRequire), () => new CommitTranRequire()));
            BinSerializer.RegisterKnownType(new UserSerializer(PayloadType.RollbackTranRequire, typeof(RollbackTranRequire), () => new RollbackTranRequire()));
            BinSerializer.RegisterKnownType(new UserSerializer(PayloadType.GenPartitionRequire, typeof(GenPartitionRequire), () => new GenPartitionRequire()));
            BinSerializer.RegisterKnownType(new UserSerializer(PayloadType.KVInsertRequire, typeof(KVInsertRequire), () => new KVInsertRequire()));
            BinSerializer.RegisterKnownType(new UserSerializer(PayloadType.KVDeleteRequire, typeof(KVDeleteRequire), () => new KVDeleteRequire()));
            BinSerializer.RegisterKnownType(new UserSerializer(PayloadType.KVAddRefRequire, typeof(KVAddRefRequire), () => new KVAddRefRequire()));
#endif
        }
#endregion

        private int sendMsgIdIndex;
        /// <summary>
        /// 仅Host进程有效,表示远端的运行时标识，用于Host进程收到存储消息时转发至相应的目标
        /// </summary>
        public ulong RemoteRuntimeId { get; }

        private readonly SharedMessageQueue _sendQueue;
        private readonly SharedMessageQueue _receiveQueue;

        private readonly Dictionary<int, IntPtr> _pendingMsgs;
        private readonly IMessageDispatcher _msgDispatcher;

        public SharedMemoryChannel(string name, int count, IMessageDispatcher dispatcher, ulong remoteRuntimeId)
        {
            //TODO:判断Host进程

            _sendQueue = new SharedMessageQueue($"{name}-S", count);
            _receiveQueue = new SharedMessageQueue($"{name}-R", count);
            _pendingMsgs = new Dictionary<int, IntPtr>();
            _msgDispatcher = dispatcher;
            RemoteRuntimeId = remoteRuntimeId;
        }

        public SharedMemoryChannel(string name, IMessageDispatcher dispatcher)
        {
            //注意队列名称相反
            _sendQueue = new SharedMessageQueue($"{name}-R");
            _receiveQueue = new SharedMessageQueue($"{name}-S");
            _pendingMsgs = new Dictionary<int, IntPtr>();
            _msgDispatcher = dispatcher;
        }

#region ====Receive Methods====
        public void StartReceive()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    StartReceiveOnCurrentThread();
                }
                catch (Exception ex)
                {
                    Log.Warn($"接收线程错误: {ex.Message}\n{ex.StackTrace}");
                }
            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// 在当前线程启用接收循环，无消息阻塞当前线程
        /// </summary>
        public void StartReceiveOnCurrentThread()
        {
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
            while (true)
            {
                unsafe
                {
                    MessageChunk* chunk = _receiveQueue.GetMessageChunkForRead();
                    if (chunk != null)
                    {
                        if (chunk->Type == 255)
                        {
                            break;
                        }
                        OnMessageChunk(chunk);
                    }
                }
            }
            Log.Debug("Stopped receive loop.");
        }

        public void StopReceive()
        {
            //使用发送特定消息的方式通知接收Loop停止
            unsafe
            {
                MessageChunk* chunk = _receiveQueue.GetMessageChunkForWrite();
                chunk->Type = 255;
                chunk->DataLength = 0;
                _receiveQueue.PostMessageChunk(chunk);
            }
        }

#if DEBUG
        /// <summary>
        /// Only for test
        /// </summary>
        public unsafe void ReceiveOneForTest()
        {
            MessageChunk* chunk = _receiveQueue.GetMessageChunkForRead();
            _receiveQueue.ReturnMessageChunk(chunk);
        }
#endif

        private unsafe void OnMessageChunk(MessageChunk* chunk)
        {
            //Log.Debug($"[{System.Diagnostics.Process.GetCurrentProcess().ProcessName}]收到:{(MessageType)chunk->Type}");
            var msgId = chunk->ID;
            bool isEnd = (chunk->Flag & (byte)MessageFlag.LastChunk) == (byte)MessageFlag.LastChunk;

            MessageChunk* curChunk = chunk;
            curChunk->Next = null;

            if (_pendingMsgs.TryGetValue(msgId, out IntPtr preChunkPtr))
            {
                MessageChunk* preChunk = (MessageChunk*)preChunkPtr;
                curChunk->First = preChunk->First;
                preChunk->Next = curChunk;
                if (isEnd) //收到完整消息，交给消息处理器处理
                {
                    _pendingMsgs.Remove(msgId); //先从挂起的消息字典表中移除
                    ProcessMessage(curChunk->First);//再处理完整的消息
                }
                else
                {
                    _pendingMsgs[msgId] = new IntPtr(curChunk); //重置消息ID的最后一包
                }
            }
            else
            {
                curChunk->First = curChunk;
                if (isEnd) //就一包
                    ProcessMessage(curChunk); //直接交给消息处理器
                else
                    _pendingMsgs.Add(msgId, new IntPtr(curChunk));
            }
        }

        private unsafe void ProcessMessage(MessageChunk* firstChunk)
        {
            //注意：除特殊消息(eg: CancelMessage)外交给MessageDispatcher处理
            _msgDispatcher.ProcessMessage(this, firstChunk);
        }

        /// <summary>
        /// 反序列化消息 注意：不管是否成功归还缓存块至通道
        /// </summary>
        /// <exception>只抛出MessageSerializationException</exception>
        public unsafe T Deserialize<T>(MessageChunk* first) where T : struct, IMessage
        {
            if (first == null)
                throw new MessageSerializationException(MessageSerilizationErrorCode.DeserializeFailByFirstSegmentIsNull, null);
            //??注意：不能判断first->First == first, 因为AppContainer子进程内收到的路由消息已修改第一包

            var mrs = MessageReadStream.ThreadInstance;
            mrs.Reset(first);
            BinSerializer bs = BinSerializer.ThreadInstance;
            bs.Init(mrs);
            T res = default;
            try
            {
                var payloadType = bs.ReadByte(); //不用res = bs.Deserialize();避免box
                if (payloadType != (byte)res.PayloadType)
                    throw new MessageSerializationException(MessageSerilizationErrorCode.DeserializeFailByMessageType, null);

                res.ReadObject(bs);
                mrs.Close();
            }
            catch (MessageSerializationException)
            {
                throw;
            }
            catch (Exception ex) //TODO:单独捕获解密错误信息
            {
                //Log.Warn(ex.StackTrace);
                throw new MessageSerializationException(MessageSerilizationErrorCode.DeserializeFail, ex);
            }
            finally
            {
                ReturnMessageChunks(first);
                bs.Clear();
            }

            return res;
        }

        public unsafe void ReturnMessageChunks(MessageChunk* first)
        {
            MessageChunk* cur = first;
            MessageChunk* next = null;
            while (cur != null)
            {
                next = cur->Next;
                _receiveQueue.ReturnMessageChunk(cur);
                cur = next;
            }
        }
#endregion

#region ====Send Methods====
        /// <summary>
        /// 序列化并发送消息，如果序列化异常标记消息为错误状态仍旧发送,接收端根据消息类型是请求还是响应作不同处理
        /// </summary>
        public void SendMessage<T>(ref T msg) where T : struct, IMessage
        {
            //Log.Debug($"[{System.Diagnostics.Process.GetCurrentProcess().ProcessName}]发送:{msg.Type}");
            MessageFlag flag = MessageFlag.None;
            int msgId = Interlocked.Increment(ref sendMsgIdIndex);
            ulong sourceId = 0; //TODO: fix

            var mws = MessageWriteStream.ThreadInstance;
            mws.Reset(msg.Type, msgId, sourceId, flag, _sendQueue);
            BinSerializer bs = BinSerializer.ThreadInstance;
            bs.Init(mws);
            try
            {
                bs.Write((byte)msg.PayloadType); //不用bs.Serialize(msg)防止box
                msg.WriteObject(bs);
                mws.FinishWrite();
            }
            catch (Exception ex)
            {
                //发生异常，则通知接收端取消挂起的消息
                unsafe
                {
                    SendCancelMessage(mws.CurrentChunk);
                }
                //重新抛出异常
                Log.Warn(ExceptionHelper.GetExceptionDetailInfo(ex));
                throw new MessageSerializationException(MessageSerilizationErrorCode.SerializeFail, ex);
            }
            finally
            {
                bs.Clear();
            }
        }

        private unsafe void SendCancelMessage(MessageChunk* curChunk)
        {
            //注意：标记当前包为取消状态，并且发送至接收端，由接收端取消本包及之前的包
            Log.Debug("Not implemented.");
        }
#endregion

#region ====Debug Methods====
        public string GetDebugInfo()
        {
            var sb = new System.Text.StringBuilder();
            _sendQueue.BuildDebugInfo(sb);
            _receiveQueue.BuildDebugInfo(sb);
            return sb.ToString();
        }
#endregion
    }
}
