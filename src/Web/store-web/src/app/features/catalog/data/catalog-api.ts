import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  Category,
  CreateProductRequest,
  PagedResult,
  Product,
  ProductFilter,
  UpdateProductRequest,
} from './catalog.models';

/**
 * Acesso HTTP ao Catálogo, sempre pelo Gateway (/api/catalog/...).
 * Só faz chamadas: não guarda estado nem decide nada de tela.
 */
@Injectable({ providedIn: 'root' })
export class CatalogApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/catalog`;

  listProducts(filter: ProductFilter): Observable<PagedResult<Product>> {
    let params = new HttpParams().set('page', filter.page).set('pageSize', filter.pageSize);
    if (filter.name.trim()) {
      params = params.set('name', filter.name.trim());
    }
    if (filter.categoryId) {
      params = params.set('categoryId', filter.categoryId);
    }
    return this.http.get<PagedResult<Product>>(`${this.baseUrl}/products`, { params });
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
