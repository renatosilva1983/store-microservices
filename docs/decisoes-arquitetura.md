# Decisões de Arquitetura e Design — store-microservices

> Formato ADR (Architecture Decision Record): cada decisão tem **contexto, escolha, porquê e alternativa descartada**.
> Registrar decisões em ADR também é uma prática que vale citar na entrevista.
> No fim de cada item, a **frase para a entrevista**.

---

## Visão geral em 30 segundos

> "É uma loja dividida em microsserviços por contexto de negócio (DDD): Catálogo, Pedidos e Identidade. Cada serviço segue Clean Architecture e tem seu próprio banco. O front em Angular fala só com um API Gateway (YARP). Entre os serviços, uso HTTP com resiliência (Polly) quando preciso de resposta imediata e eventos no RabbitMQ (MassTransit) quando posso trabalhar com consistência eventual. Tudo sobe com docker-compose."

```
Angular 20 ──► Gateway (YARP) ──┬──► Catalog.Api ──► PostgreSQL (catalog)
                                ├──► Ordering.Api ──► PostgreSQL (ordering)
                                └──► Identity.Api (JWT)
Ordering.Api ──HTTP + Polly──► Catalog.Api      (consulta preço/produto)
Ordering.Api ──evento PedidoCriado──► RabbitMQ ──► Catalog.Api (baixa estoque)
```

---

## PARTE 1 — Arquitetura (nível sistema)

### ADR-01 · Microsserviços divididos por Bounded Context (DDD estratégico)
- **Escolha:** 3 serviços — Catalog (produtos/categorias/estoque), Ordering (pedidos), Identity (login/JWT).
- **Porquê:** a fronteira de um microsserviço deve seguir a fronteira do negócio, não a técnica. "Produto" no Catálogo (descrição, estoque) é diferente de "item de pedido" no Ordering (preço congelado no momento da compra). Isso é **bounded context** e **linguagem ubíqua**.
- **Descartado:** dividir por camada técnica (um "serviço de banco", um "serviço de regras") — gera acoplamento e chamadas em cadeia.
- **Frase:** "Dividi os serviços por bounded context, como o DDD propõe: cada um tem seu modelo e sua linguagem, então podem evoluir e ser publicados de forma independente."

### ADR-02 · Clean Architecture dentro de cada serviço
- **Escolha:** cada serviço com 4 projetos: `Domain`, `Application`, `Infrastructure`, `Api`. Dependências apontam para dentro.
- **Porquê:** regra de negócio isolada de banco e framework; testável sem infraestrutura; projetos separados fazem o **compilador** impedir referência errada.
- **Descartado:** Vertical Slice pura — também é válida, mas a vaga pede Clean Architecture explicitamente. Dentro de `Application`, as pastas são **por funcionalidade** (feature folders), unindo o melhor dos dois.
- **Frase:** "Cada microsserviço segue Clean Architecture; dentro da Application organizo por funcionalidade. O domínio não conhece EF Core nem ASP.NET."

### ADR-03 · Database per Service
- **Escolha:** cada serviço tem seu próprio banco PostgreSQL (containers separados no compose). Nenhum serviço lê o banco do outro.
- **Porquê:** é o que garante independência real. Banco compartilhado = acoplamento escondido (mudar uma tabela quebra outro serviço).
- **Consequência assumida:** sem JOIN entre serviços → dados de outro serviço chegam por API ou por evento (ver ADR-05/06).
- **Por que PostgreSQL:** leve no Docker, gratuito, mesmo banco do FleetExecutive. O acesso fica atrás de Repository, então trocar por SQL Server muda só a Infrastructure.
- **Frase:** "Cada serviço é dono dos seus dados. Se o Pedidos precisa de algo do Catálogo, pergunta via API ou recebe por evento — nunca acessa o banco dele."

### ADR-04 · API Gateway com YARP
- **Escolha:** `Store.Gateway` com YARP (proxy reverso da Microsoft) como único ponto de entrada do front.
- **Porquê:** o Angular conhece uma URL só; o gateway centraliza roteamento, CORS e (opcional) rate limiting. Os serviços internos ficam escondidos.
- **Descartado:** Ocelot — funciona, mas YARP é mantido pela Microsoft e mais performático. Front chamando cada serviço direto — espalha URLs e CORS.
- **Padrão:** **API Gateway** (variação **BFF – Backend for Frontend** quando é dedicado a um front).
- **Frase:** "Usei o padrão API Gateway com YARP: o front fala com um único endereço e os serviços internos podem mudar sem afetar o cliente."

