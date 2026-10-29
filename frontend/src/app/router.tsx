import { createBrowserRouter, Navigate } from 'react-router'
import { ProductsPage } from '../features/inventory/pages/ProductsPage'
import { NewOrderPage } from '../features/orders/pages/NewOrderPage'
import { OrdersPage } from '../features/orders/pages/OrdersPage'
import { Layout } from './Layout'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    children: [
      { index: true, element: <Navigate to="/orders" replace /> },
      { path: 'orders', element: <OrdersPage /> },
      { path: 'orders/new', element: <NewOrderPage /> },
      { path: 'products', element: <ProductsPage /> },
      { path: '*', element: <Navigate to="/orders" replace /> },
    ],
  },
])
