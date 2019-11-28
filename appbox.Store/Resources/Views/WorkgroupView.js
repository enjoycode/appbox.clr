@Component
export default class WorkgroupView extends Vue {
    /**当前组织单元实例*/
    @Prop({ type: Object, default: null }) orgUnit

    /**创建工作组对话框是否打开*/
    entity = { Name: '' }

    fetch() {
        if (!this.orgUnit) {
            return
        }
        sys.Services.OrgUnitService.LoadWorkgroup(this.orgUnit.BaseId).then(res => {
            this.entity = res
        }).catch(err => {
            this.$message.error("工作组不存在：" + err)
        })
    }

    save() {
        sys.Services.OrgUnitService.SaveWorkgroup(this.entity, this.orgUnit.Id).then(res => {
            this.$message.success('保存工作组信息成功')
        }).catch(err => {
            this.$message.error(err)
        })
    }

    @Watch('orgUnit')
    onOrgUnitChanged(val) {
        this.fetch()
    }

    @Watch('entity.Name')
    onNameChanged(val) {
        if (this.orgUnit) {
            this.orgUnit.Name = val
        }
    }

    mounted() {
        this.fetch()
    }
}
