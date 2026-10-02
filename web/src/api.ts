import { useAuth } from 'react-oidc-context'

export interface NewProduct {
  name: string
  colour: string
  price: number
}

export interface Product extends NewProduct {
  id: string
}

export interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export type FieldErrors = Partial<Record<keyof NewProduct, string[]>>

export const productRules = {
  nameMaxLength: 100,
  colourMaxLength: 50,
  minPrice: 0.01,
  maxPrice: 1_000_000,
  pageSize: 50,
} as const

export const productsQueryKey = ['products'] as const

export class ApiError extends Error {
  readonly status: number
  readonly problem: ProblemDetails

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? `Request failed with status ${status}.`)
    this.status = status
    this.problem = problem
  }

  get fieldErrors(): FieldErrors {
    return Object.fromEntries(Object.entries(this.problem.errors ?? {}).map(([field, messages]) => [field.toLowerCase(), messages]))
  }
}

export function useProductsApi() {
  const auth = useAuth()

  async function request<T>(query: string, init: RequestInit = {}): Promise<T> {
    const response = await fetch(`/api/products${query}`, {
      ...init,
      headers: {
        Authorization: `Bearer ${auth.user?.access_token ?? ''}`,
        ...(init.body ? { 'Content-Type': 'application/json' } : {}),
      },
    })

    if (response.status === 401) await auth.signinRedirect()
    if (!response.ok) throw new ApiError(response.status, await readProblem(response))

    return (await response.json()) as T
  }

  return {
    list: (colour: string, after: string | undefined, signal: AbortSignal) => {
      const params = new URLSearchParams({ limit: String(productRules.pageSize + 1) })
      if (colour) params.set('colour', colour)
      if (after) params.set('after', after)
      return request<Product[]>(`?${params.toString()}`, { signal })
    },
    create: (product: NewProduct) => request<Product>('', { method: 'POST', body: JSON.stringify(product) }),
  }
}

async function readProblem(response: Response): Promise<ProblemDetails> {
  try {
    return (await response.json()) as ProblemDetails
  } catch {
    return {}
  }
}
