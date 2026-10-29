import { useNavigate } from 'react-router'
import { useLayout } from '../../../app/layoutContext'
import { OrderForm } from '../components/OrderForm'
import type { Order } from '../types'

export function NewOrderPage() {
  const navigate = useNavigate()
  const { showSuccess } = useLayout()

  function handleCreated(order: Order) {
    showSuccess(`Pedido de ${order.clienteNombre} creado (${order.sku} × ${order.cantidad}).`)
    navigate('/orders')
  }

  return (
    <section>
      <h1 className="mb-4 text-2xl font-semibold text-gray-900">Nuevo pedido</h1>
      <OrderForm onCreated={handleCreated} />
    </section>
  )
}
