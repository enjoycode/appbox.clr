using System;
using appbox.Serialization;

namespace appbox.Server
{
    /// <summary>
    /// 用于子进程向主进程发送监测指标，目前仅用于监测服务调用
    /// </summary>
    public struct MetricRequire : IMessage
    {
        public MessageType Type => MessageType.MetricRequire;

        public PayloadType PayloadType => PayloadType.MetricRequire;

        /// <summary>
        /// 服务全名称 eg: "sys.HelloService.SayHello"
        /// </summary>
        public string Service { get; private set; }

        public double Value { get; private set; }

        public MetricRequire(string method, double value)
        {
            Service = method;
            Value = value;
        }

        public void WriteObject(BinSerializer bs)
        {
            bs.Write(Service);
            bs.Write(Value);
        }

        public void ReadObject(BinSerializer bs)
        {
            Service = bs.ReadString();
            Value = bs.ReadDouble();
        }
    }
}
