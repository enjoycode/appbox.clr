using System;

namespace appbox.Client.Channel
{
    public struct InvokeResult<T>
    {
        public int I { get; set; }
        public string E { get; set; }
        public T D { get; set; }
    }
}
