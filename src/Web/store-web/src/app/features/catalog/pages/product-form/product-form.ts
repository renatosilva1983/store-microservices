import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { AbstractControl, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { toProblemDetails } from '../../../../core/errors/problem-details';
import { NotificationService } from '../../../../core/notifications/notification.service';
import { CatalogApi } from '../../data/catalog-api';
import { Category, Product } from '../../data/catalog.models';

/** Mesma regra do value object Sku no back-end. */
const SKU_PATTERN = /^[A-Za-z0-9-]{3,20}$/;

/**
 * Formulário reativo tipado (ADR-25): o tipo de cada campo é inferido do valor inicial.
 * A mesma tela cria e edita; o id vem da rota por input() (withComponentInputBinding).
 */
@Component({
  selector: 'app-product-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './product-form.html',
})
export class ProductForm implements OnInit {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly api = inject(CatalogApi);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);

  /** Preenchido pela rota ':id/edit'; vazio em 'new'. */
  readonly id = input<string>();

  protected readonly isEdit = computed(() => !!this.id());
  protected readonly categories = signal<Category[]>([]);
  protected readonly saving = signal(false);

  protected readonly form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    description: ['', [Validators.maxLength(1000)]],
    sku: ['', [Validators.required, Validators.pattern(SKU_PATTERN)]],
    price: [0, [Validators.required, Validators.min(0)]],
    initialStock: [0, [Validators.required, Validators.min(0)]],
    categoryId: ['', [Validators.required]],
  });

  ngOnInit(): void {
    this.api.listCategories().subscribe((categories) => this.categories.set(categories));

    const id = this.id();
    if (id) {
      // SKU e estoque inicial não se editam por aqui (regra do back-end).
      this.form.controls.sku.disable();
      this.form.controls.initialStock.disable();
      this.api.getProduct(id).subscribe((product) => this.fill(product));
    }
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const description = value.description.trim() || null;
    const id = this.id();

    const request$: Observable<unknown> = id
      ? this.api.updateProduct(id, {
          name: value.name,
          description,
          price: value.price,
          categoryId: value.categoryId,
        })
      : this.api.createProduct({
          name: value.name,
          description,
          sku: value.sku,
          price: value.price,
          initialStock: value.initialStock,
          categoryId: value.categoryId,
        });

    this.saving.set(true);
    request$.subscribe({
      next: () => {
        this.notifications.success(id ? 'Produto atualizado.' : 'Produto criado.');
        this.router.navigate(['/products']);
      },
      error: (error: HttpErrorResponse) => {
        this.saving.set(false);
        this.applyServerErrors(error);
      },
    });
  }

  protected hasError(control: AbstractControl, error?: string): boolean {
    const touched = control.touched || control.dirty;
    return touched && (error ? control.hasError(error) : control.invalid);
  }

  protected serverError(control: AbstractControl): string | null {
    const value = control.getError('server');
    return typeof value === 'string' ? value : null;
  }

  private fill(product: Product): void {
    this.form.patchValue({
      name: product.name,
      description: product.description ?? '',
      sku: product.sku,
      price: product.price,
      initialStock: product.stockQuantity,
      categoryId: product.categoryId,
    });
  }

  /** Erros de validação do back (400, campo → mensagens) aparecem no próprio campo. */
  private applyServerErrors(error: HttpErrorResponse): void {
    const errors = toProblemDetails(error)?.errors;
    if (!errors) {
      return;
    }

    for (const [field, messages] of Object.entries(errors)) {
      const name = field.charAt(0).toLowerCase() + field.slice(1);
      const control = this.form.get(name);
      control?.setErrors({ server: messages[0] });
      control?.markAsTouched();
    }
  }
}
