import { CurrencyPipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { Product } from '../../data/catalog.models';

/**
 * Componente de apresentação (dumb — ADR-25): só exibe e emite eventos.
 * Não injeta serviço, não chama API, não navega. Fácil de reaproveitar e testar.
 */
@Component({
  selector: 'app-product-table',
  imports: [CurrencyPipe],
  templateUrl: './product-table.html',
})
export class ProductTable {
  readonly products = input.required<Product[]>();
  readonly edit = output<Product>();
  readonly remove = output<Product>();
}
