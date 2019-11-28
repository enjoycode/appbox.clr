@Component
export default class OpsLogin extends Vue {
    user = ''
    pwd = ''
    loading = false

    onLogin() {
        this.loading = true
        $runtime.channel.login(this.user, this.pwd).then(res => {
            this.$router.push({ path: '/ops/v/cluster' })
        }).catch(err => {
            this.loading = false
            this.$message.error('Login failed: ' + err)
        })
    }

    mounted() {
        var opsRoutes = [
            {
                path: '/ops/v', component: sys.Views.OpsHome,
                children: [
                    { path: 'cluster', component: sys.Views.ClusterHome },
                    { path: 'metrics/:node', component: sys.Views.NodeMetrics, props: true },
                    { path: 'services', component: sys.Views.InvokeMetrics }
                ]
            }
        ]
        this.$router.addRoutes(opsRoutes)
    }
}