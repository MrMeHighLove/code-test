import api from './client';

export interface Product {
  id: string;
  name: string;
  description: string | null;
  price: number;
  colour: string;
  createdAt: string;
}

export interface PagedProductsResponse {
  items: Product[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface CreateProductPayload {
  name: string;
  description?: string;
  price: number;
  colour: string;
}

export const getProducts = (
  params: { colour?: string; page?: number; pageSize?: number },
  signal?: AbortSignal
) =>
  api.get<PagedProductsResponse>('/api/products', { params, signal });

export const createProduct = (data: CreateProductPayload) =>
  api.post<Product>('/api/products', data);
