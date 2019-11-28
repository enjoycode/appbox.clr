using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using appbox.Models;
using appbox.Server.Channel;
using appbox.Runtime;
using System.IO;

namespace appbox.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public sealed class BlobController : ControllerBase
    {

        private static int fileIndex;
        /// <summary>
        /// 仅测试用
        /// </summary>
        [HttpGet("Upload")]
        public async Task<ActionResult<string>> Upload()
        {
            int index = System.Threading.Interlocked.Increment(ref fileIndex);
            string fileName = $"/pub/a{index}.png";

            using (var fs = System.IO.File.OpenRead("TestUpload.png"))
            {
                await Store.BlobStore.UploadAsync(1, fs, fileName);
            }

            return "Upload done.";
        }

        [HttpGet("list/{**path}")]
        public async Task<IActionResult> List(string path)
        {
            if (string.IsNullOrEmpty(path))
                path = "/";
            else
                path = "/" + path;
            Log.Debug($"列出目录: {path}");

            var res = await Store.BlobStore.ListAsync(1, path);
            return Ok(res);
        }

        /// <summary>
        /// 仅用于设计时由开发人员上传文件
        /// </summary>
        [HttpPost("dev/{app}/{**path}")] //Post /blob/dev/sys/pub/imgs
        public async Task<IActionResult> UploadByDev(string app, string path)
        {
            if (Request.Form.Files.Count != 1)
                return BadRequest("每次只能上传一个文件");
            var formFile = Request.Form.Files[0];
            if (string.IsNullOrEmpty(formFile.FileName))
                return BadRequest("必须指定文件名称");

            //判断是否开发人员(考虑专用Permission项对应)
            var developerSession = HttpContext.Session.LoadWebSession() as Design.IDeveloperSession;
            if (developerSession == null)
                return Forbid();

            var application = await RuntimeContext.Current.GetApplicationModelAsync(app);
            string toPath;
            if (string.IsNullOrEmpty(path))
                toPath = Path.Combine("/", formFile.FileName);
            else
                toPath = Path.Combine("/", path, formFile.FileName);

            //上传至BlobStore
            using (var fs = formFile.OpenReadStream())
            {
                await Store.BlobStore.UploadAsync(application.StoreId, fs, toPath);
            }
            return Ok();
        }

        /// <summary>
        /// 用于直接上传文件至BlobStore
        /// </summary>
        /// <param name="validator">上传权限验证服务,返回映射到存储的路径 eg:sys.XXXService.UploadProdImg</param>
        /// <param name="args">用于验证服务的附加参数</param>
        [HttpPost("{validator}/{**args}")] //Post /blob/sys.DesignService.ValidatorUpload/可选参数
        public async Task<IActionResult> Upload(string validator, string args)
        {
            if (string.IsNullOrEmpty(validator))
                return BadRequest("必须指定验证服务");
            if (Request.Form.Files.Count != 1)
                return BadRequest("每次只能上传一个文件");
            var sr = validator.Split('.');
            if (sr.Length != 3)
                return BadRequest("验证服务格式错误");

            ApplicationModel app;
            try
            {
                app = await RuntimeContext.Current.GetApplicationModelAsync(sr[0]);
            }
            catch (Exception)
            {
                return BadRequest("Application not exists.");
            }

            var formFile = Request.Form.Files[0];

            //设置当前会话
            RuntimeContext.Current.CurrentSession = HttpContext.Session.LoadWebSession();

            //调用验证服务
            var iargs = new Data.InvokeArgs();
            iargs.Add(args);
            iargs.Add(formFile.Name);
            string toPath = null;
            try
            {
                var res = await RuntimeContext.Current.InvokeAsync(validator, iargs);
                toPath = (string)res;
            }
            catch (Exception ex)
            {
                Log.Debug($"验证文件上传失败 Validator:{validator} Args: {args}");
                return BadRequest($"验证上传失败: {ex.Message}");
            }

            //上传至BlobStore
            using (var fs = formFile.OpenReadStream())
            {
                await Store.BlobStore.UploadAsync(app.StoreId, fs, toPath);
            }
            return Ok();
        }

        /// <summary>
        /// 用于前端获取指定App's BlobStore公开目录[/pub/{path}]内文件
        /// </summary>
        [HttpGet("{app}/{**path}")] //Get /blob/sys/imgs/aa.jpg
        public void View(string app, string path)
        {
            Response.OnStarting(async () =>
            {
                byte appStoreId = 0;
                bool hasError = false;
                try
                {
                    var a = await RuntimeContext.Current.GetApplicationModelAsync(app);
                    appStoreId = a.StoreId;
                }
                catch (Exception ex)
                {
                    Log.Debug($"根据名称获取ApplicationModel错识: {ex.Message}");
                    Response.StatusCode = 404;
                    hasError = true;
                }

                if (!hasError)
                {
                    //TODO:考虑BlobApi先获取文件元数据,先判断是否存在
                    Response.ContentType = Host.FileContentType.GetMimeType(System.IO.Path.GetExtension(path));
                    string file = $"/pub/{path}";
                    await Store.BlobStore.DownloadAsync(appStoreId, Response.Body, file);
                }
            });
        }

    }
}
