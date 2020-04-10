using System;
using System.Data;
using Xunit;

namespace appbox.Reporting.Tests
{
    public class GenReportTest
    {
        [Fact]
        public void Test1()
        {
            //var rdl = new RDL.ReportDefn();
            //var report = new RDL.Report();
            var rdlXml = Resources.LoadStringResource("Resources.TestReport.rdl");
            var rdlParser = new RDL.RDLParser(rdlXml);
            var report = rdlParser.Parse();
            Assert.True(report.ErrorMaxSeverity <= 4);
            //if (report.ErrorMaxSeverity > 0)
            //{
            //    foreach (string emsg in report.ErrorItems)
            //    {
            //        Console.WriteLine(emsg);
            //    }

            //    int severity = report.ErrorMaxSeverity;
            //    report.ErrorReset();
            //    if (severity > 4)
            //        report = null;
            //}

            //获取数据源
            var dt = new DataTable();
            dt.Columns.Add("CategoryID", typeof(long));
            dt.Columns.Add("CategoryName", typeof(string));
            dt.Columns.Add("Description", typeof(string));
            for (int i = 0; i < 20; i++)
            {
                dt.Rows.Add(i, "Name", "Description");
            }
            report.DataSets["Data"].SetData(dt);
            report.RunGetData(null); //必须在手工设定数据源后执行

            //输出为pdf
            var outFile = System.IO.Path.Combine(AppContext.BaseDirectory, "A_TestReport.pdf");
            using var sg = new RDL.OneFileStreamGen(outFile, true);
            report.RunRender(sg, RDL.OutputPresentationType.PDF);
        }
    }
}
