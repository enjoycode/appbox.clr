using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace appbox.Server
{

    public sealed class MessageWriteStream : Stream
    {

        #region ====Static====
        [ThreadStatic]
        static MessageWriteStream threadInstance;

        public static MessageWriteStream ThreadInstance
        {
            get
            {
                if (threadInstance == null)
                    threadInstance = new MessageWriteStream();
                return threadInstance;
            }
        }
        #endregion

        unsafe MessageChunk* _curChunk;
        unsafe byte* _curDataPtr;
        unsafe byte* _maxDataPtr;

        MessageType _msgType;
        int _msgID;
        MessageFlag _msgFlag;
        ulong _sourceId;
        SharedMessageQueue _queue;

        //public unsafe MessageChunk* FirstChunk => _curChunk->First;

        internal unsafe MessageChunk* CurrentChunk => _curChunk;

        #region ====Overrides Properties====

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
        #endregion

        #region ====Ctor & Init====
        private MessageWriteStream() { }

        public unsafe void Reset(MessageType msgType, int msgID, ulong sourceId, MessageFlag msgFlag, SharedMessageQueue queue = null)
        {
            _curChunk = null; //必须设置，否则缓存重用有问题

            _msgType = msgType;
            _msgID = msgID;
            _sourceId = sourceId;
            _msgFlag = msgFlag;
            _queue = queue;

            CreateChunk();

            //第一包设MessageSource
            //_curChunk->MessageSource = srcType;
        }
        #endregion

        #region ====Write Methods====

        public unsafe override void WriteByte(byte value)
        {
            CheckNeedCreate();
            _curDataPtr[0] = value;
            _curDataPtr++;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if ((buffer.Length - offset) < count)
                throw new ArgumentException();

            Write(new ReadOnlySpan<byte>(buffer, offset, count));
        }

        public unsafe override void Write(ReadOnlySpan<byte> buffer)
        {
            CheckNeedCreate();
            var curLeft = (int)(_maxDataPtr - _curDataPtr);
            if (curLeft >= buffer.Length)
            {
                var dest = new Span<byte>(_curDataPtr, buffer.Length);
                buffer.CopyTo(dest);
                _curDataPtr += buffer.Length;
            }
            else
            {
                //先写入当前段内剩余的字节数
                Write(buffer.Slice(0, curLeft));
                //再写入余下的字节数
                Write(buffer.Slice(curLeft, buffer.Length - curLeft));
            }
        }

        public override void Flush()
        {
            //do nothing
        }

        public unsafe void FinishWrite()
        {
            //设置当前消息包的长度
            _curChunk->DataLength = (ushort)(MessageChunk.PayloadDataSize - (_maxDataPtr - _curDataPtr));
            //将当前消息包标为完整消息结束
            _curChunk->Flag |= (byte)MessageFlag.LastChunk;
            //如果在消息队列内直接发送
            if (_queue != null)
            {
                _queue.PostMessageChunk(_curChunk);
                _queue = null; //注意重置
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void CheckNeedCreate()
        {
            if (_curDataPtr == _maxDataPtr)
                CreateChunk();
        }

        private unsafe void CreateChunk()
        {
            var preChunk = _curChunk;

            if (_queue == null)
                _curChunk = (MessageChunk*)ByteBuffer.Pop();
            else
                _curChunk = _queue.GetMessageChunkForWrite();

            //初始化包的消息头
            _curChunk->Type = (byte)_msgType;
            _curChunk->ID = _msgID;
            _curChunk->Flag = (byte)_msgFlag;
            _curChunk->MessageSourceID = _sourceId;
            _curChunk->DataLength = MessageChunk.PayloadDataSize;
            //需要设置消息链表
            _curChunk->Next = null;
            if (preChunk == null)
            {
                _curChunk->First = _curChunk;
                _curChunk->Flag |= (byte)MessageFlag.FirstChunk;
            }
            else
            {
                preChunk->Next = _curChunk;
                _curChunk->First = preChunk->First;
            }
            //设置数据部分位置
            _curDataPtr = MessageChunk.GetDataPtr(_curChunk);
            _maxDataPtr = _curDataPtr + MessageChunk.PayloadDataSize;

            //直接发送preChunk
            if (_queue != null && preChunk != null)
                _queue.PostMessageChunk(preChunk);
        }
        #endregion

        #region ====Not Supported Methods====

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        #endregion
    }

}
