using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using appbox.Caching;
using appbox.Design;
using appbox.Models;
using appbox.Runtime;
using appbox.Serialization;
using appbox.Server.Channel;
using Microsoft.AspNetCore.Mvc;

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
            var appPkg = await Design.AppStoreService.Export(appName);
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

        /// <summary>
        /// 设计时生成报表
        /// </summary>
        /// <param name="reportModelId"></param>
        /// <returns>pdf file for report</returns>
        [HttpGet("{reportId}")] //Get /api/design/report/1234567
        public async Task Report(string reportId)
        {
            throw new NotImplementedException();
            // //设置当前用户会话
            // RuntimeContext.Current.CurrentSession = HttpContext.Session.LoadWebSession();
            // //判断权限
            // if (!(RuntimeContext.Current.CurrentSession is IDeveloperSession developerSession))
            //     throw new Exception("Must login as a Developer");
            // var desighHub = developerSession.GetDesignHub();
            // if (desighHub == null)
            //     throw new Exception("Cannot get DesignContext");
            // //TODO: 以下代码与OpenReportModel重复，待优化
            // var modelNode = desighHub.DesignTree.FindModelNode(ModelType.Report, ulong.Parse(reportId));
            // if (modelNode == null)
            //     throw new Exception($"Cannot find report model: {reportId}");

            // string xml = null;
            // if (modelNode.IsCheckoutByMe)
            // {
            //     xml = await StagedService.LoadReportCodeAsync(modelNode.Model.Id);
            // }
            // if (xml == null)
            // {
            //     xml = await Store.ModelStore.LoadReportCodeAsync(modelNode.Model.Id);
            // }

            // var rdlParser = new Reporting.RDL.RDLParser(xml);
            // var report = rdlParser.Parse();
            // if (report.ErrorMaxSeverity > 0)
            // {
            //     var sb = StringBuilderCache.Acquire();
            //     sb.AppendLine("Report has some errors:");
            //     foreach (string emsg in report.ErrorItems)
            //     {
            //         sb.AppendLine(emsg);
            //     }
            //     Log.Warn(StringBuilderCache.GetStringAndRelease(sb));

            //     int severity = report.ErrorMaxSeverity;
            //     report.ErrorReset();
            //     if (severity > 4)
            //         throw new Exception("Parse Report error.");
            // }

            // //根据推/拉模式获取或生成预览数据源, TODO:暂全部生成测试数据
            // foreach (var ds in report.DataSets)
            // {
            //     ((Reporting.RDL.DataSet)ds).MakePreviewData(10);
            // }
            // report.RunGetData(null); //必须在手工设定数据源后执行

            // //输出为pdf, TODO:***暂简单写临时文件
            // HttpContext.Response.ContentType = "application/pdf";
            // var tempfile = System.IO.Path.GetTempFileName();
            // try
            // {
            //     using (var sg = new Reporting.RDL.OneFileStreamGen(tempfile, true))
            //     {
            //         report.RunRender(sg, Reporting.RDL.OutputPresentationType.PDF);
            //     }
            //     Log.Debug($"Save report to tempfile: {tempfile}");
            //     using var fs = System.IO.File.OpenRead(tempfile);
            //     await fs.CopyToAsync(HttpContext.Response.Body);
            // }
            // finally
            // {
            //     System.IO.File.Delete(tempfile);
            //     Log.Debug($"Clean report tempfile: {tempfile}");
            // }
        }

        /// <summary>
        /// 设计时生成条码图片
        /// </summary>
        /// <param name="type"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        [HttpGet("{type}/{code}/{width}/{height}/{scale}")] //Get /api/design/barcode/BarCode128/123456/200/100/1
        public async Task Barcode(string type, string code, int width, int height, float scale)
        {
            throw new NotImplementedException();
            //设置当前用户会话
            // RuntimeContext.Current.CurrentSession = HttpContext.Session.LoadWebSession();
            // //判断权限
            // if (!(RuntimeContext.Current.CurrentSession is IDeveloperSession))
            //     throw new Exception("Must login as a Developer");

            // var target = Reporting.RDL.RdlEngineConfig.CreateCustomReportItem(type);
            // var props = new Dictionary<string, object>
            // {
            //     {"Code", code }
            // };
            // target.SetProperties(props);
            // var skbmp = target.DrawImage((int)(width * scale), (int)(height * scale));
            // using var bmp = new Drawing.Bitmap(skbmp);
            // using var ms = new System.IO.MemoryStream(1024);
            // bmp.Save(ms, Drawing.ImageFormat.Jpeg, 90);
            // ms.Position = 0;

            // HttpContext.Response.ContentType = "image/jpg";
            // await ms.CopyToAsync(HttpContext.Response.Body);
            // //bmp.Save(HttpContext.Response.Body, Drawing.ImageFormat.Png); //不支持同步直接写
        }
    }
}
