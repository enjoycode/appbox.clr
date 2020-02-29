using System;
using System.Threading.Tasks;
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
    }
}
