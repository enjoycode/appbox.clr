@Component
export default class PartsListView extends Vue {
    groups = [] //分区列表

    loadData() {
        $runtime.channel.invoke('sys.ClusterService.GetParts', []).then(res => {
            if (res && res.length > 0) {
                this.groups = res
            }
        }).catch(err => {
            this.$message.error('加载失败:' + err)
        })
    }

    mounted() {
        this.loadData()
    }
}
