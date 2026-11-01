# Guia de estudo — Fase 1 (Catalog.Api)

Roteiro para estudar o código **debugando**. Para cada etapa: onde colocar o breakpoint, o que observar e a pergunta de entrevista que aquela parte responde.

## Antes de começar

1. Suba só o banco: `docker compose up -d catalog-db`
2. Abra a solution no Visual Studio, defina `Catalog.Api` como projeto de inicialização e rode com **F5** (perfil `https`). O navegador abre o **Scalar** (documentação interativa da API).
3. Na primeira execução, as migrations são aplicadas e as 3 categorias do seed são criadas.
4. Use o arquivo `Catalog.Api.http` (botão "Send request" no VS) ou o Scalar para disparar as requisições.

## O caminho de uma requisição: `POST /api/products`

```
HTTP → ProductsController.Create
     → LoggingDecorator      (log de início, cronômetro)
     → ValidationDecorator   (FluentValidation; se falhar, volta 400 sem chegar ao handler)
     → CreateProductCommandHandler
         → ICategoryRepository.ExistsAsync   ──► CategoryRepository (EF Core)
         → IProductRepository.SkuExistsAsync ──► ProductRepository  (EF Core)
         → Product.Create (domínio: regras e invariantes)
         → IUnitOfWork.SaveChangesAsync      ──► CatalogDbContext
     ← Result<Guid>
← ProductsController traduz: sucesso → 201 Created | erro → ProblemDetails (404/409/400)
```

### Breakpoints, na ordem

| # | Arquivo | Onde | O que observar |
|---|---|---|---|
| 1 | `Catalog.Api/Program.cs` | `builder.Services.AddApplication()` | Composition Root: cada camada se registra; a Api só chama |
| 2 | `BuildingBlocks.Application/DependencyInjection.cs` | `AddHandlerDecorators` | A ordem dos `TryDecorate` define quem fica por fora (log → validação → handler) |
| 3 | `Catalog.Api/Controllers/ProductsController.cs` | início de `Create` | Na janela *Locals*, o tipo real de `handler` é `LoggingDecorator.CommandHandler`, não o handler: é o Decorator em ação |
| 4 | `BuildingBlocks.Application/Behaviors/ValidationDecorator.cs` | `ValidateAsync` | Envie o request inválido do `.http` e veja o retorno antes de chegar ao handler |
| 5 | `Catalog.Application/Products/Create/CreateProductCommandHandler.cs` | primeira linha de `Handle` | `products` e `categories` são interfaces; passe o mouse para ver a classe concreta injetada (Infrastructure) |
| 6 | `Catalog.Domain/Products/Product.cs` | `Create` | O modelo rico: estado só muda por métodos; setters são privados |
| 7 | `Catalog.Infrastructure/Persistence/Repositories/ProductRepository.cs` | `SkuExistsAsync` | Com o log do EF em `Information` (Development), veja o SQL gerado no Output |
| 8 | `Catalog.Api/Controllers/ApiController.cs` | `Problem(Error)` | Envie o SKU repetido: o `ErrorType.Conflict` vira 409 num lugar só |

### Outros fluxos para debugar

- **Listagem paginada** (`GET /api/products?name=note`): breakpoint em `CatalogQueries.ListProductsAsync`. Repare que não passa por repositório nem entidade: é o lado de leitura do CQRS, com `AsNoTracking` e projeção direto para DTO.
- **Excluir categoria com produtos** (`DELETE /api/categories/{id}`): `DeleteCategoryCommandHandler` devolve `Category.HasProducts` → 409.
- **Invariante do domínio**: chame `product.DebitStock(999)` num teste e veja a `DomainException`; na API ela vira 400 pelo `GlobalExceptionHandler`.
- **Testes**: abra o *Test Explorer*, rode `CreateProductCommandHandlerTests` com debug. Os repositórios são mocks (NSubstitute): o caso de uso roda sem banco.

## Mapa: arquivo → padrão → pergunta de entrevista

| Arquivo | Padrão (ADR) | Pergunta |
|---|---|---|
| `Results/Result.cs`, `Error.cs` | Result Pattern (17) | "Por que não lançar exceção para regra de negócio?" |
| `Messaging/I*Handler.cs` | CQRS (12) | "Qual a diferença entre comando e consulta? Por que não usou MediatR?" |
| `Behaviors/*Decorator.cs` | Decorator (13), Open/Closed | "Como adicionar log em todos os casos de uso sem mexer em nenhum?" |
| `Domain/ValueObjects/Money.cs`, `Sku.cs` | Value Object (11) | "Qual a diferença entre entidade e value object?" |
| `Domain/Products/Product.cs` | Entidade rica, Agregado (11) | "Por que os setters são privados? Por que só `CategoryId` e não a `Category`?" |
| `Domain/Common/DomainException.cs` | Invariantes | "Onde fica validação de formato e onde fica regra de negócio?" |
| `Application/Abstractions/*` | Ports and Adapters, DIP | "Como a Application chama o banco sem referenciar a Infrastructure?" |
| `Application/Abstractions/ICatalogQueries.cs` | CQRS (lado de leitura) | "Por que a leitura não usa o repositório?" |
| `Infrastructure/Persistence/CatalogDbContext.cs` | Unit of Work (14) | "O DbContext já não é um Unit of Work?" |
| `Infrastructure/Persistence/Configurations/*` | Mapeamento fora do domínio | "Como o domínio fica livre do EF Core?" |
| `Infrastructure/DependencyInjection.cs`, `Program.cs` | DI por camada (20), Composition Root | "Por que a Api referencia a Infrastructure?" |
| `Api/Controllers/*` | MVC, controllers finos (21) | "O que um controller deve e não deve fazer?" |
| `Api/ErrorHandling/GlobalExceptionHandler.cs` | ProblemDetails (17) | "Como você padroniza erros para o front?" |
| `Dockerfile`, `docker-compose.yml` | Multi-stage, compose (09) | "Por que multi-stage? Por que copiar os .csproj antes do código?" |

## Decisões que valem ser defendidas

- **Domain sem dependências**: por isso usa `DomainException` para invariantes, e não o `Result` do BuildingBlocks. Erros esperados (não encontrado, SKU repetido) são `Result` na Application.
- **SKU imutável**: o update não altera o SKU; ele é a identidade comercial do produto.
- **Handlers via `[FromServices]` na action**: cada action recebe só o handler que usa, em vez de um construtor com dez dependências.
- **`TimeProvider` injetado**: o handler não chama `DateTime.UtcNow` direto; nos testes dá para controlar o tempo.
- **Request ≠ Command**: a API tem os próprios contratos (`Contracts/`), então pode evoluir sem mexer nos casos de uso.
