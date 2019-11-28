@Component
export default class EnterpriseView extends Vue {
    /**当前组织单元实例*/
    @Prop({ type: Object, default: null }) orgUnit

    /**创建工作组对话框是否打开*/
    entity = { Name: '', Address: '' }

    fetch() {
        if (!this.orgUnit) {
            return
        }
        sys.Services.OrgUnitService.LoadEnterprise(this.orgUnit.BaseId).then(res => {
            this.entity = res
        }).catch(err => {
            this.$message.error("组织不存在：" + err)
        })
    }

    save() {
        sys.Services.OrgUnitService.SaveEnterprise(this.entity, this.orgUnit.Id).then(res => {
            this.$message.success('保存组织信息成功')
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
