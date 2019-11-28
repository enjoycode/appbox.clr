using System;
using System.IO;

namespace appbox.Server
{

    public sealed class MessageReadStream : Stream
    {

        #region ====Static====
        [ThreadStatic]
        static MessageReadStream threadInstance;

        public static MessageReadStream ThreadInstance
        {
            get
            {
                if (threadInstance == null)
                    threadInstance = new MessageReadStream();
                return threadInstance;
            }
        }
        #endregion

        unsafe MessageChunk* _curChunk;
        unsafe byte* _curDataPtr;
        unsafe byte* _maxDataPtr;

        #region ====Overrides Properties====

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { /*return _length;*/ throw new NotImplementedException(); }
        }

        public override long Position
        {
            get { /*return this._position;*/ throw new NotImplementedException(); }
            set { throw new NotSupportedException(); }
        }
        #endregion

        #region ====Ctor & Init====
        private MessageReadStream() { }

        public unsafe void Reset(MessageChunk* first)
        {
            //注意：不能加此判断，否则跨进程路由的消息报错
            //if (first->First != first)
            //    throw new ArgumentException(nameof(first));

            _curChunk = first;
            _curDataPtr = MessageChunk.GetDataPtr(_curChunk);
            _maxDataPtr = _curDataPtr + _curChunk->DataLength;

            //todo:考虑是否实现计算总数据大小
        }
        #endregion

        #region ====Read Methods====
        public override void Close()
        {
            //do noting
        }

        public unsafe override int ReadByte()
        {
            CheckNeedMove();
            byte value = _curDataPtr[0];
            _curDataPtr++;
            return value;
        }

        public unsafe override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if ((buffer.Length - offset) < count)
                throw new ArgumentException();

            var dest = new Span<byte>(buffer, offset, count);
            return Read(dest);
        }

        public unsafe override int Read(Span<byte> buffer)
        {
            CheckNeedMove();
            //当前段内剩余的可读字节数
            var curLeft = (int)(_maxDataPtr - _curDataPtr);
            if (curLeft >= buffer.Length)
            {
                var src = new ReadOnlySpan<byte>(_curDataPtr, buffer.Length);
                src.CopyTo(buffer);
                _curDataPtr += buffer.Length;
                return buffer.Length;
            }
            else
            {
                //先读取当前段内剩余的字节数
                Read(buffer.Slice(0, curLeft));
                //再读取余下的字节数
                var res = Read(buffer.Slice(curLeft, buffer.Length - curLeft));
                return res + curLeft;
            }
        }

        public override void Flush()
        {
            //do nothing
        }

        private unsafe void CheckNeedMove()
        {
            if (_curDataPtr == _maxDataPtr)
            {
                do
                {
                    if (_curChunk->Next == null)
                    {
                        //_curChunk->DumpAllTcpInfo(true);
                        throw new IOException("No message chunk to read.");
                    }

                    //移至下一消息段
                    _curChunk = _curChunk->Next;
                    _curDataPtr = MessageChunk.GetDataPtr(_curChunk);
                    _maxDataPtr = _curDataPtr + _curChunk->DataLength;
                } while (_curDataPtr == _maxDataPtr); //TODO:移除while，旧实现会出现空数据包
            }
        }
        #endregion

        #region ====Not Supported Methods====

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
