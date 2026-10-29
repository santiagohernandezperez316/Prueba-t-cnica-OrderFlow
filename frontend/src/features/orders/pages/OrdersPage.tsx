import { Alert, Button, Spinner } from 'flowbite-react'
import { Link } from 'react-router'
import { OrdersTable } from '../components/OrdersTable'
import { useOrderUpdates, useOrders } from '../hooks'

export function OrdersPage() {
  const isLive = useOrderUpdates()
  const { data: orders, isPending, isError, error, refetch } = useOrders(isLive)

  function renderContent() {
    if (isPending) {
      return (
        <div className="flex items-center gap-2 text-gray-600">
          <Spinner size="sm" /> Cargando pedidos…
        </div>
      )
    }

    if (!orders) {
      return (
        <Alert color="failure">
          <p>No se pudieron cargar los pedidos: {error?.message}</p>
          <Button size="xs" color="red" className="mt-2" onClick={() => void refetch()}>
            Reintentar
          </Button>
        </Alert>
      )
    }

    if (orders.length === 0) {
      return (
        <p className="text-gray-600">
          Aún no hay pedidos.{' '}
          <Link to="/orders/new" className="font-medium text-primary-700 underline">
            Crea el primero
          </Link>
          .
        </p>
      )
    }

    return (
      <>
        {isError && (
          <Alert color="warning" className="mb-4">
            No se pudo actualizar la lista ({error.message}). Se muestran los últimos datos recibidos.
          </Alert>
        )}
        <OrdersTable orders={orders} />
      </>
    )
  }

  return (
    <section>
      <div className="mb-4 flex items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <h1 className="text-2xl font-semibold text-gray-900">Pedidos</h1>
          <span className="flex items-center gap-2 text-sm text-gray-600">
            <span className={`h-2 w-2 rounded-full ${isLive ? 'bg-green-500' : 'bg-gray-400'}`} />
            {isLive ? 'En vivo' : 'Polling'}
          </span>
        </div>
        <Button as={Link} to="/orders/new">
          + Nuevo pedido
        </Button>
      </div>
      {renderContent()}
    </section>
  )
}
