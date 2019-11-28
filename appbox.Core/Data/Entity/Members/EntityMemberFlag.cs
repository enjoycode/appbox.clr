using System;

namespace appbox.Data
{
    public struct EntityMemberFlag
    {

        private const byte AllowNull_Flag = 1;
        private const byte HasLoad_Flag = 2;
        private const byte HasValue_Flag = 4;
        private const byte HasChanged_Flag = 8;
        private const byte IsAttached_Flag = 16; //TODO:确认存在必要性，改为IsPartitionKey_Flag
        /// <summary>
        /// 是否DataField成员的引用外键
        /// </summary>
        private const byte IsRefKeyOfDataField_Flag = 32; //TODO:待移除

        internal byte Data;

        internal bool IsRefKeyOfDataField
        {
            get { return (Data & IsRefKeyOfDataField_Flag) == IsRefKeyOfDataField_Flag; }
            set
            {
                if (value)
                    Data |= IsRefKeyOfDataField_Flag;
                else
                    Data = (byte)(Data & ~IsRefKeyOfDataField_Flag);
            }
        }

        internal bool AllowNull
        {
            get { return (Data & AllowNull_Flag) == AllowNull_Flag; }
            set
            {
                if (value)
                    Data |= AllowNull_Flag;
                else
                    Data = (byte)(Data & ~AllowNull_Flag);
            }
        }

        public bool HasValue
        {
            get { return (Data & HasValue_Flag) == HasValue_Flag; }
            set
            {
                if (value)
                    Data |= HasValue_Flag;
                else
                    Data = (byte)(Data & ~HasValue_Flag);
            }
        }

        internal bool HasLoad
        {
            get { return (Data & HasLoad_Flag) == HasLoad_Flag; }
            set
            {
                if (value)
                    Data |= HasLoad_Flag;
                else
                    Data = (byte)(Data & ~HasLoad_Flag);
            }
        }

        public bool HasChanged
        {
            get { return (Data & HasChanged_Flag) == HasChanged_Flag; }
            set
            {
                if (value)
                    Data |= HasChanged_Flag;
                else
                    Data = (byte)(Data & ~HasChanged_Flag);
            }
        }

        internal bool IsAttached
        {
            get { return (Data & IsAttached_Flag) == IsAttached_Flag; }
            set
            {
                if (value)
                    Data |= IsAttached_Flag;
                else
                    Data = (byte)(Data & ~IsAttached_Flag);
            }
        }

    }
}
