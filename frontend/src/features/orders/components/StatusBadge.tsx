import { Badge } from 'flowbite-react'
import type { OrderStatus } from '../types'

const statusStyles: Record<OrderStatus, { color: 'warning' | 'success' | 'failure'; label: string }> = {
  Pending: { color: 'warning', label: 'Pendiente' },
  Confirmed: { color: 'success', label: 'Confirmado' },
  Rejected: { color: 'failure', label: 'Rechazado' },
}

export function StatusBadge({ status }: { status: OrderStatus }) {
  const { color, label } = statusStyles[status]

  return (
    <Badge color={color} className="w-fit">
      {label}
    </Badge>
  )
}
