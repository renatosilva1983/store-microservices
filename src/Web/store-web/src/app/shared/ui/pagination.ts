import { Component, computed, input, output } from '@angular/core';

/**
 * Componente de apresentação (dumb): recebe dados por input(), avisa por output().
 * Não sabe de onde vêm os dados nem o que acontece ao trocar de página.
 */
@Component({
  selector: 'app-pagination',
  template: `
    @if (totalPages() > 1) {
      <nav class="pagination" aria-label="Paginação">
        <button type="button" [disabled]="page() <= 1" (click)="pageChange.emit(page() - 1)">Anterior</button>
        <span>Página {{ page() }} de {{ totalPages() }} · {{ totalCount() }} itens</span>
        <button type="button" [disabled]="!hasNext()" (click)="pageChange.emit(page() + 1)">Próxima</button>
      </nav>
    }
  `,
})
export class Pagination {
  readonly page = input.required<number>();
  readonly totalPages = input.required<number>();
  readonly totalCount = input<number>(0);
  readonly pageChange = output<number>();

  protected readonly hasNext = computed(() => this.page() < this.totalPages());
}
