@Component
export default class InvokeMetrics extends Vue {
    times = [new Date(2019, 1, 1), new Date(2019, 1, 2)]

    topcount = []
    toptimes = []

    chartOpt1 = {
        tooltip: {
            trigger: 'item',
            formatter: "{a} <br/>{b} : {c} ({d}%)"
        },
        series: [
            {
                name: '调用次数',
                type: 'pie',
                radius: '56%',
                data: [],
                itemStyle: {
                    emphasis: {
                        shadowBlur: 10,
                        shadowOffsetX: 0,
                        shadowColor: 'rgba(0, 0, 0, 0.5)'
                    }
                }
            }
        ]
    }

    chartOpt2 = {
        tooltip: {
            trigger: 'item',
            formatter: "{a} <br/>{b} : {c} ({d}%)"
        },
        series: [
            {
                name: '调用耗时',
                type: 'pie',
                radius: '56%',
                data: [],
                itemStyle: {
                    emphasis: {
                        shadowBlur: 10,
                        shadowOffsetX: 0,
                        shadowColor: 'rgba(0, 0, 0, 0.5)'
                    }
                }
            }
        ]
    }

    refresh() {
        sys.Services.MetricService.GetTopInvoke(true, this.times[0], this.times[1], 10)
            .then(res => {
                var obj = JSON.parse(res)
                if (obj.status != 'success') {
                    this.$message.error('查询调用次数排名失败')
                } else {
                    this.topcount = obj.data.result
                    this.chartOpt1.series[0].data = this.convertToChartData(this.topcount)
                }
            }).catch(err => {
                this.$message.error(err)
            })
        sys.Services.MetricService.GetTopInvoke(false, this.times[0], this.times[1], 10)
            .then(res => {
                var obj = JSON.parse(res)
                if (obj.status != 'success') {
                    this.$message.error('查询调用耗时排名失败')
                } else {
                    this.toptimes = obj.data.result
                    this.chartOpt2.series[0].data = this.convertToChartData(this.toptimes)
                }
            }).catch(err => {
                this.$message.error(err)
            })
    }

    // 将时序数据转换为图表数据格式
    convertToChartData(source) {
        var res = []
        for (var i = 0; i < source.length; ++i) {
            res.push({ name: source[i].metric.method, value: source[i].value[1] })
        }
        return res
    }

    onTimesChange() {
        this.refresh()
    }

    mounted() {
        this.refresh()
    }

    created() {
        const end = new Date()
        const start = new Date()
        start.setTime(start.getTime() - 3600 * 1000)
        this.$set(this.times, 0, start)
        this.$set(this.times, 1, end)
    }
}