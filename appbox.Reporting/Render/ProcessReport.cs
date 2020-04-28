using System;
using appbox.Reporting.Resources;
using System.Collections;

namespace appbox.Reporting.RDL
{

    ///<summary>
    ///The primary class to "run" a report to the supported output presentation types
    ///</summary>
    public enum OutputPresentationType
    {
        HTML,
        PDF,
        XML,
        ASPHTML,
        Internal,
        MHTML,
        CSV,
        RTF,
        Word,
        ExcelTableOnly,
        Excel2007,
        Excel2003
    }

    [Serializable]
    public class ProcessReport
    {
        readonly Report r;
        readonly IStreamGen _sg;

        public ProcessReport(Report rep, IStreamGen sg)
        {
            if (rep.rl.MaxSeverity > 4)
                throw new Exception(Strings.ProcessReport_Error_ReportHasErrors);

            r = rep;
            _sg = sg;
        }

        public ProcessReport(Report rep)
        {
            if (rep.rl.MaxSeverity > 4)
                throw new Exception(Strings.ProcessReport_Error_ReportHasErrors);

            r = rep;
            _sg = null;
        }

        // Run the report passing the parameter values and the output
        public void Run(IDictionary parms, OutputPresentationType type)
        {
            r.RunGetData(parms);
            r.RunRender(_sg, type);
        }

    }
}