### ADR-05 · Comunicação síncrona: HTTP + resiliência (Polly)
- **Escolha:** Ordering consulta o Catálogo por HTTP (preço e existência do produto) usando **Typed HttpClient** (`IHttpClientFactory`) + `Microsoft.Extensions.Http.Resilience` (Polly): **Retry** com backoff, **Circuit Breaker** e **Timeout**.
- **Porquê:** criar pedido precisa da resposta *agora* (preço válido). Rede falha: sem resiliência, uma instabilidade no Catálogo derruba o Pedidos.
- **Padrões:** Retry, Circuit Breaker, Timeout (padrões de resiliência).
- **Cuidado citável:** nunca `new HttpClient()` por requisição (esgotamento de sockets) → `IHttpClientFactory`.
- **Frase:** "Para chamadas síncronas uso HttpClient tipado com retry e circuit breaker: se o Catálogo cair, o circuito abre e o Pedidos falha rápido, em vez de travar tudo."

### ADR-06 · Comunicação assíncrona: eventos com RabbitMQ + MassTransit
- **Escolha:** ao criar um pedido, Ordering publica `PedidoCriadoIntegrationEvent`; Catalog consome e baixa o estoque.
- **Porquê:** baixar estoque não precisa travar o cliente; desacopla os serviços (o Ordering nem sabe quem consome). Aceito **consistência eventual**.
- **Por que MassTransit:** abstrai o RabbitMQ (filas, exchanges, retry, dead-letter) e permite trocar o broker. Usei RabbitMQ na Kakau.
- **Padrões:** **Publish/Subscribe**, **Event-Driven Architecture**, **consumidor idempotente** (processar o mesmo evento duas vezes não pode baixar estoque duas vezes).
- **Extra (se der tempo): Outbox Pattern** com o outbox do MassTransit + EF Core — grava o evento na mesma transação do pedido, evitando "salvou o pedido mas o evento se perdeu".
- **Frase:** "Quando não preciso de resposta imediata, publico um evento. O estoque é atualizado com consistência eventual, e o consumidor é idempotente porque mensageria garante entrega pelo menos uma vez."

### ADR-07 · Autenticação JWT
- **Escolha:** `Identity.Api` emite JWT; cada serviço valida o token (`AddJwtBearer`) e usa `[Authorize]` / políticas.
- **Porquê:** token stateless — cada serviço valida sozinho, sem consultar o Identity a cada chamada.
- **Simplificação consciente:** usuários fixos/em memória no Identity; em produção seria IdentityServer/Keycloak/Entra ID.
- **Frase:** "Autenticação com JWT emitido por um serviço de identidade; cada API valida o token de forma independente."

### ADR-08 · Observabilidade básica
- **Escolha:** Health Checks (`/health`) por serviço, logs estruturados com Serilog, Correlation Id propagado entre serviços.
- **Porquê:** em microsserviços uma requisição passa por vários processos; sem correlation id não dá para rastrear um erro.
- **Frase:** "Cada serviço expõe health check e loga com um correlation id, para eu seguir uma requisição de ponta a ponta."

### ADR-09 · Docker multi-stage + docker-compose
- **Escolha:** Dockerfile multi-stage por serviço (SDK para build, runtime enxuto para rodar); `docker-compose.yml` sobe tudo.
- **Porquê:** imagem final menor e reprodutível; qualquer avaliador roda o projeto com um comando.
- **Frase:** "Com `docker compose up` sobe o sistema inteiro: serviços, gateway, RabbitMQ e bancos."

### ADR-10 · Versões
- **.NET 10 (LTS)** — versão de suporte longo atual; o .NET 8 sai de suporte em nov/2026. C# moderno (records, `init`, pattern matching, primary constructors) cobre e supera o C# 9 da vaga.
- **Angular 20+ (estável mais recente)** — a vaga exige 20+.
- **Frase:** "Escolhi a versão LTS do .NET por estabilidade e suporte."

---

## PARTE 2 — Design Patterns e práticas (nível código)

### ADR-11 · DDD tático no domínio
- **Entidades ricas** (comportamento junto dos dados): `Order.AddItem()`, `Order.Cancel()`, `Product.DebitStock()` — nada de setter público alterando status.
- **Value Objects** (records, igualdade por valor): `Money`, `Sku`, `Address`.
- **Aggregate** `Order` (raiz) com `OrderItem`: só se altera item pela raiz, que garante as invariantes (ex.: pedido sem itens não pode ser confirmado).
- **Domain Events** dentro do serviço (ex.: `OrderCreatedDomainEvent`) → convertidos em evento de integração para o RabbitMQ.
- **Frase:** "O domínio é rico: as regras ficam nas entidades e no agregado, não espalhadas em services."

