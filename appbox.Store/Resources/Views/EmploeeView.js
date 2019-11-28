@Component
export default class EmploeeView extends Vue {
    /**当前组织单元实例*/
    @Prop({ type: Object, default: null }) orgUnit

    const newUserTitle = '新建用户'
    const resetPassTitle = '重置密码'

    /**创建用户对话框是否打开*/
    dlgTitle = this.newUserTitle
    dlgVisible = false
    dlgAccount = ''
    dlgPassword = ''

    entity = {
        Name: '',
        Male: true,
        Birthday: new Date('1977-3-16'),
        Account: ''
    }

    get btAccountIcon() {
        return this.entity.Account ? "fas fa-times" : "fas fa-plus"
    }

    onAccountClick() {
        if (this.entity.Account) {
            this.deleteAccount()
        } else {
            this.dlgTitle = this.newUserTitle
            this.dlgVisible = true
        }
    }
    onResetPassClick() {
        this.dlgTitle = this.resetPassTitle
        this.dlgVisible = true
    }
    onDlgOkClick() {
        if (this.dlgTitle === this.newUserTitle) {
            this.createAccount()
        } else { // 重置密码状态
            this.resetPassword()
        }
        this.dlgVisible = false
    }

    /**创建员工用户*/
    createAccount() {
        if (!this.dlgAccount || !this.dlgPassword) {
            this.$message.error('用户名或密码不能为空')
            return
        }
        sys.Services.OrgUnitService.NewEmploeeUser(this.entity, this.dlgAccount, this.dlgPassword).then(res => {
            this.entity.Account = this.dlgAccount
            this.dlgAccount = this.dlgPassword = ''
        }).catch(err => {
            this.$message.error('新建用户失败: ' + err)
        })
    }
    /**删除员工用户*/
    deleteAccount() {
        this.$confirm('此操作将永久删除用户, 是否继续?', '提示', {
            confirmButtonText: '确定',
            cancelButtonText: '取消',
            type: 'warning'
        }).then(() => {
            sys.Services.OrgUnitService.DeleteEmploeeUser(this.entity).then(res => {
                this.entity.Account = ''
            }).catch(err => {
                this.$message.error('删除节点失败: ' + err)
            })
        }).catch(() => { })
    }
    /**重置员工密码*/
    resetPassword() {
        if (!this.dlgPassword) {
            this.$message.error('密码不能为空')
            return
        }
        sys.Services.OrgUnitService.ResetPassword(this.entity, this.dlgPassword).then(res => {
            this.dlgAccount = this.dlgPassword = ''
        }).catch(err => {
            this.$message.error('重置密码失败: ' + err)
        })
    }

    fetch() {
        if (!this.orgUnit) { return }
        sys.Services.OrgUnitService.LoadEmploee(this.orgUnit.BaseId).then(res => {
            this.entity = res
        }).catch(err => {
            this.$message.error(err)
        })
    }

    save() {
        sys.Services.OrgUnitService.SaveEmploee(this.entity, this.orgUnit.Id).then(res => {
            this.$message.success('保存员工信息成功')
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
