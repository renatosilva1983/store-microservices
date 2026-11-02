import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  Category,
  CreateProductRequest,
  PagedResult,
  Product,
  ProductFilter,
  ProductListItem,
  UpdateProductRequest,
} from './catalog.models';

/** Consulta GraphQL da listagem: o front escolhe exatamente os campos que precisa. */
const PRODUCTS_QUERY = `
  query Products($name: String, $categoryId: UUID, $page: Int!, $pageSize: Int!) {
    products(name: $name, categoryId: $categoryId, page: $page, pageSize: $pageSize) {
      page pageSize totalCount totalPages
      items { id name description sku price currency stockQuantity categoryName }
    }
  }`;

interface GraphQLResponse<T> {
  data?: T;
  errors?: { message: string; extensions?: { code?: string } }[];
}

interface ProductsPage {
  items: ProductListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

/**
 * Acesso HTTP ao Catálogo, sempre pelo Gateway (/api/catalog/...).
 * Leitura da listagem via GraphQL; escrita e detalhe via REST (ADR-32).
 * Só faz chamadas: não guarda estado nem decide nada de tela.
 */
@Injectable({ providedIn: 'root' })
export class CatalogApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/catalog`;

  listProducts(filter: ProductFilter): Observable<PagedResult<ProductListItem>> {
    const variables = {
      name: filter.name.trim() || null,
      categoryId: filter.categoryId || null,
      page: filter.page,
      pageSize: filter.pageSize,
    };

    return this.http
      .post<GraphQLResponse<{ products: ProductsPage }>>(`${this.baseUrl}/graphql`, { query: PRODUCTS_QUERY, variables })
      .pipe(
        map((response) => {
          // GraphQL responde 200 mesmo com erro: o erro vem no array "errors".
          if (response.errors?.length || !response.data) {
            throw new Error(response.errors?.[0]?.message ?? 'Erro na consulta GraphQL.');
          }
          const page = response.data.products;
          return {
            ...page,
            hasPreviousPage: page.page > 1,
            hasNextPage: page.page < page.totalPages,
          };
        }),
      );
  }

  getProduct(id: string): Observable<Product> {
    return this.http.get<Product>(`${this.baseUrl}/products/${id}`);
  }

  createProduct(request: CreateProductRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.baseUrl}/products`, request);
  }

  updateProduct(id: string, request: UpdateProductRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/products/${id}`, request);
  }

  deleteProduct(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/products/${id}`);
  }

  listCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.baseUrl}/categories`);
  }
}
