using System;
using System.IO;
using System.IO.Compression;

namespace appbox.Store
{
    /// <summary>
    /// 用于压缩编解码模型的代码
    /// </summary>
    internal static class ModelCodeUtil
    {
        /// <summary>
        /// 用于简单压缩代码
        /// </summary>
        internal static byte[] CompressCode(string code)
        {
            using var ms = new MemoryStream(1024);
            //先写入字符数
            Serialization.VariantHelper.WriteInt32(code.Length, ms);
            using (var cs = new BrotliStream(ms, CompressionMode.Compress, true))
            {
                StringHelper.WriteTo(code, cs.WriteByte);
            }

            return ms.ToArray();
        }

        internal static unsafe string DecompressCode(byte[] data)
        {
            fixed (byte* dataPtr = data)
            {
                return DecompressCode(new IntPtr(dataPtr), data.Length);
            }
        }

        internal unsafe static string DecompressCode(IntPtr dataPtr, int size)
        {
            string res;
            byte* data = (byte*)dataPtr;
            using (var ums = new UnmanagedMemoryStream(data, size))
            {
                //读取字符数
                int chars1 = Serialization.VariantHelper.ReadInt32(ums);
                //再从压缩流中读取
                using var cs = new BrotliStream(ums, CompressionMode.Decompress, true);
                res = StringHelper.ReadFrom(chars1, () => (byte)cs.ReadByte());
            }
            return res;
        }

        internal static byte[] EncodeServiceCode(string sourceCode, string declareCode)
        {
            //头部 1字节压缩标记 + var字符个数 + var字符个数
            using (var ms = new MemoryStream(1024))
            {
                ms.WriteByte(1);

                //先写入字符数
                Serialization.VariantHelper.WriteInt32(sourceCode.Length, ms);
                var len = string.IsNullOrEmpty(declareCode) ? 0 : declareCode.Length;
                Serialization.VariantHelper.WriteInt32(len, ms);
                //再写入压缩的utf8
                using (var cs = new BrotliStream(ms, CompressionMode.Compress, true))
                {
                    StringHelper.WriteTo(sourceCode, cs.WriteByte);
                    if (len > 0)
                        StringHelper.WriteTo(declareCode, cs.WriteByte);
                }

                return ms.ToArray();
            }
        }

        internal static unsafe void DecodeServiceCode(byte[] data, out string sourceCode, out string declareCode)
        {
            fixed (byte* dataPtr = data)
            {
                DecodeServiceCode(new IntPtr(dataPtr), data.Length, out sourceCode, out declareCode);
            }
        }

        internal static unsafe void DecodeServiceCode(IntPtr dataPtr, int size, out string sourceCode, out string declareCode)
        {
            byte* data = (byte*)dataPtr;
            using (var ums = new UnmanagedMemoryStream(data, size))
            {
                ums.ReadByte();

                //读取字符数
                int chars1 = Serialization.VariantHelper.ReadInt32(ums);
                int chars2 = Serialization.VariantHelper.ReadInt32(ums);
                //再从压缩流中读取
                using (var cs = new BrotliStream(ums, CompressionMode.Decompress, true))
                {
                    sourceCode = StringHelper.ReadFrom(chars1, () => (byte)cs.ReadByte());
                    if (chars2 > 0)
                        declareCode = StringHelper.ReadFrom(chars2, () => (byte)cs.ReadByte());
                    else
                        declareCode = null;
                }
            }
        }

        internal static byte[] EncodeViewCode(string templateCode, string scriptCode, string styleCode)
        {
            //头部 1字节压缩标记 + var字符个数 + var字符个数 + var字符个数
            using (var ms = new MemoryStream(1024))
            {
                ms.WriteByte(1);

                //先写入字符数
                Serialization.VariantHelper.WriteInt32(templateCode.Length, ms);
                Serialization.VariantHelper.WriteInt32(scriptCode.Length, ms);
                var len = string.IsNullOrEmpty(styleCode) ? 0 : styleCode.Length;
                Serialization.VariantHelper.WriteInt32(len, ms);
                //再写入压缩的utf8
                using (var cs = new BrotliStream(ms, CompressionMode.Compress, true))
                {
                    StringHelper.WriteTo(templateCode, cs.WriteByte);
                    StringHelper.WriteTo(scriptCode, cs.WriteByte);
                    if (len > 0)
                        StringHelper.WriteTo(styleCode, cs.WriteByte);
                }

                return ms.ToArray();
            }
        }

        internal static void DecodeViewCode(byte[] srcData, out string templateCode, out string scriptCode, out string styleCode)
        {
            unsafe
            {
                fixed (byte* data = srcData)
                {
                    DecodeViewCode(new IntPtr(data), srcData.Length, out templateCode, out scriptCode, out styleCode);
                }
            }
        }

        internal static void DecodeViewCode(IntPtr dataPtr, int size, out string templateCode, out string scriptCode, out string styleCode)
        {
            unsafe
            {
                byte* data = (byte*)dataPtr.ToPointer();
                using (var ums = new UnmanagedMemoryStream(data, size))
                {
                    ums.ReadByte();

                    //读取字符数
                    int chars1 = Serialization.VariantHelper.ReadInt32(ums);
                    int chars2 = Serialization.VariantHelper.ReadInt32(ums);
                    int chars3 = Serialization.VariantHelper.ReadInt32(ums);
                    //再从压缩流中读取
                    using (var cs = new BrotliStream(ums, CompressionMode.Decompress, true))
                    {
                        templateCode = StringHelper.ReadFrom(chars1, () => (byte)cs.ReadByte());
                        scriptCode = StringHelper.ReadFrom(chars2, () => (byte)cs.ReadByte());
                        if (chars3 > 0)
                            styleCode = StringHelper.ReadFrom(chars3, () => (byte)cs.ReadByte());
                        else
                            styleCode = null;
                    }
                }
            }
        }

        internal static byte[] EncodeViewRuntimeCode(string runtimeCode)
        {
            //头部 1字节压缩标记 + var字符个数
            using (var ms = new MemoryStream(1024))
            {
                ms.WriteByte(1);

                //先写入字符数
                Serialization.VariantHelper.WriteInt32(runtimeCode.Length, ms);
                //再写入压缩的utf8
                using (var cs = new BrotliStream(ms, CompressionMode.Compress, true))
                {
                    StringHelper.WriteTo(runtimeCode, cs.WriteByte);
                }

                return ms.ToArray();
            }
        }

        internal static void DecodeViewRuntimeCode(byte[] data, out string runtimeCode)
        {
            unsafe
            {
                fixed (byte* dataPtr = data)
                {
                    DecodeViewRuntimeCode(new IntPtr(dataPtr), data.Length, out runtimeCode);
                }
            }
        }

        internal static void DecodeViewRuntimeCode(IntPtr dataPtr, int size, out string runtimeCode)
        {
            unsafe
            {
                byte* data = (byte*)dataPtr;
                using (var ums = new UnmanagedMemoryStream(data, size))
                {
                    ums.ReadByte();

                    //读取字符数
                    int chars = Serialization.VariantHelper.ReadInt32(ums);
                    //再从压缩流中读取
                    using (var cs = new BrotliStream(ums, CompressionMode.Decompress, true))
                    {
                        runtimeCode = StringHelper.ReadFrom(chars, () => (byte)cs.ReadByte());
                    }
                }
            }
        }
    }
}