### ADR-12 · CQRS sem MediatR
- **Escolha:** separar **Commands** (escrita) e **Queries** (leitura) com interfaces próprias `ICommandHandler<TCommand, TResult>` e `IQueryHandler<TQuery, TResult>`, registradas via DI.
- **Porquê:** escrita usa o domínio + EF Core; leitura pode ir direto para DTO (projeção com `Select`, ou Dapper), mais simples e rápida.
- **Por que sem MediatR:** desde 2025 o MediatR tem licença comercial para empresas; e implementar a abstração mostra que entendo o padrão em vez de só usar a biblioteca.
- **Padrões:** CQRS; a ideia do **Mediator** (desacoplar quem pede de quem executa).
- **Frase:** "Separei comandos e consultas. Não usei MediatR por causa da mudança de licença, e a abstração própria tem poucas linhas."

### ADR-13 · Decorator para validação e log
- **Escolha:** decorators em volta dos handlers (`ValidationDecorator`, `LoggingDecorator`) registrados com **Scrutor** (`services.Decorate(...)`).
- **Porquê:** validação e log são preocupações transversais; o handler fica só com a regra (Open/Closed + Single Responsibility).
- **Frase:** "Validação e log entram como decorators: adiciono comportamento sem mexer no handler."

### ADR-14 · Repository + Unit of Work
- **Escolha:** repositório **por agregado** (`IOrderRepository`, `IProductRepository`), interface na camada interna, implementação EF Core na Infrastructure. `IUnitOfWork.SaveChangesAsync()` encapsula o `DbContext`.
- **Porquê:** o domínio não conhece EF; teste com mock. O `DbContext` já é um Unit of Work — a interface só o expõe sem vazar o EF.
- **Descartado:** repositório genérico `IRepository<T>` para tudo — vira um "DbSet disfarçado" e não expressa o negócio.
- **Frase:** "Repositório por agregado, não genérico; e o DbContext já é um Unit of Work, eu só o abstraio."

### ADR-15 · Strategy + Factory (cálculo de frete)
- **Escolha:** `IShippingStrategy` com `StandardShipping`, `ExpressShipping`, `PickupInStore`; escolha por **Keyed Services** do .NET (`[FromKeyedServices("express")]`) ou uma `ShippingStrategyFactory`.
- **Porquê:** elimina `switch` crescente; novo tipo de frete = nova classe, sem alterar as existentes (**Open/Closed**).
- **Frase:** "O frete usa Strategy: cada regra numa classe, escolhida por chave. Novo tipo de frete não mexe no código existente."

### ADR-16 · Adapter / Anti-Corruption Layer (Ordering → Catalog)
- **Escolha:** no Ordering, a interface de domínio `IProductCatalog` (o que o Pedidos precisa saber) é implementada por `CatalogHttpAdapter` na Infrastructure, que chama a API do Catálogo e traduz o DTO externo.
- **Porquê:** se o contrato do Catálogo mudar, só o adapter muda. Protege o modelo do Ordering do modelo alheio (**camada anticorrupção** do DDD).
- **Frase:** "A integração com outro serviço passa por um Adapter: o domínio do Pedidos fala a própria língua e o adapter traduz. Fiz o mesmo com integrações SOAP em projetos reais."

### ADR-17 · Result Pattern + ProblemDetails + tratamento global de erro
- **Escolha:** erro de negócio esperado retorna `Result<T>` (sucesso/falha com código); exceção só para o inesperado. `IExceptionHandler` global converte tudo em **ProblemDetails** (RFC 7807).
- **Porquê:** exceção para fluxo de negócio é cara e esconde o caminho; ProblemDetails dá erro padronizado ao front, sem vazar stack trace.
- **Frase:** "Erro de negócio é resultado, não exceção; o inesperado cai num handler global que devolve ProblemDetails."

### ADR-18 · Validação com FluentValidation
- **Porquê:** regras de entrada fora dos DTOs, testáveis e reutilizáveis. Validação de **formato** na Application; **invariante de negócio** no domínio.

### ADR-19 · Options Pattern
- **Escolha:** `IOptions<JwtSettings>`, `IOptions<RabbitMqSettings>` com validação na inicialização (`ValidateOnStart`).
- **Porquê:** configuração tipada, sem string mágica espalhada.

