using System;
using System.IO;

namespace appbox.Server.Channel
{
    sealed class WebSocketFrameReadStream : Stream
    {

        WebSocketFrame current;
        int position = 0;

        public WebSocketFrameReadStream(WebSocketFrame frame)
        {
            current = frame.First;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        { }

        public override int ReadByte()
        {
            int left = current.Length - position;
            if (left >= 1)
            {
                return current.Buffer[position++];
            }
            else
            {
                position = 0;
                if (current.Next == null)
                    return -1;
                current = current.Next;
                return ReadByte();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int left = current.Length - position;
            if (left >= count)
            {
                Buffer.BlockCopy(current.Buffer, position, buffer, offset, count);
                position += count;
                return count;
            }
            else
            {
                Buffer.BlockCopy(current.Buffer, position, buffer, offset, left);
                position = 0;
                if (current.Next == null)
                    return left;

                current = current.Next;
                int readed = Read(buffer, offset + left, count - left);
                return readed + left;
            }
        }

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
    }

}
