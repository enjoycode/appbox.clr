@Component
export default class DiskIO extends Vue {
    /** 目标实例IP */
    @Prop({ type: String, default: '10.211.55.3' }) node
    /** 开始时间 */
    @Prop({ type: Date, default: () => { var now = new Date(); return new Date(now.getFullYear(), now.getMonth(), now.getDate()) } }) start
    /** 结束时间 */
    @Prop({ type: Date, default: () => { return new Date() } }) end

    chartOptions = {
        title: { text: 'Disk IO', x: 'center' },
        grid: { left: 60, right: 30 },
        tooltip: { trigger: 'axis' },
        xAxis: { type: 'time' },
        yAxis: {
            min: 0,
            minInterval: 512,
            axisLabel: {
                formatter: function (value, index) {
                    if (value >= 1024 * 1024 * 1024) {
                        return (value / (1024 * 1024 * 1024)).toFixed(1) + 'GB'
                    } else if (value >= 1024 * 1024) {
                        return (value / (1024 * 1024)).toFixed(1) + 'MB'
                    } else if (value >= 1024) {
                        return (value / 1024).toFixed() + 'KB'
                    } else {
                        return value + 'B'
                    }
                }
            }
        },
        series: []
    }

    refresh() {
        sys.Services.MetricService.GetDiskIO(this.node, this.start, this.end)
            .then(res => {
                this.chartOptions.series.splice(0)
                var seria1 = { type: 'line', name: 'read', data: res[0], showSymbol: false }
                this.chartOptions.series.push(seria1)
                var seria2 = { type: 'line', name: 'write', data: res[1], showSymbol: false }
                this.chartOptions.series.push(seria2)
            }).catch(err => {
                this.$message(err)
            })
    }

    mounted() {
        this.refresh()
    }
}
