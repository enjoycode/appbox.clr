@Component({
    components: {
        CpuUsages: sys.Views.CpuUsages,
        MemUsages: sys.Views.MemUsages,
        NetTraffic: sys.Views.NetTraffic,
        DiskIo: sys.Views.DiskIO
    }
})
export default class NodeMetrics extends Vue {
    @Prop({ type: String, default: '10.211.55.3' }) node

    times = [new Date(2019, 1, 1), new Date(2019, 1, 2)]
    timesOptions = {
        shortcuts: [
            {
                text: 'Last 1 hours',
                onClick(picker) {
                    const end = new Date()
                    const start = new Date()
                    start.setTime(start.getTime() - 3600 * 1000)
                    picker.$emit('pick', [start, end])
                }
            },
            {
                text: 'Last 8 hours',
                onClick(picker) {
                    const end = new Date()
                    const start = new Date()
                    start.setTime(start.getTime() - 3600 * 1000 * 8)
                    picker.$emit('pick', [start, end])
                }
            }
        ]
    }

    onTimesChange() {
        this.$refs.cpu.refresh()
        this.$refs.mem.refresh()
        this.$refs.net.refresh()
        this.$refs.disk.refresh()
    }

    goback() {
        this.$router.go(-1)
    }

    created() {
        const end = new Date()
        const start = new Date()
        start.setTime(start.getTime() - 3600 * 1000)
        this.$set(this.times, 0, start)
        this.$set(this.times, 1, end)
    }

}
