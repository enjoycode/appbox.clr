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
            var dic = new Dictionary<string, RouteItem>(32); //key为视图,eg: erp.Customers
            var routes = await Store.ModelStore.LoadViewRoutes();
            if (routes == null || routes.Length == 0)
                return Ok(dic.Values.ToArray());

            var children = new List<RouteItem>(16);
            for (int i = 0; i < routes.Length; i++)
            {
                var view = routes[i].Item1;
                var path = routes[i].Item2;
                if (string.IsNullOrEmpty(path)) //无自定义路径，则肯定没有上级
                {
                    dic.Add(view, new RouteItem { v = view });
                }
                else
                {
                    //判断是否有上级
                    var sepIndex = path.AsSpan().IndexOf(';');
                    if (sepIndex > 0)
                    {
                        var parent = path.AsSpan(0, sepIndex).ToString();
                        var child = path.AsSpan(sepIndex + 1).ToString();
                        if (string.IsNullOrEmpty(child))//无自定义子级路径
                        {
                            var dotIndex = view.AsSpan().IndexOf('.');
                            child = view.AsSpan(dotIndex + 1).ToString();
                        }
                        var item = new RouteItem { Parent = parent, v = view, p = child };
                        dic.Add(view, item);
                        children.Add(item);
                    }
                    else
                    {
                        dic.Add(view, new RouteItem { v = view, p = path });
                    }
                }
            }

            for (int i = 0; i < children.Count; i++)
            {
                if (dic.TryGetValue(children[i].Parent, out RouteItem route))
                {
                    if (route.s == null) route.s = new List<RouteItem>();
                    route.s.Add(children[i]);
                }
                else
                {
                    Log.Warn($"Can't find Parent[{children[i].Parent}] for [{children[i].v}]");
                }
            }

            if (children.Count == 0)
                return Ok(dic.Values);
            else
                return Ok(dic.Values.Where(t => t.Parent == null));
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

    sealed class RouteItem
    {
        [Newtonsoft.Json.JsonIgnore]
        public string Parent { get; set; }

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
