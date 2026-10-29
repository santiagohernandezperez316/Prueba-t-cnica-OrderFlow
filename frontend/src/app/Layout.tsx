import { Button, Navbar, NavbarBrand, Sidebar, SidebarItem, SidebarItemGroup, SidebarItems, Toast, ToastToggle } from 'flowbite-react'
import { useEffect, useState } from 'react'
import { Outlet, useLocation } from 'react-router'
import { RouterLink } from '../shared/RouterLink'
import type { LayoutContext } from './layoutContext'

const TOAST_DURATION_MS = 4000

const sidebarTheme = {
  root: { inner: 'h-full overflow-y-auto overflow-x-hidden rounded-none bg-primary-700 px-3 py-4' },
  item: {
    base: 'flex items-center justify-center rounded-lg p-2 text-base font-medium text-white hover:bg-primary-800',
    active: 'bg-primary-800',
  },
}

export function Layout() {
  const { pathname } = useLocation()
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [isMenuOpen, setIsMenuOpen] = useState(false)

  const closeMenu = () => setIsMenuOpen(false)

  useEffect(() => {
    if (!successMessage) return
    const timer = setTimeout(() => setSuccessMessage(null), TOAST_DURATION_MS)
    return () => clearTimeout(timer)
  }, [successMessage])

  return (
    <div className="flex min-h-screen flex-col bg-gray-50">
      <Navbar fluid className="border-b-2 border-primary-500">
        <NavbarBrand as={RouterLink} href="/orders">
          <img src="/q10-logo.svg" alt="Q10" className="mr-3 h-8" />
          <span className="text-lg font-semibold text-gray-900">Panel de pedidos</span>
        </NavbarBrand>
        <Button
          color="light"
          size="sm"
          className="md:hidden"
          aria-label={isMenuOpen ? 'Cerrar menú' : 'Abrir menú'}
          aria-expanded={isMenuOpen}
          aria-controls="sidebar"
          onClick={() => setIsMenuOpen((open) => !open)}
        >
          <svg className="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} aria-hidden="true">
            <path strokeLinecap="round" d="M4 6h16M4 12h16M4 18h16" />
          </svg>
        </Button>
      </Navbar>

      <div className="flex flex-1 flex-col md:flex-row">
        <Sidebar
          id="sidebar"
          aria-label="Navegación principal"
          className={`${isMenuOpen ? 'block' : 'hidden'} h-auto w-full shrink-0 md:block md:w-56`}
          theme={sidebarTheme}
        >
          <SidebarItems>
            <SidebarItemGroup>
              <SidebarItem as={RouterLink} href="/orders" active={pathname.startsWith('/orders')} onClick={closeMenu}>
                Pedidos
              </SidebarItem>
              <SidebarItem as={RouterLink} href="/products" active={pathname === '/products'} onClick={closeMenu}>
                Inventario
              </SidebarItem>
            </SidebarItemGroup>
          </SidebarItems>
        </Sidebar>

        <main className="min-w-0 flex-1 p-4 sm:p-6">
          <Outlet context={{ showSuccess: setSuccessMessage } satisfies LayoutContext} />
        </main>
      </div>

      {successMessage && (
        <Toast role="status" className="fixed top-20 right-4 border border-green-200 bg-green-50 text-green-800">
          <p className="text-sm font-medium">{successMessage}</p>
          <ToastToggle onDismiss={() => setSuccessMessage(null)} className="bg-green-50 text-green-700 hover:bg-green-100" />
        </Toast>
      )}
    </div>
  )
}
