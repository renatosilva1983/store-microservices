# Guia do código

Onde cada recurso do projeto está aplicado, com o arquivo para abrir e o que observar nele. O porquê de cada escolha está nas [decisões de arquitetura](decisoes-arquitetura.md) (ADRs); como rodar, no [README](../README.md).

Os caminhos seguem a mesma estrutura em todos os serviços: `Domain` (regras), `Application` (casos de uso), `Infrastructure` (banco, HTTP, mensageria) e `Api` (controllers e configuração).

## Arquitetura

| Recurso | Onde está | O que observar |
| --- | --- | --- |
| Microsserviços por contexto de negócio | [`src/Services`](../src/Services/) | Catálogo, Pedidos e Identidade, cada um com seu banco (ADR-01, ADR-03) |
| Clean Architecture | [`Ordering`](../src/Services/Ordering/) | Quatro projetos por serviço; as referências nos `.csproj` só apontam para dentro (ADR-02) |
| Composition Root | [`Ordering.Api/Program.cs`](../src/Services/Ordering/Ordering.Api/Program.cs) | Cada camada se registra com uma linha (`AddApplication`, `AddInfrastructure`) |
| Ports and Adapters | [`IOrderRepository.cs`](../src/Services/Ordering/Ordering.Application/Abstractions/IOrderRepository.cs) → [`OrderRepository.cs`](../src/Services/Ordering/Ordering.Infrastructure/Persistence/Repositories/OrderRepository.cs) | A Application define a porta; a Infrastructure implementa |
| Pastas por funcionalidade | [`Ordering.Application/Orders`](../src/Services/Ordering/Ordering.Application/Orders/) | `Create`, `Cancel`, `Queries`, `Stock`: um caso de uso por pasta |
| Building blocks | [`BuildingBlocks`](../src/BuildingBlocks/) | O que todos os serviços compartilham, sem regra de negócio |
| API Gateway (YARP) | [`Store.Gateway/appsettings.json`](../src/Gateway/Store.Gateway/appsettings.json) | Rotas e destinos em configuração; o Gateway não referencia nenhum serviço (ADR-04) |

## Domínio (DDD tático)

| Recurso | Onde está | O que observar |
| --- | --- | --- |
| Agregado e raiz | [`Order.cs`](../src/Services/Ordering/Ordering.Domain/Orders/Order.cs) | Itens só mudam pela raiz; invariantes protegidas (pedido cancelado não muda) |
| Entidade rica | [`Product.cs`](../src/Services/Catalog/Catalog.Domain/Products/Product.cs) | Setters privados; métodos de negócio (`ChangePrice`, `DebitStock`) |
| Value objects | [`Money.cs`](../src/Services/Catalog/Catalog.Domain/ValueObjects/Money.cs), [`Sku.cs`](../src/Services/Catalog/Catalog.Domain/ValueObjects/Sku.cs) | Imutáveis, comparados por valor, validados na criação |
| Factory method | `Order.Create`, `Product.Create` | Só se cria o objeto em estado válido |
| Cópia de dados entre contextos | [`OrderItem.cs`](../src/Services/Ordering/Ordering.Domain/Orders/OrderItem.cs) | O pedido guarda nome, SKU e preço do momento da compra |

## Casos de uso e padrões de projeto

