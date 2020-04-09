using System;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// A report object name.   CLS comliant identifier.
    ///</summary>
    [Serializable]
    internal class Name
    {
        internal string Nm { get; set; }

        internal Name(string name)
        {
            Nm = name;
        }

        public override string ToString()
        {
            return Nm;
        }
    }
}
