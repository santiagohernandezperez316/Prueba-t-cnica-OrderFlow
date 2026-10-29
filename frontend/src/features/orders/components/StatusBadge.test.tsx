import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { OrderStatus } from '../types'
import { StatusBadge } from './StatusBadge'

describe('StatusBadge', () => {
  it.each<[OrderStatus, string, string]>([
    ['Pending', 'Pendiente', 'bg-yellow-100'],
    ['Confirmed', 'Confirmado', 'bg-green-100'],
    ['Rejected', 'Rechazado', 'bg-red-100'],
  ])('muestra %s como "%s" con %s', (status, label, colorClass) => {
    const { container } = render(<StatusBadge status={status} />)

    expect(screen.getByText(label)).toBeInTheDocument()
    expect(container.firstElementChild).toHaveClass(colorClass)
  })
})
