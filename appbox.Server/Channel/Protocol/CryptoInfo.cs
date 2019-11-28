using System;

namespace appbox.Server
{
    public struct CryptoInfo
    {
        public CryptoType CryptoType;
        public unsafe fixed byte Key1[40]; //todo:确认最大可能值
        public byte Key1Length;
        public unsafe fixed byte Key2[40];
        public byte Key2Length;
    }

    public enum CryptoType : byte
    {
        None,
        TEA
    }
}
