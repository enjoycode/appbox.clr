namespace appbox.Serialization
{
    public interface IBinSerializable
    {

        void WriteObject(BinSerializer writer);

        void ReadObject(BinSerializer reader);

    }

}

