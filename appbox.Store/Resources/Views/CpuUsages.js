@Component
export default class CpuUsages extends Vue {
    /** 目标实例IP */
    @Prop({ type: String, default: '10.211.55.3' }) node
    /** 开始时间 */
    @Prop({ type: Date, default: () => { var now = new Date(); return new Date(now.getFullYear(), now.getMonth(), now.getDate()) } }) start
    /** 结束时间 */
    @Prop({ type: Date, default: () => { return new Date() } }) end

    chartOptions = {
        title: { text: 'Cpu Usages', x: 'center' },
        grid: { left: 60, right: 30 },
        tooltip: { trigger: 'axis' },
        xAxis: { type: 'time' },
        yAxis: { min: 0, max: 100 },
        series: []
    }

    refresh() {
        sys.Services.MetricService.GetCpuUsages(this.node, this.start, this.end)
            .then(res => {
                this.chartOptions.series.splice(0)
                for (var i = 0; i < res.length; ++i) {
                    var seria = { type: 'line', name: 'cpu' + i, data: res[i], showSymbol: false }
                    this.chartOptions.series.push(seria)
                }
            }).catch(err => {
                this.$message(err)
            })
    }

    mounted() {
        this.refresh()
    }
}
