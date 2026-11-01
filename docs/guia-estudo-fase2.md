# Guia de estudo — Fase 2 (Gateway, Ordering e Angular)

Mesmo formato da Fase 1: por onde a requisição passa, onde colocar breakpoints e qual pergunta de entrevista cada parte responde.

## Antes de começar

1. Gere a migration do Ordering (uma vez):
   ```powershell
   dotnet ef migrations add InitialCreate -p src/Services/Ordering/Ordering.Infrastructure -s src/Services/Ordering/Ordering.Api -o Persistence/Migrations
   ```
2. Suba só os bancos: `docker compose up -d catalog-db ordering-db`
3. Back-end: abra `store-server.slnx`, escolha o perfil **"Back-end (Gateway + Catalog + Ordering)"** ao lado do botão Iniciar e F5. Sobem os três; dá para colocar breakpoint em qualquer um.
4. Front: abra `src/Web/store-web/store-web.slnx` em outra janela do Visual Studio e F5 (na primeira vez o build faz o `npm install`). Os breakpoints nos `.ts` funcionam no próprio VS.

| Processo | Endereço |
|---|---|
| Store.Gateway | http://localhost:5000 |
| Catalog.Api | http://localhost:5086/scalar |
| Ordering.Api | http://localhost:5087/scalar |
| Angular | http://localhost:4200 |

## Fluxo 1: o front lista produtos pelo Gateway

```
Angular (ProductList) → ProductsFacade.load() → CatalogApi.listProducts()
  → HTTP GET http://localhost:5000/api/catalog/products?page=1
     → Store.Gateway (YARP): rota "catalog", transform /api/catalog/{**} → /api/{**}
        → Catalog.Api GET /api/products  (Fase 1)
  ← JSON → signal _result → computed products() → ProductTable redesenha
```

| # | Onde | O que observar |
|---|---|---|
| 1 | Navegador, DevTools → Network | A chamada vai para a porta **5000** (Gateway), não para 5086. O front não sabe onde o Catálogo está |
| 2 | `Store.Gateway/appsettings.json` | A rota `catalog` e o `PathPattern`: o Gateway esconde as rotas internas |
| 3 | `Catalog.Api` → `ProductsController.List` | A requisição chega com `/api/products`, já reescrita |
| 4 | DevTools → Sources → `products.facade.ts`, no `switchMap` | Digite rápido na busca: o `debounceTime` segura 300 ms e o `switchMap` cancela a busca anterior (requisição *canceled* no Network) |

## Fluxo 2: criar pedido (Ordering chama o Catálogo)

Use `Ordering.Api.http` (pegue um `productId` real antes).

```
POST /api/orders → OrdersController.Create
  → LoggingDecorator → ValidationDecorator → CreateOrderCommandHandler
       → IProductCatalog.GetProductAsync ──► CatalogHttpAdapter (Infrastructure)
             → HttpClient (IHttpClientFactory + Polly: retry, circuit breaker, timeout)
                → GET http://localhost:5086/api/products/{id}
             ← traduz o JSON do Catálogo para CatalogProduct
       → Order.Create / AddItem   (preço COPIADO do catálogo para o item)
       → IShippingCalculator ──► ShippingCalculator escolhe a IShippingStrategy
       → Order.SetShipping → IOrderRepository.Add → IUnitOfWork.SaveChangesAsync
← 201 Created
```

| # | Onde | O que observar |
|---|---|---|
| 1 | `CreateOrderCommandHandler.Handle` | `catalog` é a interface; no *Locals* o tipo real é `CatalogHttpAdapter` |
| 2 | `CatalogHttpAdapter.GetProductAsync` | O DTO privado do Catálogo vira `CatalogProduct`: é a camada anticorrupção |
| 3 | `Catalog.Api` → `ProductsController.GetById` | O breakpoint para no **outro serviço**: comunicação síncrona entre microsserviços |
| 4 | `ShippingCalculator.Calculate` | O dicionário de estratégias, montado a partir de `IEnumerable<IShippingStrategy>` injetado |
| 5 | `Order.AddItem` / `RecalculateTotals` | O agregado mantém os totais coerentes; ninguém de fora altera `Subtotal` |
| 6 | `OrderConfiguration` | `OwnsMany`: os itens ficam em `order_items`, mas são salvos e lidos junto com o pedido |

