@Component
export default class OpsHome extends Vue {
    menus = [
        { title: '集群管理', icon: 'fas fa-server', index: '/ops/v/cluster' },
        { title: '服务监控', icon: 'fas fa-cubes', index: '/ops/v/services' },
        { title: '告警管理', icon: 'fas fa-exclamation-circle', index: '/ops/v/metrics' },
        { title: '日志分析', icon: 'fas fa-hdd', index: '/ops/v/metrics' },
        { title: '会话分析', icon: 'fas fa-user-friends', index: '/ops/v/metrics' },
    ]
}
