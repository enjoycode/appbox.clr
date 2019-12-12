using System;
using System.Text.Json;

namespace appbox.Serialization
{

    public interface IJsonSerializable
    {

        //暂不使用 string JsonObjID { get; } 

        PayloadType JsonPayloadType { get; }

        void WriteToJson(Utf8JsonWriter writer, WritedObjects objrefs);

        void ReadFromJson(ref Utf8JsonReader reader, ReadedObjects objrefs);

    }

}