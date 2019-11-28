using System;
using Newtonsoft.Json;

namespace appbox.Serialization
{

    public interface IJsonSerializable
    {

        //暂不使用 string JsonObjID { get; } 

        PayloadType JsonPayloadType { get; }

        void WriteToJson(JsonTextWriter writer, WritedObjects objrefs);

        void ReadFromJson(JsonTextReader reader, ReadedObjects objrefs);

    }

}