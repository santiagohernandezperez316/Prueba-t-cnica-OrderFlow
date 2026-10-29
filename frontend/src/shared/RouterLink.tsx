import type { ComponentProps } from 'react'
import { Link } from 'react-router'

export function RouterLink({ href = '/', ...props }: ComponentProps<'a'>) {
  return <Link to={href} {...props} />
}
