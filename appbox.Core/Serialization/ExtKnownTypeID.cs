namespace appbox.Serialization
{
    public struct ExtKnownTypeID
    {
        public uint AssemblyID;
        public uint TypeID;

        public override int GetHashCode()
        {
            ulong temp = ((ulong)AssemblyID) << 32 | TypeID;
            return temp.GetHashCode();
        }
    }
}

