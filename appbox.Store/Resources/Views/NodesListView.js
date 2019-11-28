@Component
export default class NodesListView extends Vue {
    nodes: [] //节点列表

    loadNodes() {
        $runtime.channel.invoke('sys.ClusterService.GetNodes', []).then(res => {
            this.nodes = res
            this.$forceUpdate()
        }).catch(err => {
            this.$message.error('加载异常:' + err)
        })
    }

    gotoNode(ip) {
        this.$router.push('/ops/v/metrics/' + ip)
    }

    setAsMeta(node) {
        var args = [node.PeerId, node.IPAddress, node.Port]
        $runtime.channel.invoke('sys.ClusterService.SetAsMeta', args).then(res => {
            //TODO:延迟刷新
        }).catch(err => {
            this.$message.error('设置MetaNode异常:' + err)
        })
    }

    promoteRF() {
        $runtime.channel.invoke('sys.ClusterService.PromoteReplFactor', []).then(res => {
            this.$message.success('提升副本因子成功，请稍候检查后台任务是否完成')
        }).catch(err => {
            this.$message.error('提升副本因子异常:' + err)
        })
    }

    mounted() {
        this.loadNodes()
    }
}
