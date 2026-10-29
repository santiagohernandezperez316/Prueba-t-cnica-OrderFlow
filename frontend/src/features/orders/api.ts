import { request } from '../../shared/http'
import type { CreateOrderRequest, Order } from './types'

export const getOrders = () => request<Order[]>('/api/orders')

export const createOrder = (input: CreateOrderRequest) =>
  request<Order>('/api/orders', { method: 'POST', body: input })

export const getSkus = () => request<string[]>('/api/skus')
