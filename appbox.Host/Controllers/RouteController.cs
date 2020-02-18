using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace appbox.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ResponseCache(Duration = 120)]
    public sealed class RouteController : ControllerBase
    {
        /// <summary>
        /// 获取路由表
        /// </summary>
        [HttpGet()]
        public async Task<IActionResult> Get()
        {
            //TODO:获取是否移动端后获取不同的路由表
            //TODO:Cache result
            var dic = new Dictionary<string, RouteItem>(32);
            var routes = await Store.ModelStore.LoadViewRoutes();
            if (routes != null && routes.Length > 0)
            {
                for (int i = 0; i < routes.Length; i++)
                {
                    var view = routes[i].Item1;
                    var path = routes[i].Item2;
                    if (string.IsNullOrEmpty(path))
                    {
                        dic.Add(routes[i].Item1.Replace('.', '/'), new RouteItem { v = view });
                    }
                    else
                    {
                        //继续判断是否有上级
                        var sepIndex = path.AsSpan().IndexOf(';');
                        if (sepIndex > 0 && dic.TryGetValue(path.AsSpan(0, sepIndex).ToString(), out RouteItem parent))
                        {
                            if (parent.s == null) parent.s = new List<RouteItem>();
                            parent.s.Add(new RouteItem { v = view, p = path.AsSpan(sepIndex + 1).ToString() });
                        }
                        else
                        {
                            if (!dic.TryAdd(path, new RouteItem { v = view, p = path }))
                            {
                                Log.Warn($"Route[{path}] for view[{view}] has existed");
                            }
                        }
                    }
                }
            }

            return Ok(dic.Values.ToArray());
        }

        /// <summary>
        /// 加载视图运行时代码
        /// </summary>
        [HttpGet("[action]")]
        public async Task<IActionResult> Load(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest();

            var res = await Store.ModelStore.LoadViewAssemblyAsync(id);
            return Content(res);
        }
    }

    struct RouteItem
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        public string v { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        [Newtonsoft.Json.JsonProperty(NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string p { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        [Newtonsoft.Json.JsonProperty(NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public List<RouteItem> s { get; set; }
    }

}