### ADR-20 · Injeção de Dependência organizada por camada
- **Escolha:** extensões `AddApplication()`, `AddInfrastructure(config)` em cada camada; `Program.cs` enxuto.
- **Lifetimes:** `DbContext`/repositórios/handlers = **Scoped**; strategies sem estado = Singleton ou Transient; consumidores do MassTransit criam escopo próprio (evita **captive dependency**).
- **Frase:** "Cada camada registra suas dependências; Scoped para o que depende da requisição e cuidado para não injetar Scoped em Singleton."

### ADR-21 · Controllers (MVC) em vez de Minimal APIs
- **Porquê:** a vaga pede MVC; controllers deixam explícitos model binding, filtros e convenções. Minimal API seria igualmente válida para serviços pequenos — saber explicar a troca.
- **Frase:** "Usei controllers porque o time trabalha com MVC; Minimal APIs seriam uma alternativa válida em serviços menores."

### ADR-22 · Testes
- **Escolha:** xUnit + NSubstitute + Shouldly. Unitários no domínio e nos handlers (AAA). Extra: integração com `WebApplicationFactory` + **Testcontainers** (PostgreSQL real).
- **Porquê das bibliotecas:** FluentAssertions 8 passou a ter licença comercial (2025) e o Moq teve a polêmica do SponsorLink (2023) — NSubstitute e Shouldly são livres e simples.
- **Frase:** "Testo regra de negócio no domínio e nos handlers; integração com banco real via Testcontainers."

### ADR-23 · CI com GitHub Actions
- Build + testes a cada push/PR. Mesmo conhecimento usado na Montanari e no FleetExecutive.

---

## PARTE 3 — Front-end (Angular 20+)

### ADR-24 · Angular moderno
- **Standalone components** (sem NgModule), **Signals** (`signal`, `computed`, `input()`, `output()`), **novo control flow** (`@if`, `@for ... track`), `inject()`, lazy loading com `loadComponent`, **zoneless** se estável.
- **Porquê:** é o Angular atual e o que a vaga (20+) espera ver.

### ADR-25 · Organização e padrões no front
- **Feature folders** (`features/catalog`, `features/orders`) + `core` (interceptors, guards, auth) + `shared` (componentes reutilizáveis).
- **Smart/Dumb components** (container/presentational): a página busca dados; o componente de lista só recebe `input()` e emite `output()`.
- **Facade service** por feature: a página fala com um service que esconde HTTP + estado (signals).
- **Interceptors funcionais** (JWT e tratamento de erro); **guard funcional** de rota.
- **Formulários reativos tipados** (`nonNullable.group`).
- **Frase:** "No front, organizo por feature, separo componentes de página e de apresentação, e uso um service como facade com signals para o estado."

### ADR-32 · GraphQL para consultas do front (Hot Chocolate) — Fase 3.5
- **Divisão:**
  - **REST:** todos os comandos (criar, editar, excluir — no Catalog e no Ordering), a comunicação entre serviços (Ordering → Catalog via Adapter) e o Identity.
  - **GraphQL:** consultas do Catálogo feitas pelo front (produtos com categoria, filtro, ordenação, paginação). Sem mutations — escrita continua em REST.
  - Os GETs REST do Catalog continuam existindo para integração entre serviços e Swagger.
- **Biblioteca:** Hot Chocolate (MIT), com paginação por cursor, filtros e projeções.
- **Porquê:** o front pede só os campos que usa (evita **over-fetching**) e traz produto + categoria numa chamada (evita **under-fetching** / várias idas ao servidor). REST segue melhor para comandos e integração: simples, padronizado, cacheável por HTTP.
- **Arquitetura:** os resolvers GraphQL chamam os **mesmos query handlers** da Application. GraphQL e REST são só bordas diferentes (Api) sobre o mesmo núcleo — prova prática da Clean Architecture.
- **Gateway:** YARP roteia `/graphql` para o Catalog.
- **Cuidados citáveis:** problema **N+1** (resolvido com **DataLoader**, que agrupa as buscas) e limite de profundidade/complexidade da query (evita consultas abusivas).
- **Frase:** "Usei REST para comandos e integração entre serviços, e GraphQL para as consultas do front, sobre os mesmos casos de uso. Isso só foi simples porque a lógica está na Application, não nos controllers. Para evitar N+1 no GraphQL, usei DataLoader."

---

