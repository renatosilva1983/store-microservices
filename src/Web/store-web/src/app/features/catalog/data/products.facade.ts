import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Observable, Subject, catchError, finalize, switchMap, tap } from 'rxjs';
import { CatalogApi } from './catalog-api';
import { Category, PagedResult, Product, ProductFilter } from './catalog.models';

/**
 * Facade (ADR-25): a página conversa com este serviço, nunca com o HttpClient.
 * Guarda o estado da listagem em signals e esconde as chamadas HTTP.
 *
 * Fornecido na rota (não em 'root'): nasce e morre junto com a tela de produtos.
 */
@Injectable()
export class ProductsFacade {
  private readonly api = inject(CatalogApi);
  private readonly destroyRef = inject(DestroyRef);

  // Estado privado e gravável; a tela só enxerga as versões somente leitura.
  private readonly _filter = signal<ProductFilter>({ name: '', categoryId: '', page: 1, pageSize: 10 });
  private readonly _result = signal<PagedResult<Product> | null>(null);
  private readonly _categories = signal<Category[]>([]);
  private readonly _loading = signal(false);

  readonly filter = this._filter.asReadonly();
  readonly categories = this._categories.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly products = computed(() => this._result()?.items ?? []);
  readonly page = computed(() => this._result()?.page ?? 1);
  readonly totalPages = computed(() => this._result()?.totalPages ?? 0);
  readonly totalCount = computed(() => this._result()?.totalCount ?? 0);

  // switchMap: se o usuário digitar rápido, a busca anterior é cancelada e só vale a última.
  private readonly reload$ = new Subject<void>();

  constructor() {
    this.reload$
      .pipe(
        tap(() => this._loading.set(true)),
        switchMap(() =>
          this.api.listProducts(this._filter()).pipe(
            catchError(() => EMPTY),
            finalize(() => this._loading.set(false)),
          ),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => this._result.set(result));
  }

  load(): void {
    this.reload$.next();
  }

  loadCategories(): void {
    this.api
      .listCategories()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((categories) => this._categories.set(categories));
  }

  search(name: string): void {
    this._filter.update((f) => ({ ...f, name, page: 1 }));
    this.load();
  }

  filterByCategory(categoryId: string): void {
    this._filter.update((f) => ({ ...f, categoryId, page: 1 }));
    this.load();
  }

  goToPage(page: number): void {
    this._filter.update((f) => ({ ...f, page }));
    this.load();
  }

  delete(product: Product): Observable<void> {
    return this.api.deleteProduct(product.id).pipe(
      tap(() => {
        // Se apagou o último item da página, volta uma página.
        const backOnePage = this.products().length === 1 && this._filter().page > 1;
        if (backOnePage) {
          this._filter.update((f) => ({ ...f, page: f.page - 1 }));
        }
        this.load();
      }),
    );
  }
}