| Recurso | Onde está | O que observar |
| --- | --- | --- |
| CQRS sem MediatR | [`Messaging`](../src/BuildingBlocks/BuildingBlocks.Application/Messaging/) | `ICommandHandler` e `IQueryHandler`; leitura e escrita separadas (ADR-12) |
| Leitura otimizada | [`Infrastructure/.../OrderQueries.cs`](../src/Services/Ordering/Ordering.Infrastructure/Persistence/Queries/OrderQueries.cs) | `AsNoTracking` e projeção só das colunas usadas |
| Decorator (validação e log) | [`ValidationDecorator.cs`](../src/BuildingBlocks/BuildingBlocks.Application/Behaviors/ValidationDecorator.cs), [`LoggingDecorator.cs`](../src/BuildingBlocks/BuildingBlocks.Application/Behaviors/LoggingDecorator.cs), [`DependencyInjection.cs`](../src/BuildingBlocks/BuildingBlocks.Application/DependencyInjection.cs) | Envolvem todo handler sem que ele saiba (ADR-13) |
| Validação | [`CreateProductCommandValidator.cs`](../src/Services/Catalog/Catalog.Application/Products/Create/CreateProductCommandValidator.cs) | FluentValidation, aplicada pelo decorator |
| Result Pattern | [`Result.cs`](../src/BuildingBlocks/BuildingBlocks.Application/Results/Result.cs), [`Error.cs`](../src/BuildingBlocks/BuildingBlocks.Application/Results/Error.cs) | Erro esperado como valor, sem exceção (ADR-17) |
| Repository e Unit of Work | [`IOrderRepository.cs`](../src/Services/Ordering/Ordering.Application/Abstractions/IOrderRepository.cs) | Um repositório por agregado; `SaveChangesAsync` numa transação |
| Strategy e Factory | [`ShippingStrategies.cs`](../src/Services/Ordering/Ordering.Application/Shipping/ShippingStrategies.cs), [`ShippingCalculator.cs`](../src/Services/Ordering/Ordering.Application/Shipping/ShippingCalculator.cs) | Três regras de frete; regra nova = classe nova; estratégias injetadas como `IEnumerable` (ADR-15) |
| Adapter / Anti-Corruption Layer | [`IProductCatalog.cs`](../src/Services/Ordering/Ordering.Application/Abstractions/IProductCatalog.cs) → [`CatalogHttpAdapter.cs`](../src/Services/Ordering/Ordering.Infrastructure/Catalog/CatalogHttpAdapter.cs) | O Pedidos não conhece o formato nem o HTTP do Catálogo (ADR-16) |
| Options Pattern | [`JwtOptions.cs`](../src/BuildingBlocks/BuildingBlocks.Api/Authentication/JwtOptions.cs), [`CatalogServiceOptions.cs`](../src/Services/Ordering/Ordering.Infrastructure/Catalog/CatalogServiceOptions.cs) | Configuração tipada, validada na subida |
| Injeção de dependência por camada | `DependencyInjection.cs` de cada projeto | Cada camada registra só o que é dela (ADR-20) |

## API (MVC)

| Recurso | Onde está | O que observar |
| --- | --- | --- |
| Controllers finos | [`OrdersController.cs`](../src/Services/Ordering/Ordering.Api/Controllers/OrdersController.cs) | Traduzem HTTP em comando e resultado em status; ação de negócio como endpoint (`/cancel`) |
| Result → ProblemDetails | [`ApiController.cs`](../src/BuildingBlocks/BuildingBlocks.Api/Controllers/ApiController.cs) | Tipo de erro vira status HTTP num lugar só |
| Tratamento global de exceção | [`GlobalExceptionHandler.cs`](../src/Services/Ordering/Ordering.Api/ErrorHandling/GlobalExceptionHandler.cs) | Sem stack trace na resposta |
| DTOs de entrada e saída | [`OrderRequests.cs`](../src/Services/Ordering/Ordering.Api/Contracts/OrderRequests.cs), [`ProductResponse.cs`](../src/Services/Catalog/Catalog.Application/Products/ProductResponse.cs) | A API nunca expõe a entidade |
| GraphQL para leitura | [`CatalogQuery.cs`](../src/Services/Catalog/Catalog.Api/GraphQL/CatalogQuery.cs) | Resolvers chamam os mesmos handlers do REST; profundidade limitada (ADR-32) |
| Exemplos de chamada | [`Ordering.Api.http`](../src/Services/Ordering/Ordering.Api/Ordering.Api.http), [`Store.Gateway.http`](../src/Gateway/Store.Gateway/Store.Gateway.http) | Roteiros prontos para executar no Visual Studio |