## PARTE 4 — Deploy na AWS (Fase 4, opcional)

### ADR-26 · ECS Fargate para os microsserviços
- **Escolha:** cada serviço (Gateway, Catalog, Ordering, Identity) é um **ECS Service** com tasks **Fargate**; imagens no **ECR** (um repositório por serviço).
- **Porquê:** containers gerenciados sem administrar servidor; cada serviço escala e é publicado sozinho — o mesmo princípio da arquitetura, agora na infraestrutura. Reaproveita o que já fiz no FleetExecutive.
- **Descartado:** EKS (Kubernetes) — mais poderoso, mas caro e complexo para o tamanho do projeto; EC2 com compose — funciona, mas não demonstra deploy independente por serviço.
- **Frase:** "Na AWS cada microsserviço é um serviço ECS em Fargate, com imagem própria no ECR: deploy e escala independentes, sem gerenciar servidor."

### ADR-27 · Rede e comunicação interna
- **ALB** só na frente do **Gateway** (único ponto público da API). Serviços internos sem acesso público, liberados só por **Security Group**.
- **ECS Service Connect** para descoberta de serviços: o Gateway e o Ordering chamam `http://catalog-api` pelo nome, sem IP fixo (padrão **Service Discovery**).
- Tasks em **subnets públicas com security groups restritivos**, para evitar o custo do **NAT Gateway** (em produção: subnets privadas + NAT ou VPC Endpoints).
- **Frase:** "Só o gateway é exposto pelo load balancer; os serviços se encontram por service discovery e só aceitam tráfego interno."

### ADR-28 · Dados e mensageria com economia de crédito
- **RDS PostgreSQL** (instância pequena) com **um banco por serviço** e usuários separados: o isolamento lógico do Database per Service se mantém; em produção seriam instâncias separadas.
- **RabbitMQ como task Fargate** em vez de Amazon MQ (custo bem menor). Sem persistência durável — aceitável para demonstração; em produção: Amazon MQ ou Amazon SQS/SNS.
- **Frase:** "Mantive banco por serviço como isolamento lógico numa instância RDS e o RabbitMQ em container, por custo; em produção usaria instâncias separadas e Amazon MQ ou SQS."

### ADR-29 · Front estático: S3 + CloudFront
- **Escolha:** build do Angular publicado num bucket **S3** privado servido pelo **CloudFront** (HTTPS, cache, CDN).
- **Porquê:** front Angular é estático — não precisa de container; fica barato e rápido.
- **Frase:** "O Angular é estático, então vai para S3 com CloudFront; container só para o que executa código no servidor."

### ADR-30 · Configuração, segredos e logs
- Segredos (connection strings, chave JWT) no **Secrets Manager / SSM Parameter Store**, injetados na task — nada de segredo no repositório ou na imagem.
- Logs dos containers no **CloudWatch Logs** (Serilog em JSON). Health check do ALB apontando para `/health`.
- **Frase:** "Configuração vem do ambiente e segredos do Secrets Manager — a mesma imagem roda local e na nuvem (princípio do Twelve-Factor App)."

### ADR-31 · Infraestrutura como código + CI/CD
- **Terraform** descrevendo VPC, ECR, ECS, ALB, RDS, S3/CloudFront. **Porquê:** recriar e destruir tudo com um comando (economiza crédito) e é a ferramenta de IaC mais pedida no mercado. Alternativa válida: AWS CDK em C#.
- **GitHub Actions com OIDC** (sem chave de acesso guardada): build → testes → push no ECR → atualização do serviço ECS só do serviço alterado.
- **Frase:** "A infraestrutura é código: com Terraform subo e destruo o ambiente inteiro. O pipeline usa OIDC, sem credencial fixa, e publica só o serviço que mudou."

### Controle de custo (operacional)
- Alerta no **AWS Budgets** antes do primeiro deploy; conferir validade e cobertura dos créditos.
- Após gravar a demo: `desired_count = 0` nas tasks ou `terraform destroy`.

---

## Simplificações conscientes (diga antes que perguntem)
- Identity com usuários fixos (em produção: Keycloak/Entra ID/IdentityServer).
- Sem Kubernetes, service discovery ou tracing distribuído (OpenTelemetry seria o próximo passo).
- Saga/orquestração de pedido não implementada; o fluxo é coreografado por eventos.
- **Frase:** "Mantive o escopo de um projeto de estudo; os próximos passos seriam OpenTelemetry, Outbox completo e orquestração com Kubernetes."
