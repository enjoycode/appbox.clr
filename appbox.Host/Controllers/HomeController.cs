using System;
using Microsoft.AspNetCore.Mvc;

namespace appbox.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            //TODO:待测试直接返回目标内容，不使用RedirectResult
            return Redirect("app/index.html");
        }
    }
}
