using System;
using System.Text.Json;
using System.Threading.Tasks;
using appbox.Design;
using appbox.Models;
using appbox.Runtime;
using appbox.Serialization;
using appbox.Server.Channel;
using Microsoft.AspNetCore.Mvc;
// using appbox.Reporting;
using System.Collections;
using System.Data;

namespace appbox.Controllers
{
    /// <summary>
    /// 设计时专用，目前用于导入导出模型包
    /// </summary>
    [Route("api/[controller]/[action]")]
    public class DesignController : ControllerBase
    {
        /// <summary>
        /// 导出应用模型包
        /// </summary>
        /// <param name="appName">eg: erp</param>
        [HttpGet("{appName}")] //Get /api/design/export/erp
        public async Task Export(string appName)
        {
            //设置当前用户会话
            RuntimeContext.Current.CurrentSession = HttpContext.Session.LoadWebSession();
            //调用设计时服务生成导入包
            //try
            //{
            var appPkg = await AppStoreService.Export(appName);
            HttpContext.Response.Headers.Add("Content-Disposition", $"attachment; filename={appName}.apk");
            HttpContext.Response.ContentType = "application/octet-stream";
            var bs = new BinSerializer(HttpContext.Response.Body);
            bs.Serialize(appPkg);
            //}
            //catch (Exception ex)
            //{
            //    Log.Warn($"Export error: {ex.Message}");
            //}
        }

        /// <summary>
        /// 导入应用模型包
        /// </summary>
        [HttpPost()]
        public async Task<IActionResult> Import()
        {
            if (Request.Form.Files.Count != 1)
                return BadRequest("Please upload one apk file");

            //设置当前用户会话
            RuntimeContext.Current.CurrentSession = HttpContext.Session.LoadWebSession();
            //TODO:结合前端修改上传方式
            var formFile = Request.Form.Files[0];
            using var ss = formFile.OpenReadStream();
            //反序列化
            var bs = new BinSerializer(ss);
            var appPkg = (Design.AppPackage)bs.Deserialize();
            await Design.AppStoreService.Import(appPkg);
            return Ok();
        }

#if APPBOXPRO
        private static Report Parse(string json)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var jr = new Utf8JsonReader(bytes.AsSpan());
            var report = Reporting.Serialization.JsonSerializer.Desialize(ref jr);
            return report;
        }
#endif

