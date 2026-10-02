import { useInfiniteQuery } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { productRules, productsQueryKey, useProductsApi } from './api.ts'

const priceFormat = new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })

export default function ProductList() {
  const api = useProductsApi()
  const [draft, setDraft] = useState('')
  const [colour, setColour] = useState('')
  const { data, error, isPending, hasNextPage, fetchNextPage, isFetchingNextPage } = useInfiniteQuery({
    queryKey: [...productsQueryKey, colour],
    queryFn: ({ pageParam, signal }) => api.list(colour, pageParam, signal),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: lastPage => (lastPage.length === productRules.pageSize ? lastPage.at(-1)?.id : undefined),
  })
  const products = data?.pages.flat() ?? []

  function applyFilter(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setColour(draft.trim())
  }

  function clearFilter() {
    setDraft('')
    setColour('')
  }

  return (
    <section className="panel" aria-labelledby="catalogue-title">
      <div className="panel-header">
        <h2 id="catalogue-title">Catalogue</h2>
        <form role="search" className="filter" onSubmit={applyFilter}>
          <label className="label" htmlFor="colour-filter">
            Colour
          </label>
          <input id="colour-filter" type="search" placeholder="Any colour" value={draft} onChange={event => setDraft(event.target.value)} />
          <button className="button">Filter</button>
          {colour && (
            <button type="button" className="button" onClick={clearFilter}>
              Clear
            </button>
          )}
        </form>
      </div>

      {isPending ? (
        <p className="status" role="status">
          Loading products…
        </p>
      ) : error ? (
        <p className="alert" role="alert">
          {error.message}
        </p>
      ) : products.length === 0 ? (
        <div className="empty">
          <p className="empty-title">{colour ? `No ${colour} products.` : 'No products yet.'}</p>
          <p className="status">{colour ? 'Try another colour or clear the filter.' : 'Add the first one with the form.'}</p>
        </div>
      ) : (
        <>
          <div className="table-scroll">
            <table>
              <caption className="visually-hidden">{colour ? `Products in ${colour}` : 'All products'}</caption>
              <thead>
                <tr>
                  <th scope="col">Name</th>
                  <th scope="col">Colour</th>
                  <th scope="col" className="numeric">
                    Price
                  </th>
                </tr>
              </thead>
              <tbody>
                {products.map(product => (
                  <tr key={product.id}>
                    <td>{product.name}</td>
                    <td>
                      <span className="swatch" style={{ backgroundColor: product.colour }} aria-hidden="true" />
                      {product.colour}
                    </td>
                    <td className="numeric">{priceFormat.format(product.price)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="list-footer">
            <span className="status" role="status">
              Showing {products.length} {products.length === 1 ? 'product' : 'products'}
            </span>
            {hasNextPage && (
              <button type="button" className="button" disabled={isFetchingNextPage} onClick={() => void fetchNextPage()}>
                {isFetchingNextPage ? 'Loading…' : 'Load more'}
              </button>
            )}
          </div>
        </>
      )}
    </section>
  )
}
