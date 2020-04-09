using System;

namespace appbox.Reporting.RDL
{
    ///<summary>
    /// ChartElementOutput parsing.
    ///</summary>
    internal enum ChartElementOutputEnum
    {
        Output,
        NoOutput
    }

    internal class ChartElementOutput
    {
        static internal ChartElementOutputEnum GetStyle(string s, ReportLog rl)
        {
            ChartElementOutputEnum ceo;

            switch (s)
            {
                case "Output":
                    ceo = ChartElementOutputEnum.Output;
                    break;
                case "NoOutput":
                    ceo = ChartElementOutputEnum.NoOutput;
                    break;
                default:
                    rl.LogError(4, "Unknown ChartElementOutput '" + s + "'.  Output assumed.");
                    ceo = ChartElementOutputEnum.Output;
                    break;
            }
            return ceo;
        }
    }


}