## Integração entre serviços

| Recurso | Onde está | O que observar |
| --- | --- | --- |
| HTTP com resiliência (Polly) | [`CatalogClientRegistration.cs`](../src/Services/Ordering/Ordering.Infrastructure/Catalog/CatalogClientRegistration.cs) | Timeout, retry e circuit breaker; falha vira 503 (ADR-05) |
| Contratos de integração | [`OrderCreatedIntegrationEvent.cs`](../src/BuildingBlocks/BuildingBlocks.Contracts/Orders/OrderCreatedIntegrationEvent.cs), [`StockIntegrationEvents.cs`](../src/BuildingBlocks/BuildingBlocks.Contracts/Catalog/StockIntegrationEvents.cs) | O que os serviços compartilham: só os eventos |
| Transactional Outbox | [`CreateOrderCommandHandler.cs`](../src/Services/Ordering/Ordering.Application/Orders/Create/CreateOrderCommandHandler.cs), [`MessagingRegistration.cs`](../src/BuildingBlocks/BuildingBlocks.Messaging/MessagingRegistration.cs) | Evento publicado antes do `SaveChanges`: gravado na mesma transação do pedido (ADR-06) |
| Consumidor e baixa de estoque | [`OrderCreatedConsumer.cs`](../src/Services/Catalog/Catalog.Infrastructure/Messaging/OrderCreatedConsumer.cs), [`ReserveStockForOrderCommand.cs`](../src/Services/Catalog/Catalog.Application/Products/ReserveStock/ReserveStockForOrderCommand.cs) | Tudo ou nada: valida todos os itens antes de debitar |
| Idempotência e consistência eventual | [`StockResponseCommands.cs`](../src/Services/Ordering/Ordering.Application/Orders/Stock/StockResponseCommands.cs) | Confirmar duas vezes não muda nada; o pedido vai de `Pending` a `Confirmed` ou `Cancelled` |
| Retry e fila de erro | [`MessagingRegistration.cs`](../src/BuildingBlocks/BuildingBlocks.Messaging/MessagingRegistration.cs) | 3 novas tentativas, depois a fila `_error` |

## Segurança

| Recurso | Onde está | O que observar |
| --- | --- | --- |
| JWT validado em cada serviço | [`AuthenticationRegistration.cs`](../src/BuildingBlocks/BuildingBlocks.Api/Authentication/AuthenticationRegistration.cs), [`TokenService.cs`](../src/Services/Identity/Identity.Api/Auth/TokenService.cs) | Assinatura, emissor, audiência e validade, sem chamar o Identity (ADR-07) |
| Proteção contra BOLA/IDOR | [`OrderAccess.cs`](../src/Services/Ordering/Ordering.Application/Orders/OrderAccess.cs), [`ICurrentUser.cs`](../src/BuildingBlocks/BuildingBlocks.Application/Security/ICurrentUser.cs) | O dono vem do token; pedido de outro usuário responde 404 (ADR-42) |
| Login sem vazar informação | [`AuthController.cs`](../src/Services/Identity/Identity.Api/Controllers/AuthController.cs) | Comparação em tempo constante; a mesma mensagem para e-mail e senha errados |
| Rate limiting e headers | [`GatewaySecurity.cs`](../src/Gateway/Store.Gateway/Security/GatewaySecurity.cs) | 5 logins por minuto por IP; headers de segurança; sem headers que revelam a tecnologia (ADR-41) |
| Segurança no pipeline | [`ci.yml`](../.github/workflows/ci.yml), [`codeql.yml`](../.github/workflows/codeql.yml), [`dependabot.yml`](../.github/dependabot.yml) | Dependências vulneráveis quebram o build; CodeQL; Dependabot (ADR-43) |

## Front-end (Angular 22)

