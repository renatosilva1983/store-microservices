import { Component, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { NotificationService } from '../../../../core/notifications/notification.service';
import { Pagination } from '../../../../shared/ui/pagination';
import { ProductTable } from '../../components/product-table/product-table';
import { Product } from '../../data/catalog.models';
import { ProductsFacade } from '../../data/products.facade';

/**
 * Página (smart): conhece o Facade, coordena os componentes de apresentação e navega.
 */
@Component({
  selector: 'app-product-list',
  imports: [ReactiveFormsModule, RouterLink, ProductTable, Pagination],
  templateUrl: './product-list.html',
})
export class ProductList implements OnInit {
  protected readonly facade = inject(ProductsFacade);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);

  protected readonly search = new FormControl(this.facade.filter().name, { nonNullable: true });

  constructor() {
    // debounceTime: espera o usuário parar de digitar (300 ms) antes de buscar.
    this.search.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((name) => this.facade.search(name));
  }

  ngOnInit(): void {
    this.facade.loadCategories();
    this.facade.load();
  }

  protected onCategoryChange(event: Event): void {
    this.facade.filterByCategory((event.target as HTMLSelectElement).value);
  }

  protected onEdit(product: Product): void {
    this.router.navigate(['/products', product.id, 'edit']);
  }

  protected onRemove(product: Product): void {
    if (!confirm(`Excluir "${product.name}"? Essa ação não pode ser desfeita.`)) {
      return;
    }

    // O erro já é exibido pelo interceptor; aqui só tratamos o sucesso.
    this.facade.delete(product).subscribe({
      next: () => this.notifications.success('Produto excluído.'),
      error: () => undefined,
    });
  }
}
