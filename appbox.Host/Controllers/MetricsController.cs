using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Prometheus;

namespace appbox.Controllers
{
    [Route("/metrics")]
    public class MetricsController : Controller
    {
        [HttpGet]
        public void Get()
        {
            //TODO:验证仅允许Prometheus访问
            Response.OnStarting(() => Metrics.DefaultRegistry.CollectAndExportAsTextAsync(Response.Body));
        }
    }
}
