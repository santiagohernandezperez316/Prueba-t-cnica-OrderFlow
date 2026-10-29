import { useQuery } from '@tanstack/react-query'
import { getProducts } from './api'

const POLLING_INTERVAL_MS = 5000

export function useProducts() {
  return useQuery({ queryKey: ['products'], queryFn: getProducts, refetchInterval: POLLING_INTERVAL_MS })
}