| Recurso | Onde está | O que observar |
| --- | --- | --- |
| Standalone e zoneless | [`app.config.ts`](../src/Web/store-web/src/app/app.config.ts) | Sem NgModule e sem zone.js |
| Signals e Facade | [`products.facade.ts`](../src/Web/store-web/src/app/features/catalog/data/products.facade.ts) | Estado da tela em `signal`/`computed`; o componente não conhece HTTP |
| Smart × dumb components | [`product-list.ts`](../src/Web/store-web/src/app/features/catalog/pages/product-list/product-list.ts) × [`product-table.ts`](../src/Web/store-web/src/app/features/catalog/components/product-table/product-table.ts) | A página coordena; a tabela só recebe `input()` e emite `output()` |
| RxJS onde faz sentido | [`product-list.ts`](../src/Web/store-web/src/app/features/catalog/pages/product-list/product-list.ts) | `debounceTime` na busca |
| Formulário reativo tipado | [`product-form.ts`](../src/Web/store-web/src/app/features/catalog/pages/product-form/product-form.ts) | `NonNullableFormBuilder` e validação no TypeScript |
| Interceptors e guard funcionais | [`auth.interceptor.ts`](../src/Web/store-web/src/app/core/auth/auth.interceptor.ts), [`error.interceptor.ts`](../src/Web/store-web/src/app/core/errors/error.interceptor.ts), [`auth.guard.ts`](../src/Web/store-web/src/app/core/auth/auth.guard.ts) | Token anexado, 401 volta ao login, ProblemDetails vira mensagem |
| Consulta GraphQL | [`catalog-api.ts`](../src/Web/store-web/src/app/features/catalog/data/catalog-api.ts) | Pede só os campos da tabela; confere o array `errors` |
| Organização por funcionalidade | [`src/app`](../src/Web/store-web/src/app/) | `core`, `shared` e `features`, como no guia oficial |

## Testes

| Recurso | Onde está | O que observar |
| --- | --- | --- |
| Testes de domínio | [`OrderTests.cs`](../tests/Ordering.UnitTests/Domain/OrderTests.cs), [`MoneyTests.cs`](../tests/Catalog.UnitTests/Domain/MoneyTests.cs) | Regras testadas sem mock nem infraestrutura |
| Casos de uso com mocks | [`CreateOrderCommandHandlerTests.cs`](../tests/Ordering.UnitTests/Application/CreateOrderCommandHandlerTests.cs) | O Catálogo substituído pela porta mockada |
| Regra de acesso | [`OrderAccessTests.cs`](../tests/Ordering.UnitTests/Application/OrderAccessTests.cs) | O cliente A não lê nem cancela o pedido do cliente B |
| Teste de fumaça de segurança | [`security-smoke.ps1`](../tests/smoke/security-smoke.ps1) | Headers, BOLA/IDOR e rate limiting contra o sistema rodando |

## Entrega e infraestrutura

| Recurso | Onde está | O que observar |
| --- | --- | --- |
| Docker multi-stage e compose | [`Dockerfile`](../src/Services/Ordering/Ordering.Api/Dockerfile), [`docker-compose.yml`](../docker-compose.yml) | Sobe bancos, RabbitMQ, serviços e Gateway com um comando |
| Central Package Management | [`Directory.Packages.props`](../Directory.Packages.props) | Todas as versões de pacotes num arquivo só (ADR-34) |
| CI | [`ci.yml`](../.github/workflows/ci.yml) | Build, testes, Angular e Terraform a cada push e PR |
| Git Flow e versões | [fluxo-git.md](fluxo-git.md), [CHANGELOG](../CHANGELOG.md) | Branches por funcionalidade, releases com tag e versionamento semântico (ADR-33) |
| Infraestrutura como código (AWS) | [infra/terraform](../infra/terraform/README.md) | Rede com subnets isoladas, ECR, RDS, Secrets Manager e deploy por OIDC (ADR-26 a ADR-40) |
