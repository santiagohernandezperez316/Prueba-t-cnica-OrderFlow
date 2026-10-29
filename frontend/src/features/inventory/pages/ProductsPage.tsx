import { Alert, Button, Spinner, Table, TableBody, TableCell, TableHead, TableHeadCell, TableRow } from 'flowbite-react'
import { useProducts } from '../hooks'

export function ProductsPage() {
  const { data: products, isPending, isError, error, refetch } = useProducts()

  function renderContent() {
    if (isPending) {
      return (
        <div className="flex items-center gap-2 text-gray-600">
          <Spinner size="sm" /> Cargando inventario…
        </div>
      )
    }

    if (!products) {
      return (
        <Alert color="failure">
          <p>No se pudo cargar el inventario: {error?.message}</p>
          <Button size="xs" color="red" className="mt-2" onClick={() => void refetch()}>
            Reintentar
          </Button>
        </Alert>
      )
    }

    return (
      <>
        {isError && (
          <Alert color="warning" className="mb-4">
            No se pudo actualizar el inventario ({error.message}). Se muestran los últimos datos recibidos.
          </Alert>
        )}
        <div className="overflow-x-auto">
          <Table>
            <TableHead>
              <TableRow>
                <TableHeadCell>SKU</TableHeadCell>
                <TableHeadCell>Producto</TableHeadCell>
                <TableHeadCell>Disponible</TableHeadCell>
              </TableRow>
            </TableHead>
            <TableBody className="divide-y divide-gray-200">
              {products.map((product) => (
                <TableRow key={product.sku} className="bg-white">
                  <TableCell className="font-mono text-gray-900">{product.sku}</TableCell>
                  <TableCell>{product.nombre}</TableCell>
                  <TableCell className={product.disponible === 0 ? 'font-semibold text-red-700' : undefined}>
                    {product.disponible}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      </>
    )
  }

  return (
    <section>
      <h1 className="mb-1 text-2xl font-semibold text-gray-900">Inventario</h1>
      <p className="mb-4 text-sm text-gray-600">
        Stock actual por producto. Un pedido se confirma solo si hay stock cuando Inventory lo procesa.
      </p>
      {renderContent()}
    </section>
  )
}
