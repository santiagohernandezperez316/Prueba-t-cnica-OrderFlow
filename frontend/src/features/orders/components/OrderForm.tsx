import { zodResolver } from '@hookform/resolvers/zod'
import { Alert, Button, HelperText, Label, Select, Spinner, TextInput } from 'flowbite-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { ApiError } from '../../../shared/http'
import { useCreateOrder, useSkus } from '../hooks'
import { orderSchema, type OrderFormValues } from '../schema'
import type { Order } from '../types'

const formFields = ['clienteNombre', 'sku', 'cantidad'] as const

interface OrderFormProps {
  onCreated: (order: Order) => void
}

export function OrderForm({ onCreated }: OrderFormProps) {
  const skus = useSkus()
  const createOrder = useCreateOrder()
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors },
  } = useForm<OrderFormValues>({
    resolver: zodResolver(orderSchema),
    defaultValues: { clienteNombre: '', sku: '', cantidad: 1 },
  })

  function showFieldErrors(error: Error): boolean {
    if (!(error instanceof ApiError)) return false

    let shown = false
    for (const field of formFields) {
      const messages = error.fieldErrors[field]
      if (messages?.length) {
        setError(field, { type: 'server', message: messages.join(' ') })
        shown = true
      }
    }
    return shown
  }

  function onSubmit(values: OrderFormValues) {
    setFormError(null)
    createOrder.mutate(values, {
      onSuccess: (order) => {
        reset()
        onCreated(order)
      },
      onError: (error) => {
        if (!showFieldErrors(error)) setFormError(error.message)
      },
    })
  }

  return (
    <form noValidate onSubmit={handleSubmit(onSubmit)} className="flex max-w-md flex-col gap-4">
      {formError && <Alert color="failure">{formError}</Alert>}
      {skus.isError && <Alert color="failure">No se pudieron cargar los SKUs: {skus.error.message}</Alert>}

      <div>
        <Label htmlFor="clienteNombre">Cliente</Label>
        <TextInput
          id="clienteNombre"
          {...register('clienteNombre')}
          color={errors.clienteNombre ? 'failure' : 'gray'}
          className="mt-1"
        />
        {errors.clienteNombre && <HelperText color="failure">{errors.clienteNombre.message}</HelperText>}
      </div>

      <div>
        <Label htmlFor="sku">SKU</Label>
        <Select
          id="sku"
          {...register('sku')}
          disabled={!skus.data}
          color={errors.sku ? 'failure' : 'gray'}
          className="mt-1"
        >
          <option value="">{skus.isPending ? 'Cargando SKUs…' : 'Selecciona un SKU'}</option>
          {skus.data?.map((sku) => (
            <option key={sku} value={sku}>
              {sku}
            </option>
          ))}
        </Select>
        {errors.sku && <HelperText color="failure">{errors.sku.message}</HelperText>}
      </div>

      <div>
        <Label htmlFor="cantidad">Cantidad</Label>
        <TextInput
          id="cantidad"
          type="number"
          min={1}
          max={100}
          {...register('cantidad', { valueAsNumber: true })}
          color={errors.cantidad ? 'failure' : 'gray'}
          className="mt-1"
        />
        {errors.cantidad && <HelperText color="failure">{errors.cantidad.message}</HelperText>}
      </div>

      <Button type="submit" disabled={createOrder.isPending} className="w-fit">
        {createOrder.isPending && <Spinner size="sm" light className="me-2" />}
        {createOrder.isPending ? 'Creando…' : 'Crear pedido'}
      </Button>
    </form>
  )
}
