import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef, type FormEvent, type InputHTMLAttributes } from 'react'
import { ApiError, productRules, productsQueryKey, useProductsApi, type NewProduct } from './api.ts'

export default function ProductForm() {
  const api = useProductsApi()
  const queryClient = useQueryClient()
  const formRef = useRef<HTMLFormElement>(null)
  const { mutate, data, error, isPending, isSuccess } = useMutation({
    mutationFn: api.create,
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: productsQueryKey }),
  })
  const fieldErrors = error instanceof ApiError ? error.fieldErrors : {}

  useEffect(() => {
    formRef.current?.querySelector<HTMLInputElement>('[aria-invalid="true"]')?.focus()
  }, [error])

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = event.currentTarget
    const values = new FormData(form)
    const text = (field: keyof NewProduct) => {
      const value = values.get(field)
      return typeof value === 'string' ? value : ''
    }
    const product: NewProduct = { name: text('name'), colour: text('colour'), price: Number(text('price')) }
    mutate(product, { onSuccess: () => form.reset() })
  }

  return (
    <section className="panel" aria-labelledby="new-product-title">
      <h2 id="new-product-title">New product</h2>
      <form ref={formRef} onSubmit={submit}>
        <Field name="name" label="Name" errors={fieldErrors.name} required maxLength={productRules.nameMaxLength} autoComplete="off" />
        <Field name="colour" label="Colour" errors={fieldErrors.colour} required maxLength={productRules.colourMaxLength} autoComplete="off" />
        <Field
          name="price"
          label="Price"
          errors={fieldErrors.price}
          required
          type="number"
          inputMode="decimal"
          min={productRules.minPrice}
          max={productRules.maxPrice}
          step={productRules.minPrice}
        />
        {error && Object.keys(fieldErrors).length === 0 && (
          <p className="alert" role="alert">
            {error.message}
          </p>
        )}
        {isSuccess && (
          <p className="status" role="status">
            Added {data.name}.
          </p>
        )}
        <button className="button primary block" disabled={isPending}>
          {isPending ? 'Adding…' : 'Add product'}
        </button>
      </form>
    </section>
  )
}

type FieldProps = InputHTMLAttributes<HTMLInputElement> & {
  name: keyof NewProduct
  label: string
  errors: string[] | undefined
}

function Field({ name, label, errors, ...input }: FieldProps) {
  const errorId = `${name}-error`

  return (
    <div className="field">
      <label className="label" htmlFor={name}>
        {label}
      </label>
      <input
        id={name}
        name={name}
        aria-invalid={errors ? true : undefined}
        aria-describedby={errors ? errorId : undefined}
        {...input}
      />
      {errors && (
        <p id={errorId} className="field-error">
          {errors.join(' ')}
        </p>
      )}
    </div>
  )
}
