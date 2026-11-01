// Contratos da API do Catálogo, do jeito que chegam pelo Gateway (JSON em camelCase).

export interface Product {
  id: string;
  name: string;
  description: string | null;
  sku: string;
  price: number;
  currency: string;
  stockQuantity: number;
  categoryId: string;
  categoryName: string;
  createdAtUtc: string;
}

export interface Category {
  id: string;
  name: string;
  description: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface ProductFilter {
  name: string;
  categoryId: string;
  page: number;
  pageSize: number;
}

export interface CreateProductRequest {
  name: string;
  description: string | null;
  sku: string;
  price: number;
  initialStock: number;
  categoryId: string;
}

export interface UpdateProductRequest {
  name: string;
  description: string | null;
  price: number;
  categoryId: string;
}
