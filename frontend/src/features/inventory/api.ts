import { request } from '../../shared/http'
import type { Product } from './types'

export const getProducts = () => request<Product[]>('/inventory-api/products')
