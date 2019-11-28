using System;
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
        public async Task<IActionResult> Get() //todo:考虑获取是否移动端后获取不同的路由表
        {
            //todo: cache it

            var list = new List<object>();
            var routes = await Store.ModelStore.LoadViewRoutes();
            if (routes != null && routes.Length > 0)
            {
                for (int i = 0; i < routes.Length; i++)
                {
                    if (string.IsNullOrEmpty(routes[i].Item2))
                        list.Add(new { v = routes[i].Item1 });
                    else
                        list.Add(new { p = routes[i].Item2, v = routes[i].Item1 });
                }
            }
            
            return Ok(list);
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
}
