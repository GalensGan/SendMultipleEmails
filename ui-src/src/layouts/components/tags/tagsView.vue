<template>
  <div class="tags-view q-ml-md row items-center justify-start">
    <draggable v-model="routes" group="people" @start="drag = true" @end="drag = false" item-key="fullPath">
      <template #item="{ element: item }">
        <q-chip class="q-mr-xs route-tag row items-center" :key="item.fullPath" :class="getTagClass(item)" square
          clickable transition-show="jump-right" transition-hide="jump-left" @click="goToRoute(item)"
          @mouseenter="mouseenterTag(item)" @mouseleave="item.showCloseIcon = false" @remove="onRemoveTag(item)">
          <div>{{ getTagLabel(item) }}</div>
          <ContextMenu :items="tagContextItems" :value="item" />
        </q-chip>
      </template>
    </draggable>

    <!-- <q-chip class="q-mr-xs route-tag row items-center" v-for="item in routes" :key="item.fullPath"
      :class="getTagClass(item)" square clickable transition-show="jump-right" transition-hide="jump-left"
      @click="goToRoute(item)" @mouseenter="mouseenterTag(item)" @mouseleave="item.showCloseIcon = false"
      @remove="onRemoveTag(item)">
      <div>{{ getTagLabel(item) }}</div>
      <ContextMenu :items="tagContextItems" :value="item" />
    </q-chip> -->
  </div>
</template>

<script lang="ts" setup>
import ContextMenu from 'src/components/contextMenu/ContextMenu.vue'

import { IRouteHistory } from './types'
import { useRouteHistories, removeHistory } from './routeHistories'
import { IContextMenuItem } from 'src/components/contextMenu/types'

// 显示和跳转 tag
const { routes } = useRouteHistories()
function getTagClass (item: IRouteHistory) {
  return {
    'bg-primary': item.isActive,
    'text-white': true
  }
}
function getTagLabel (tagItem: IRouteHistory) {
  if (tagItem.query.tagName) return `${tagItem.label} - ${tagItem.query.tagName}`
  return tagItem.label
}
const router = useRouter()
function goToRoute (item: IRouteHistory) {
  router.push({
    path: item.fullPath,
    query: item.query
  })
}

// tags 功能
// 首页没有移除功能
function mouseenterTag (item: IRouteHistory) {
  if (item.fullPath === '/') return
  item.showCloseIcon = true
}

// 移除按钮
async function onRemoveTag (item: IRouteHistory) {
  removeHistory(router, item as unknown as IRouteHistory)
}

// 右键菜单
const tagContextItems: IContextMenuItem[] = [
  {
    name: 'close',
    label: '关闭',
    tooltip: '关闭当前标签',
    onClick: params => onRemoveTag(params as IRouteHistory)
  }, {
    name: 'closeOther',
    label: '关闭其他',
    onClick: async (params) => {
      const current = params as IRouteHistory
      routes.value = routes.value.filter((route) => route.fullPath === current.fullPath)
      // 激活当前
      router.push({
        path: current.fullPath
      })
    }
  }, {
    name: 'closeAll',
    label: '关闭所有',
    onClick: async () => {
      routes.value = []
      router.push({
        path: '/'
      })
    }
  }
]

// #region 拖拽
import draggable from 'vuedraggable'
const drag = ref(false)
// #endregion
</script>

<style lang="scss" scoped>
.route-tag {
  &:hover {
    background-color: $primary;
  }
}
</style>
