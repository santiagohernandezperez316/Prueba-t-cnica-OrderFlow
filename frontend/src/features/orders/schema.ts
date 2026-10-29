import { z } from 'zod'

const cantidadMessage = 'cantidad debe estar entre 1 y 100.'

export const orderSchema = z.object({
  clienteNombre: z
    .string()
    .trim()
    .min(1, 'clienteNombre es obligatorio.')
    .max(200, 'clienteNombre no puede superar 200 caracteres.'),
  sku: z.string().min(1, 'sku es obligatorio.'),
  cantidad: z
    .number({ error: cantidadMessage })
    .int('cantidad debe ser un número entero.')
    .min(1, cantidadMessage)
    .max(100, cantidadMessage),
})

export type OrderFormValues = z.infer<typeof orderSchema>
