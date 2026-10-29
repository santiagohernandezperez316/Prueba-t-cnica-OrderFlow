import { useOutletContext } from 'react-router'

export interface LayoutContext {
  showSuccess: (message: string) => void
}

export const useLayout = () => useOutletContext<LayoutContext>()
