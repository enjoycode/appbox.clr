using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using appbox.Models;
using appbox.Runtime;
using appbox.Serialization;
using appbox.Server.Channel;

namespace appbox.Controllers
{
    /// <summary>
    /// 文件上传下载控制器
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public sealed class BlobController : ControllerBase
    {
        /// <summary>
        /// 通过服务验证并处理上传的文件，注意单文件上传，由前端处理多文件上传
        /// </summary>
        /// <param name="validator">验证是否具备上传权限 eg: SERP.ProductService.UploadImage</param>
        /// <param name="processor">处理上传的临时文件 eg: SERP.ProductService.ProcessImage</param>
        [HttpPost("/api/[controller]/{validator}/{processor}/{args}")]
        public async Task<IActionResult> Post(string validator, string processor, string args)
        {
            if (string.IsNullOrEmpty(validator) || string.IsNullOrEmpty(processor))
                return BadRequest("Must asign validator and processor service.");
            if (Request.Form.Files.Count != 1)
                return BadRequest("Only one file one time.");

            var formFile = Request.Form.Files[0];

            //设置当前用户会话
            RuntimeContext.Current.CurrentSession = HttpContext.Session.LoadWebSession();

            //1.调用验证服务
            var iargs = Data.InvokeArgs.From(formFile.FileName, (int)formFile.Length, args);
            try
            {
                await RuntimeContext.Current.InvokeAsync(validator, iargs);
            }
            catch (Exception ex)
            {
                //Log.Warn(ex.StackTrace);
                return BadRequest($"Validate upload file error: {ex.Message}");
            }

            //2.保存为临时文件
            var tempPath = Path.GetTempPath();
            var tempFile = Path.Combine(tempPath, Path.GetRandomFileName());
            try
            {
                using var fs = System.IO.File.OpenWrite(tempFile);
                await formFile.CopyToAsync(fs);
            }
            catch (Exception ex)
            {
                return BadRequest("Save temp file error: " + ex.Message);
            }

            //3.调用处理服务
            object res = null;
            iargs = Data.InvokeArgs.From(formFile.FileName, tempFile, args);
            try
            {
                res = await RuntimeContext.Current.InvokeAsync(processor, iargs);
            }
            catch (Exception ex)
            {
                return BadRequest("Process upload file error: " + ex.Message);
            }
            finally
            {
                System.IO.File.Delete(tempFile);
            }

            return Ok(res);
        }

#if FUTURE

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
            var iargs = Data.InvokeArgs.From(args, formFile.Name);
            string toPath;
            try
            {
                var res = await RuntimeContext.Current.InvokeAsync(validator, iargs);
                toPath = (string)res.ObjectValue;
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
#endif
    }
}