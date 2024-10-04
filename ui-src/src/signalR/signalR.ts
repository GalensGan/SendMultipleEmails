/* eslint-disable @typescript-eslint/no-explicit-any */
import * as signalR from '@microsoft/signalr'
import { useConfig } from 'src/config'
import { useUserInfoStore } from 'src/stores/user'
import { UzonMailClientMethods } from './types'
import logger from 'loglevel'

export interface ISignalRs {
  sendingProgressHub?: signalR.HubConnection
}

const signalRs: ISignalRs = {
}

/**
 * 获取发送邮件的 signalR 连接
 * 使用 on 注册事件，服务器会调用该事件并发送通知
 */
export function useSendEmailHub () {
  if (signalRs.sendingProgressHub) return signalRs.sendingProgressHub

  // 新建一个连接
  const config = useConfig()
  const userInfoStore = useUserInfoStore()

  const url = `${config.baseUrl}${config.signalRHub}`
  const signal = new signalR.HubConnectionBuilder()
    .withUrl(url, {
      skipNegotiation: true,
      transport: signalR.HttpTransportType.WebSockets,
      accessTokenFactory () {
        return userInfoStore.token
      }
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build()
  // 前端使用 on 来接收消息，若要注销，使用 off 即可
  // signal.on('SendAll', (res) => {
  //   console.log(res, '收到消息')
  // })

  signal.onclose((err) => {
    logger.debug('[signalR] 连接已经断开, 执行函数 onclose', err)
    signalRs.sendingProgressHub = undefined
  })

  signal.start().then(() => {
    logger.info('[signalR] 连接成功')
  })

  signal.onreconnected((msg) => {
    logger.info('[signalR] 重新连接成功', msg)
  })

  signalRs.sendingProgressHub = signal

  return signal
}

/**
 * 向服务器订阅通信
 * 在 setup 中调用，当组件销毁时会自动取消订阅
 * @param methodEnum
 * @param callback
 * @returns
 */
export function subscribeOne (methodEnum: UzonMailClientMethods, callback: (...args: any[]) => any): void {
  const hub = useSendEmailHub()
  if (!hub) return

  const methodName = UzonMailClientMethods[methodEnum]
  onBeforeMount(async () => {
    logger.debug('[signalR] 订阅事件', methodName)
    // hub.on(methodName, callback)
    hub.on(methodName, callback)
  })

  onUnmounted(async () => {
    logger.debug('[signalR] 取消订阅事件', methodName)
    hub.off(methodName, callback)
  })
}

/**
 * 只订阅一次
 */
export function subscribeOnce (methodEnum: UzonMailClientMethods, callbackFunc: (...args: any[]) => any): void {
  const hub = useSendEmailHub()
  if (!hub) return

  const methodName = UzonMailClientMethods[methodEnum]

  async function methodCore (...args: any[]): any {
    await callbackFunc(...args)
    const hub = useSendEmailHub()
    if (!hub) return

    hub.off(methodName, methodCore)
  }

  onMounted(() => {
    hub.on(methodName, methodCore)
  })

  onUnmounted(() => {
    hub.off(methodName, methodCore)
  })
}

/**
 * 订阅信息
 */
export interface ISubscribeInfo {
  methodName: string
  callback: (...args: any[]) => any
}

export function subscribeMany (subscribeInfos: ISubscribeInfo[]) {
  const hub = useSendEmailHub()
  if (!hub) return

  onMounted(() => {
    for (const { methodName, callback } of subscribeInfos) {
      hub.on(methodName, callback)
    }
  })

  onUnmounted(() => {
    for (const { methodName, callback } of subscribeInfos) {
      hub.off(methodName, callback)
    }
  })
}
