import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { OrderForm } from './OrderForm'

const jsonResponse = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })

function mockApi(createOrderResponse: () => Response) {
  const fetchMock = vi.fn(async (url: string, init?: RequestInit) => {
    if (url === '/api/skus') return jsonResponse(['SKU-001', 'SKU-002'])
    if (url === '/api/orders' && init?.method === 'POST') return createOrderResponse()
    throw new Error(`Petición inesperada: ${init?.method ?? 'GET'} ${url}`)
  })
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

function renderForm() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={queryClient}>
      <OrderForm onCreated={vi.fn()} />
    </QueryClientProvider>,
  )
}

describe('OrderForm', () => {
  beforeEach(() => vi.unstubAllGlobals())
  afterEach(() => vi.unstubAllGlobals())

  it('muestra los errores de validación y no llama al backend con datos inválidos', async () => {
    const fetchMock = mockApi(() => jsonResponse({}, 201))
    const user = userEvent.setup()
    renderForm()

    await user.clear(screen.getByLabelText('Cantidad'))
    await user.type(screen.getByLabelText('Cantidad'), '0')
    await user.click(screen.getByRole('button', { name: 'Crear pedido' }))

    expect(await screen.findByText('clienteNombre es obligatorio.')).toBeInTheDocument()
    expect(screen.getByText('sku es obligatorio.')).toBeInTheDocument()
    expect(screen.getByText('cantidad debe estar entre 1 y 100.')).toBeInTheDocument()
    expect(fetchMock).not.toHaveBeenCalledWith('/api/orders', expect.anything())
  })

  it('muestra bajo cada campo los errores de un 400 del backend', async () => {
    mockApi(() =>
      jsonResponse(
        {
          title: 'One or more validation errors occurred.',
          status: 400,
          errors: { sku: ["el sku 'SKU-001' no existe."] },
        },
        400,
      ),
    )
    const user = userEvent.setup()
    renderForm()

    await user.type(screen.getByLabelText('Cliente'), 'Ana Gómez')
    await screen.findByRole('option', { name: 'SKU-001' })
    await user.selectOptions(screen.getByLabelText('SKU'), 'SKU-001')
    await user.click(screen.getByRole('button', { name: 'Crear pedido' }))

    expect(await screen.findByText("el sku 'SKU-001' no existe.")).toBeInTheDocument()
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })
})
