using System;

namespace appbox.Server
{
    public interface IScanResponse : IDisposable
    {
        uint Skipped { get; }
        int Length { get; }

        void ForEachRow(Action<IntPtr, int, IntPtr, int> action);

        //不要用以下注释部分，因为RemoveScanResponse实现只能逐行执行
        //void GetKVAtIndex(int index, out IntPtr kp, out int ks, out IntPtr vp, out int vs);
    }
}
