export type OrderStatus = 'Pending' | 'Confirmed' | 'Rejected'

export interface Order {
  id: string
  clienteNombre: string
  sku: string
  cantidad: number
  estado: OrderStatus
  creadoEn: string
  motivoRechazo: string | null
}

export interface CreateOrderRequest {
  clienteNombre: string
  sku: string
  cantidad: number
}

export interface OrderStatusChangedEvent {
  id: string
  estado: OrderStatus
  motivoRechazo: string | null
}