        /// <summary>
        /// 设计时生成报表
        /// </summary>
        /// <param name="reportModelId"></param>
        /// <returns>pdf file for report</returns>
        [HttpGet("{reportId}")] //Get /api/design/report/1234567
        public async Task Report(string reportId)
        {
#if !APPBOXPRO
            throw new NotSupportedException();
#else
            //设置当前用户会话
            RuntimeContext.Current.CurrentSession = HttpContext.Session.LoadWebSession();
            //判断权限
            if (!(RuntimeContext.Current.CurrentSession is IDeveloperSession developerSession))
                throw new Exception("Must login as a Developer");
            var desighHub = developerSession.GetDesignHub();
            if (desighHub == null)
                throw new Exception("Cannot get DesignContext");
            //TODO: 以下代码与OpenReportModel重复，待优化
            var modelNode = desighHub.DesignTree.FindModelNode(ModelType.Report, ulong.Parse(reportId));
            if (modelNode == null)
                throw new Exception($"Cannot find report model: {reportId}");

            //TODO:直接加载字节流
            string json = null;
            if (modelNode.IsCheckoutByMe)
                json = await StagedService.LoadReportCodeAsync(modelNode.Model.Id);
            if (json == null)
                json = await Store.ModelStore.LoadReportCodeAsync(modelNode.Model.Id);

            var report = Parse(json);

            // //根据推/拉模式获取或生成预览数据源, TODO:暂全部生成测试数据
            foreach (var ds in report.DataSources)
            {
                var ods = (ObjectDataSource)ds;
                ods.DataSource = MakePreviewData(ods, 10);
            }

            //输出为pdf
            HttpContext.Response.ContentType = "application/pdf";
            var rs = new InstanceReportSource { ReportDocument = report };
            var deviceInfo = new Hashtable()
            {
                { "OutputFormat", "emf" },
                { "ProcessItemActions", false },
                { "WriteClientAction", false }
            };
            var renderContext = new Reporting.Processing.RenderingContext()
            {
                { "ReportDocumentState", null }
            };
            var processingReports = new Reporting.Processing.ReportProcessor()
                .ProcessReport(rs, deviceInfo, renderContext);
            var res = Reporting.Processing.ReportProcessor.RenderReport("IMAGE", processingReports,
                        deviceInfo, renderContext);
            await HttpContext.Response.Body.WriteAsync(res.DocumentBytes);
#endif
        }

#if APPBOXPRO
        private static DataTable MakePreviewData(ObjectDataSource ds, int rows)
        {
            rows = Math.Min(rows, 128); //暂最多128行

            var dt = new DataTable();
            foreach (var col in ds.Fields)
            {
                Type fieldType = col.Type switch
                {
                    SimpleType.Boolean => typeof(bool),
                    SimpleType.DateTime => typeof(DateTime),
                    SimpleType.Decimal => typeof(Decimal),
                    SimpleType.Float => typeof(double),
                    SimpleType.Integer => typeof(long),
                    _ => typeof(string),
                };
                dt.Columns.Add(col.Name, fieldType);
            }

            //TODO:如果有DataRegion绑定且有分组则模拟分组，暂简单模拟分组

            for (int i = 0; i < rows; i++)
            {
                var row = dt.NewRow();
                int j = 0;
                int no;
                foreach (var col in ds.Fields)
                {
                    no = i % (j + 3);
                    switch (col.Type)
                    {
                        case SimpleType.Boolean:
                            row[j] = i % 2 == 0; break;
                        case SimpleType.String:
                            row[j] = $"{col.Name}{no}";
                            break;
                        case SimpleType.Decimal:
                        case SimpleType.Integer:
                        case SimpleType.Float:
                            row[j] = i; break;
                        case SimpleType.DateTime:
                            row[j] = new DateTime(1977, 3, no + 1); break;
                    }
                    j++;
                }
                dt.Rows.Add(row);
            }

            return dt;
        }
#endif

        /// <summary>
        /// 设计时生成条码图片
        /// </summary>
        /// <param name="type"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        // [HttpGet("{type}/{code}/{width}/{height}/{scale}")] //Get /api/design/barcode/BarCode128/123456/200/100/1
        // public async Task Barcode(string type, string code, int width, int height, float scale)
        // {
        //     //设置当前用户会话
        //     RuntimeContext.Current.CurrentSession = HttpContext.Session.LoadWebSession();
        //     //判断权限
        //     if (!(RuntimeContext.Current.CurrentSession is IDeveloperSession))
        //         throw new Exception("Must login as a Developer");

        //     var bw = new ZXing.SkiaSharp.BarcodeWriter(); //TODO:优化单例ZXing.BarCodeRender
        //     bw.Format = (ZXing.BarcodeFormat)Enum.Parse(typeof(ZXing.BarcodeFormat), type);
        //     bw.Options.Height = (int)(height * scale);
        //     bw.Options.Width = (int)(width * scale);
        //     var skbmp = bw.Write(code);

        //     using var bmp = new Drawing.Bitmap(skbmp);
        //     using var ms = new System.IO.MemoryStream(1024);
        //     bmp.Save(ms, Drawing.ImageFormat.Jpeg, 90);
        //     ms.Position = 0;

        //     HttpContext.Response.ContentType = "image/jpg";
        //     await ms.CopyToAsync(HttpContext.Response.Body);
        //     //bmp.Save(HttpContext.Response.Body, Drawing.ImageFormat.Png); //不支持同步直接写
        // }
    }
}
