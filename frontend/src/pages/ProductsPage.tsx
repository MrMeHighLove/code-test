import { useEffect, useState } from 'react';
import type { ComponentProps } from 'react';
import { useNavigate } from 'react-router-dom';
import { PRODUCT_PAGE, ROUTES } from '../constants/app';
import { useAuth } from '../context/useAuth';
import type { Product } from '../api/products';
import { getProducts, createProduct } from '../api/products';

const COLOURS = ['Red', 'Blue', 'Green', 'Yellow', 'Black', 'White', 'Purple', 'Orange'];

export default function ProductsPage() {
  const [products, setProducts] = useState<Product[]>([]);
  const [colourFilter, setColourFilter] = useState('');
  const [page, setPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [price, setPrice] = useState('');
  const [colour, setColour] = useState('Red');
  const [error, setError] = useState('');
  const { logout } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    const controller = new AbortController();
    let ignore = false;

    const loadProducts = async () => {
      try {
        const { data } = await getProducts({
          colour: colourFilter || undefined,
          page,
          pageSize: PRODUCT_PAGE.pageSize,
        }, controller.signal);

        if (!ignore) {
          setProducts(data.items);
          setTotalItems(data.totalItems);
          setTotalPages(data.totalPages);

          if (data.totalPages > 0 && data.page > data.totalPages) {
            setPage(data.totalPages);
          }
        }
      } catch (err) {
        if (controller.signal.aborted) {
          return;
        }

        console.error(err);
        navigate(ROUTES.login);
      }
    };

    void loadProducts();
    return () => {
      ignore = true;
      controller.abort();
    };
  }, [colourFilter, page, navigate]);

  const handleCreate: NonNullable<ComponentProps<'form'>['onSubmit']> = async e => {
    e.preventDefault();
    setError('');
    try {
      await createProduct({
        name,
        description: description || undefined,
        price: parseFloat(price),
        colour,
      });
      setName('');
      setDescription('');
      setPrice('');
      const { data } = await getProducts({
        colour: colourFilter || undefined,
        page,
        pageSize: PRODUCT_PAGE.pageSize,
      });

      setProducts(data.items);
      setTotalItems(data.totalItems);
      setTotalPages(data.totalPages);

      if (data.totalPages > 0 && data.page > data.totalPages) {
        setPage(data.totalPages);
      }
    } catch (err: unknown) {
      const message = (err as { response?: { data?: { title?: string } } })
        ?.response?.data?.title || PRODUCT_PAGE.errors.createFailed;
      setError(message);
    }
  };

  const handleLogout = async () => {
    await logout();
    navigate(ROUTES.login);
  };

  const handleColourFilterChange: NonNullable<ComponentProps<'select'>['onChange']> = e => {
    setColourFilter(e.target.value);
    setPage(1);
  };

  const pageNumbers = getVisiblePages(page, totalPages);

  return (
    <div className="products-container">
      <header>
        <h1>{PRODUCT_PAGE.labels.title}</h1>
        <button onClick={handleLogout} className="logout-btn">{PRODUCT_PAGE.labels.logout}</button>
      </header>

      <section className="create-product">
        <h2>{PRODUCT_PAGE.labels.addProduct}</h2>
        {error && <p className="error">{error}</p>}
        <form onSubmit={handleCreate}>
          <input placeholder={PRODUCT_PAGE.placeholders.name} value={name} onChange={e => setName(e.target.value)} required />
          <input placeholder={PRODUCT_PAGE.placeholders.description} value={description} onChange={e => setDescription(e.target.value)} />
          <input type="number" step="0.01" min="0.01" placeholder={PRODUCT_PAGE.placeholders.price} value={price} onChange={e => setPrice(e.target.value)} required />
          <select value={colour} onChange={e => setColour(e.target.value)}>
            {COLOURS.map(c => <option key={c} value={c}>{c}</option>)}
          </select>
          <button type="submit">{PRODUCT_PAGE.labels.add}</button>
        </form>
      </section>

      <section className="product-list">
        <h2>{PRODUCT_PAGE.labels.allProducts}</h2>
        <div className="filter">
          <label>{PRODUCT_PAGE.labels.filterByColour} </label>
          <select value={colourFilter} onChange={handleColourFilterChange}>
            <option value={PRODUCT_PAGE.allColoursValue}>All</option>
            {COLOURS.map(c => <option key={c} value={c}>{c}</option>)}
          </select>
        </div>
        <div className="list-summary">
          <span>{PRODUCT_PAGE.labels.resultsSummary(totalItems)}</span>
        </div>
        <table>
          <thead>
            <tr>
              <th>Name</th>
              <th>Description</th>
              <th>Price</th>
              <th>Colour</th>
              <th>Created</th>
            </tr>
          </thead>
          <tbody>
            {products.map(p => (
              <tr key={p.id}>
                <td>{p.name}</td>
                <td>{p.description || '-'}</td>
                <td>${p.price.toFixed(2)}</td>
                <td>
                  <span className="colour-badge" style={getBadgeStyle(p.colour)}>
                    {p.colour}
                  </span>
                </td>
                <td>{new Date(p.createdAt).toLocaleDateString()}</td>
              </tr>
            ))}
            {products.length === 0 && (
              <tr><td colSpan={5} style={{ textAlign: 'center' }}>{PRODUCT_PAGE.labels.noProductsFound}</td></tr>
            )}
          </tbody>
        </table>
        <div className="pagination-footer">
          <span className="pagination-meta">
            {PRODUCT_PAGE.labels.pageSummary(Math.max(totalPages === 0 ? 0 : page, 0), totalPages)}
          </span>
          <div className="pagination-controls" aria-label="Pagination">
            <button
              type="button"
              className="pagination-button pagination-arrow"
              onClick={() => setPage(current => Math.max(1, current - 1))}
              disabled={page <= 1}
            >
              {PRODUCT_PAGE.labels.previous}
            </button>
            {pageNumbers.map(pageNumber => (
              <button
                key={pageNumber}
                type="button"
                className={`pagination-button ${pageNumber === page ? 'is-active' : ''}`}
                onClick={() => setPage(pageNumber)}
                disabled={pageNumber === page}
                aria-current={pageNumber === page ? 'page' : undefined}
              >
                {pageNumber}
              </button>
            ))}
            <button
              type="button"
              className="pagination-button pagination-arrow"
              onClick={() => setPage(current => Math.min(totalPages, current + 1))}
              disabled={totalPages === 0 || page >= totalPages}
            >
              {PRODUCT_PAGE.labels.next}
            </button>
          </div>
        </div>
      </section>
    </div>
  );
}

function getVisiblePages(currentPage: number, totalPages: number) {
  if (totalPages <= 0) {
    return [];
  }

  const start = Math.max(1, currentPage - 2);
  const end = Math.min(totalPages, currentPage + 2);
  const pages: number[] = [];

  for (let page = start; page <= end; page += 1) {
    pages.push(page);
  }

  return pages;
}

function getBadgeStyle(colour: string) {
  const normalizedColour = colour.toLowerCase();
  const darkTextColours = new Set(['white', 'yellow', 'orange', 'green']);

  return {
    backgroundColor: normalizedColour,
    color: darkTextColours.has(normalizedColour) ? '#0f172a' : '#f8fafc',
    borderColor: darkTextColours.has(normalizedColour) ? 'rgba(15, 23, 42, 0.12)' : 'rgba(248, 250, 252, 0.24)',
  };
}
