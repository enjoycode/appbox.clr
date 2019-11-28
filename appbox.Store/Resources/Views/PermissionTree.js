@Component
export default class PermissionTree extends Vue {
    @Prop({ default: null })
    orgunit
    
    nodes = []
    checking = false //用于标识是否正在重新设置权限打勾
    treeProps = { label: 'Name', children: 'Childs' }

    @Watch('orgunit')
    onOrgUnitChanged(val) {
        if (this.orgunit) {
            this.checking = true
            // console.log('checking -> true')
            for (var i = 0; i < this.nodes.length; i++) {
                this.loopCheckNodes(this.nodes[i])
            }
            this.$nextTick(function () { //注意：必须nextTick
                // console.log('checking -> false')
                this.checking = false
            })
        }
    }

    // 组织单元改变时循环勾选具备权限的节点
    loopCheckNodes(data) {
        if (data.OrgUnits) { //表示权限节点
            var cur = this.orgunit
            while (cur) {
                for (var i = 0; i < data.OrgUnits.length; i++) {
                    if (cur.Id === data.OrgUnits[i]) {
                        this.$refs.tree.setChecked(data.Id, true)
                        return //无需再继续
                    }
                }
                cur = cur.Parent
            }
            this.$refs.tree.setChecked(data.Id, false) //否则清除选中
        } else if (data.Childs) {
            for (var i = 0; i < data.Childs.length; i++) {
                this.loopCheckNodes(data.Childs[i])
            }
        }
    }
    // 勾选改变
    onCheckChanged(data, checked) {
        if (this.checking || !this.orgunit || !data.OrgUnits) {
            return
        }
        if (checked) { //false -> true
            //先移除当前组织所有已经具备相同权限的下级
            this.loopRemoveChildPermission(this.orgunit, data)
            //再添加当前ouid至当前权限, 注意：原DPS实现自动向上提升权限
            data.OrgUnits.push(this.orgunit.Id)
        } else { //true -> false
            //todo: 如果是sys.Admin，则检查是否至少具备一个ouid,否则报错
            this.loopDemotion(this.orgunit, data)
        }
        //调用服务更新权限
        this.savePermission(data)
    }
    //false -> true时移除所有下级的权限，即所有下级的权限变为继承的
    loopRemoveChildPermission(ou, pm) {
        var childs = ou.Childs
        if (childs) {
            for (var i = 0; i < childs.length; i++) {
                let index = pm.OrgUnits.indexOf(childs[i].Id)
                if (index != -1) {
                    pm.OrgUnits.splice(index, 1)
                }
                //注意：必须往下递归移除
                this.loopRemoveChildPermission(childs[i], pm)
            }
        }
    }
    //true -> false时向上降级：先判断是否继承下来的权限，是则降级上级组织单元的权限至其他平级
    loopDemotion(ou, pm) {
        //先判断当前组织单元的权限是否继承而来
        let ouIndex = pm.OrgUnits.indexOf(ou.Id)
        if (ouIndex != -1) { //非继承的权限直接删除
            pm.OrgUnits.splice(ouIndex, 1)
        } else { //继承的权限
            let parent = ou.Parent
            if (parent) {
                let childs = parent.Childs
                //勾选同层所有非当前的组织单元的权限
                for (var i = 0; i < childs.length; i++) {
                    if (childs[i].Id != ou.Id) {
                        pm.OrgUnits.push(childs[i].Id)
                    }
                }
                //继续往上递归
                this.loopDemotion(parent, pm);
            }
        }
    }
    //用于权限变更后保存权限模型
    savePermission(pm) {
        sys.Services.AdminService.SavePermission(pm.Id, pm.OrgUnits).catch(err => {
            this.$message.error(err) // todo: 重新加载
        })
    }

    mounted() {
        sys.Services.AdminService.LoadPermissionNodes().then(res => {
            this.nodes = res
        })
    }
}
