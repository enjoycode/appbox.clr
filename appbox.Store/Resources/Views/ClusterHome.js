@Component({
    components: { GaugeCard: sys.Views.GaugeCard }
})
export default class ClusterHome extends Vue {

    const NodesList = sys.Views.NodesListView
    const PartsList = sys.Views.PartsListView

    gagues = { Nodes: 0, Parts: 0, Sessions: 0 }
    curView = this.NodesList // 当前视图

    loadGauges() {
        $runtime.channel.invoke('sys.ClusterService.GetGauges', []).then(res => {
            this.gagues = res
        }).catch(err => {
            this.$message.error('加载失败:' + err)
        })
    }

    mounted() {
        this.loadGauges()
    }

}
