@Component({
    components: { PermissionTree: sys.Views.PermissionTree }
})
export default class OrgUnits extends Vue {

    const bizUnitBaseType = '11428632783249997832'
    const groupBaseType = '11428632783249997836'
    const empBaseType = '11428632783249997828'

    const EmploeeView = sys.Views.EmploeeView
    const BizUnitView = sys.Views.EnterpriseView
    const WorkgroupView = sys.Views.WorkgroupView

    orgUnitNodes = []
    currentOrgUnit = null
    orgUnitTreeProps = {
        label: 'Name',
        children: 'Childs'
    }
    orgUnitView = null

    /**根据类型获取节点的图标*/
    getIcon(data, node) {
        if (data.BaseType == this.bizUnitBaseType) {
            return 'fas fa-home'
        } else if (data.BaseType == this.empBaseType) {
            return 'fas fa-user'
        } else {
            return 'fas fa-folder'
        }
    }

    onCurrentChanged(data, node) {
        this.currentOrgUnit = data
        if (data.BaseType == this.empBaseType) {
            this.orgUnitView = this.EmploeeView
        } else if (data.BaseType == this.bizUnitBaseType) {
            this.orgUnitView = this.BizUnitView
        } else {
            this.orgUnitView = this.WorkgroupView
        }
    }

    /**新建工作组节点*/
    onCreateWorkgroup() {
        if (!this.currentOrgUnit) {
            this.$message.error('请先选择上级节点')
            return
        }
        if (this.currentOrgUnit.BaseType == this.empBaseType) {
            this.$message.error('请选择非员工上级节点')
            return
        }
        sys.Services.OrgUnitService.NewWorkgroup(this.currentOrgUnit.Id).then(res => {
            res.Parent = this.currentOrgUnit
            if (!res.Parent.Childs) { this.$set(res.Parent, 'Childs', []) }
            res.Parent.Childs.push(res)
        }).catch(err => {
            this.$message.error('新建工作组失败: ' + err)
        })
    }

    /**新建员工节点*/
    onCreateEmploee() {
        if (!this.currentOrgUnit) {
            this.$message.error('请先选择上级节点')
            return
        }
        if (this.currentOrgUnit.BaseType == this.empBaseType) {
            this.$message.error('请选择非员工上级节点')
            return
        }
        sys.Services.OrgUnitService.NewEmploee(this.currentOrgUnit.Id).then(res => {
            res.Parent = this.currentOrgUnit
            if (!res.Parent.Childs) { this.$set(res.Parent, 'Childs', []) }
            res.Parent.Childs.push(res)
        }).catch(err => {
            this.$message.error('新建员工失败: ' + err)
        })
    }

    /**保存节点信息*/
    onSave() {
        if (!this.orgUnitView) {
            return
        }
        let save = this.$refs.ouview.save
        if (save && typeof save === 'function') {
            save()
        }
    }

    /**删除组织单元节点*/
    onDelete() {
        if (!this.currentOrgUnit) {
            this.$message.error('请先选择待删除节点')
            return
        }
        if (!this.currentOrgUnit.ParentId) {
            this.$message.error('不能删除根节点')
            return
        }
        if (this.currentOrgUnit.Childs && this.currentOrgUnit.Childs.length > 0) {
            this.$message.error('不能删除具有子节点的节点')
            return
        }

        this.$confirm('此操作将永久删除该节点, 是否继续?', '提示', {
            confirmButtonText: '确定',
            cancelButtonText: '取消',
            type: 'warning'
        }).then(() => {
            sys.Services.OrgUnitService.DeleteOrgUnit(this.currentOrgUnit.Id).then(res => {
                let parent = this.currentOrgUnit.Parent
                let index = parent.Childs.findIndex(t => t.Id == this.currentOrgUnit.Id)
                parent.Childs.splice(index, 1)
            }).catch(err => {
                this.$message.error('删除节点失败: ' + err)
            })
        }).catch(() => { })
    }

    mounted() {
        sys.Services.OrgUnitService.LoadTreeList().then(res => {
            this.orgUnitNodes = $runtime.channel.resolveObjRef(res)
            this.onCurrentChanged(this.orgUnitNodes[0])
            //默认选中根级 element2
            // this.$nextTick(() => {
            //     this.$refs.outree.setCurrentKey(this.orgUnitNodes[0].ID)
            // })
        })
    }
}
