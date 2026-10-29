import { Table, TableBody, TableCell, TableHead, TableHeadCell, TableRow } from 'flowbite-react'
import type { Order } from '../types'
import { StatusBadge } from './StatusBadge'

const dateFormat = new Intl.DateTimeFormat('es-CO', { dateStyle: 'short', timeStyle: 'medium' })

export function OrdersTable({ orders }: { orders: Order[] }) {
  return (
    <div className="overflow-x-auto">
      <Table>
        <TableHead>
          <TableRow>
            <TableHeadCell>Id</TableHeadCell>
            <TableHeadCell>Cliente</TableHeadCell>
            <TableHeadCell>SKU</TableHeadCell>
            <TableHeadCell>Cantidad</TableHeadCell>
            <TableHeadCell>Estado</TableHeadCell>
            <TableHeadCell>Fecha</TableHeadCell>
          </TableRow>
        </TableHead>
        <TableBody className="divide-y divide-gray-200">
          {orders.map((order) => (
            <TableRow key={order.id} className="bg-white">
              <TableCell className="font-mono text-gray-900" title={order.id}>
                {order.id.slice(0, 8)}
              </TableCell>
              <TableCell>{order.clienteNombre}</TableCell>
              <TableCell>{order.sku}</TableCell>
              <TableCell>{order.cantidad}</TableCell>
              <TableCell>
                <StatusBadge status={order.estado} />
                {order.estado === 'Rejected' && order.motivoRechazo && (
                  <p className="mt-1 text-xs text-gray-500">{order.motivoRechazo}</p>
                )}
              </TableCell>
              <TableCell>{dateFormat.format(new Date(order.creadoEn))}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  )
}
