<template>
  <div class="emails-table">
    <q-table
      :data="dataToShow"
      :columns="columns"
      row-key="_id"
      :pagination.sync="pagination"
      :loading="loading"
      :filter="filter"
      binary-state-sort
      dense
      style="height: 100%"
    >
      <template v-slot:top>
        <div class="row justify-center q-gutter-sm">
          <q-btn
            label="新增"
            dense
            size="sm"
            color="secondary"
            class="q-pr-xs q-pl-xs"
            @click="openNewEmailDialog"
          ></q-btn>
          <q-btn
            label="从Excel导入"
            dense
            size="sm"
            color="orange"
            class="q-pr-xs q-pl-xs"
            @click="selectExcelFile"
          ></q-btn>
          <span class="text-subtitle1 text-primary">{{ group.name }}</span>
          <input
            type="file"
            id="fileInput"
            style="display: none"
            accept="application/vnd.ms-excel,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            @change="fileSelected"
          />
        </div>
        <q-space />
        <q-input
          dense
          debounce="300"
          placeholder="搜索"
          color="primary"
          v-model="filter"
        >
          <template v-slot:append>
            <q-icon name="search" />
          </template>
        </q-input>
      </template>
      <template v-slot:header-cell-operation="props">
        <q-th :props="props">
          {{ props.col.label }}
          <q-btn
            v-if="data.length > 0"
            :size="btn_delete.size"
            color="warning"
            label="清空"
            :dense="btn_delete.dense"
            @click="clearGroup()"
          >
          </q-btn>
        </q-th>
      </template>

      <template v-slot:body-cell-operation="props">
        <q-td :props="props" class="row justify-end">
          <q-btn
            :size="btn_modify.size"
            :color="btn_modify.color"
            :label="btn_modify.label"
            :dense="btn_modify.dense"
            @click="showModifyEmailDialog(props.row)"
            class="q-mr-sm"
          >
          </q-btn>

          <q-btn
            :size="btn_delete.size"
            :color="btn_delete.color"
            :label="btn_delete.label"
            :dense="btn_delete.dense"
            @click="deleteEmailInfo(props.row._id)"
          >
          </q-btn>
        </q-td>
      </template>
    </q-table>

    <q-dialog v-model="isShowNewEmailDialog">
      <DialogForm
        type="create"
        @createSuccess="addedNewEmail"
        :initParams="initNewEmailParams"
      />
    </q-dialog>

    <q-dialog v-model="isShowModifyEmailDialog">
      <DialogForm
        type="update"
        @updateSuccess="modifiedEmail"
        :initParams="initModifyEmailParams"
      />
    </q-dialog>
  </div>
</template>

<script>
import DialogForm from '@/components/DialogForm'
import NewEmail from '../mixins/newEmail.vue'
import NewEmails from '../mixins/newEmails.vue'
import ModifyEmail from '../mixins/modifyEmail.vue'

import { getEmails, deleteEmail, deleteEmails } from '@/api/group'

import { table } from '@/themes/index'
import { notifySuccess, okCancle } from '@/components/iPrompt'
const { btn_modify, btn_delete } = table

export default {
  mixins: [NewEmail, ModifyEmail, NewEmails],
  components: { DialogForm },
  props: {
    group: {
      type: Object,
      default() {
        return {
          groupType: 'send'
        }
      }
    }
  },

  computed: {
    dataToShow() {
      if (!this.filter) return this.data

      return this.data.filter(d => {
        if (d.userName && d.userName.indexOf(this.filter) > -1) return true
        if (d.email && d.email.indexOf(this.filter) > -1) return true
        if (d.smtp && d.smtp.indexOf(this.filter) > -1) return true
        if (d.password && d.password.indexOf(this.filter) > -1) return true
        return false
      })
    },
    columns() {
      if (this.group.groupType === 'send') {
        return [
          {
            name: 'userName',
            required: true,
            label: '姓名',
            align: 'left',
            field: row => row.userName,
            sortable: true
          },
          {
            name: 'email',
            required: true,
            label: '邮箱',
            align: 'left',
            field: row => row.email,
            sortable: true
          },
          {
            name: 'smtp',
            required: true,
            label: 'SMTP服务器地址',
            align: 'left',
            field: row => row.smtp,
            sortable: true
          },
          {
            name: 'password',
            required: true,
            label: 'SMTP密码',
            align: 'left',
            field: row => row.password,
            sortable: true
          },
          {
            name: 'operation',
            label: '操作',
            align: 'right'
          }
        ]
      } else {
        return [
          {
            name: 'userName',
            required: true,
            label: '姓名',
            align: 'left',
            field: row => row.userName,
            sortable: true
          },
          {
            name: 'email',
            required: true,
            label: '邮箱',
            align: 'left',
            field: row => row.email,
            sortable: true
          },
          {
            name: 'operation',
            label: '操作',
            align: 'right'
          }
        ]
      }
    }
  },

  data() {
    return {
      btn_modify,
      btn_delete,

      filter: '',
      loading: false,
      // 分页数据
      pagination: {
        sortBy: 'userName',
        descending: false,
        page: 1,
        rowsPerPage: 0,
        rowsNumber: 0
      },

      data: []
    }
  },

  async mounted() {
    const { data } = await getEmails(this.group._id)
    this.data = data || []
  },

  methods: {
    // 删除邮箱
    async deleteEmailInfo(emailInfoId) {
      const ok = await okCancle('是否删除该条邮箱信息?')
      if (!ok) return

      // 开始删除
      await deleteEmail(emailInfoId)

      // 清空现有数据
      const index = this.data.findIndex(d => d._id === emailInfoId)
      if (index > -1) this.data.splice(index, 1)

      notifySuccess('删除成功')
    },

    // 清空组
    async clearGroup() {
      const ok = await okCancle('是否清空该组下所有邮箱?')
      if (!ok) return

      await deleteEmails(this.group._id)

      this.data = []
      notifySuccess('已全部清除')
    }
  }
}
</script>

<style lang='scss'>
.emails-table {
  position: absolute;
  top: 0;
  bottom: 0;
  left: 0;
  right: 0;
}
</style>