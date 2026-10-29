import { HubConnectionBuilder } from '@microsoft/signalr'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { createOrder, getOrders, getSkus } from './api'
import type { Order, OrderStatusChangedEvent } from './types'

const ordersKey = ['orders']
const POLLING_INTERVAL_MS = 5000
const HUB_RETRY_DELAY_MS = 5000

export function useOrders(isLive: boolean) {
  return useQuery({
    queryKey: ordersKey,
    queryFn: getOrders,
    refetchInterval: isLive ? false : POLLING_INTERVAL_MS,
  })
}

export function useSkus() {
  return useQuery({ queryKey: ['skus'], queryFn: getSkus, staleTime: Infinity })
}

export function useCreateOrder() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: createOrder,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ordersKey }),
  })
}

export function useOrderUpdates(): boolean {
  const queryClient = useQueryClient()
  const [isLive, setIsLive] = useState(false)

  useEffect(() => {
    const connection = new HubConnectionBuilder().withUrl('/hubs/orders').withAutomaticReconnect().build()
    let retryTimer: ReturnType<typeof setTimeout> | undefined
    let disposed = false

    const refreshOrders = () => queryClient.invalidateQueries({ queryKey: ordersKey })

    connection.on('OrderStatusChanged', (event: OrderStatusChangedEvent) => {
      const orders = queryClient.getQueryData<Order[]>(ordersKey)

      if (!orders?.some((order) => order.id === event.id)) {
        void refreshOrders()
        return
      }

      queryClient.setQueryData<Order[]>(
        ordersKey,
        orders.map((order) =>
          order.id === event.id ? { ...order, estado: event.estado, motivoRechazo: event.motivoRechazo } : order,
        ),
      )
    })

    connection.onreconnecting(() => setIsLive(false))
    connection.onreconnected(() => {
      setIsLive(true)
      void refreshOrders()
    })
    connection.onclose(() => {
      setIsLive(false)
      scheduleStart()
    })

    async function start() {
      try {
        await connection.start()
        setIsLive(true)
        void refreshOrders()
      } catch {
        scheduleStart()
      }
    }

    function scheduleStart() {
      if (!disposed) {
        retryTimer = setTimeout(() => void start(), HUB_RETRY_DELAY_MS)
      }
    }

    void start()

    return () => {
      disposed = true
      clearTimeout(retryTimer)
      void connection.stop()
    }
  }, [queryClient])

  return isLive
}