### Experimento: circuit breaker
1. Pare o `Catalog.Api` (ou `docker stop catalog-api`).
2. Envie o POST de pedido umas 6 vezes.
3. No console do `Ordering.Api`: primeiro aparecem as novas tentativas (*retry*); depois o log **OnCircuitOpened** e as respostas viram **503 imediato**: o Ordering para de insistir num serviço fora do ar.
4. Suba o Catálogo de novo e espere ~15 s (*BreakDuration*): o circuito passa a meio-aberto e volta a funcionar.

## Fluxo 3: formulário do produto (Angular)

| # | Onde | O que observar |
|---|---|---|
| 1 | `catalog.routes.ts` | `:id/edit` + `withComponentInputBinding()` → o id chega como `input()` no `ProductForm` |
| 2 | `product-form.ts` → `form` | Formulário tipado: `form.getRawValue().price` já é `number` |
| 3 | Envie SKU repetido | O back devolve 409; o `errorInterceptor` mostra o toast |
| 4 | Remova o `Validators.required` do nome e salve vazio | O back devolve 400 com `errors`; `applyServerErrors` marca o campo |

## Mapa: arquivo → padrão → pergunta de entrevista

| Arquivo | Padrão (ADR) | Pergunta |
|---|---|---|
| `Store.Gateway/*` | API Gateway, YARP (04) | "Por que o front não chama os serviços direto?" |
| `Directory.Packages.props` | Central Package Management | "Como manter as versões iguais em vários serviços?" |
| `BuildingBlocks.Api/ApiController.cs` | Building blocks técnicos | "O que pode ser compartilhado entre microsserviços e o que não pode?" |
| `Ordering.Domain/Orders/Order.cs` | Agregado, invariantes (11) | "O que é raiz de agregado? Por que `OrderItem` não tem repositório?" |
| `OrderItem` (preço copiado) | Bounded context (01) | "Se o preço mudar no Catálogo, o pedido muda?" |
| `Shipping/*` | Strategy + Factory (15), Open/Closed | "Como adicionar um frete novo sem mexer nos existentes?" |
| `ShippingCalculator` | DI com `IEnumerable<T>` | "Por que não Keyed Services / Service Locator?" |
| `IProductCatalog` + `CatalogHttpAdapter` | Ports and Adapters, ACL (16) | "Como o Pedidos fala com o Catálogo sem acoplar ao contrato dele?" |
| `CatalogClientRegistration` | Typed HttpClient, Polly (05), Options (19) | "Por que não `new HttpClient()`? O que é circuit breaker?" |
| `ErrorType.Unavailable` → 503 | Result Pattern (17) | "Como o cliente sabe que o problema é temporário?" |
| `OrdersController.Cancel` | REST orientado a ação | "Por que `POST /orders/{id}/cancel` e não `PUT` com status?" |
| `products.facade.ts` | Facade + signals (25) | "Onde fica o estado da tela? Por que não no componente?" |
| `product-table.ts` | Smart/Dumb (25) | "O que é componente de apresentação?" |
| `error.interceptor.ts` | Interceptor funcional | "Como tratar erro HTTP num lugar só?" |
| `app.config.ts` | Standalone + zoneless | "O que muda sem zone.js?" |

## Decisões que valem ser defendidas

- **Ordering valida estoque, mas não baixa**: a baixa acontece no Catálogo, por evento, na Fase 3. Aqui é só uma pré-checagem para responder rápido.
- **Preço vem do Catálogo, nunca do cliente**: o request só tem `productId` e `quantity`.
- **Cada serviço tem seu `Entity` e `DomainException`**: domínio não se compartilha entre bounded contexts; só o técnico (`BuildingBlocks`).
- **Valores do circuit breaker reduzidos** (5 requisições, 50% de falha) só para conseguir ver o circuito abrir num teste manual. Em produção, os padrões do `AddStandardResilienceHandler` são mais tolerantes.
- **Gateway sem regra de negócio**: só roteamento e CORS. Autenticação entra na Fase 3.
