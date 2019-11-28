@Component
export default class MemUsages extends Vue {
    /** 目标实例IP */
    @Prop({ type: String, default: '10.211.55.3' }) node
    /** 开始时间 */
    @Prop({ type: Date, default: () => { var now = new Date(); return new Date(now.getFullYear(), now.getMonth(), now.getDate()) } }) start
    /** 结束时间 */
    @Prop({ type: Date, default: () => { return new Date() } }) end

    chartOptions = {
        title: { text: 'Mem Usages', x: 'center' },
        grid: { left: 60, right: 30 },
        tooltip: { trigger: 'axis' },
        xAxis: { type: 'time' },
        yAxis: { min: 0, max: 100 },
        series: []
    }

    refresh() {
        sys.Services.MetricService.GetMemUsages(this.node, this.start, this.end)
            .then(res => {
                this.chartOptions.series.splice(0)
                var seria = { type: 'line', name: 'mem', data: res[0], showSymbol: false, areaStyle: {} }
                this.chartOptions.series.push(seria)
            }).catch(err => {
                this.$message(err)
            })
    }

    mounted() {
        this.refresh()
    }
}
