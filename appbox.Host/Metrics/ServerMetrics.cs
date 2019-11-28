using System;
using Prometheus;

namespace appbox.Host
{
    /// <summary>
    /// 集中定义监控指标项
    /// </summary>
    static class ServerMetrics
    {
        /// <summary>
        /// 调用服务耗时
        /// </summary>
        internal static readonly Histogram InvokeDuration = Metrics
            .CreateHistogram("invoke_duration_seconds", "The duration of invoke an service method.",
            new HistogramConfiguration
            {
                // 1 ms to 32K ms buckets
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 16),
                LabelNames = new[] { "method" } //TODO:考虑source或from标明调用来源
            });
    }
}
