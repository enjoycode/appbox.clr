using appbox.Serialization;

namespace appbox.Server
{
    public struct InvalidModelsCache : IMessage
    {
        public string[] Services { get; private set; }
        public ulong[] Models { get; private set; }

        public MessageType Type => MessageType.InvalidModelsCache;
        public PayloadType PayloadType => PayloadType.InvalidModelsCache;

        public InvalidModelsCache(string[] services, ulong[] models)
        {
            Services = services;
            Models = models;
        }

        public void WriteObject(BinSerializer bs)
        {
            bs.Write(Services);
            bs.Write(Models);
        }

        public void ReadObject(BinSerializer bs)
        {
            Services = bs.ReadStringArray();
            Models = bs.ReadUInt64Array();
        }
    }
}
