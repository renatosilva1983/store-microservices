# store-microservices

Loja em **microsserviços .NET 10 + Angular 22**, com Clean Architecture, DDD, CQRS, mensageria e API Gateway.
Decisões e justificativas: [docs/decisoes-arquitetura.md](docs/decisoes-arquitetura.md).

## Arquitetura

```mermaid
flowchart LR
    W[Angular 22] --> G[Gateway YARP]
    G --> C[Catalog.Api]
    G --> O[Ordering.Api]
    G --> I[Identity.Api]
    C --> CDB[(PostgreSQL catalog)]
    O --> ODB[(PostgreSQL ordering)]
    O -- HTTP + Polly --> C
    O -- PedidoCriado --> R{{RabbitMQ}}
    R --> C
```

## Estrutura

```
store-microservices/
├─ src/
│  ├─ Gateway/Store.Gateway
│  ├─ Services/
│  │  ├─ Catalog/   Catalog.Domain | Catalog.Application | Catalog.Infrastructure | Catalog.Api
│  │  ├─ Ordering/  Ordering.Domain | Ordering.Application | Ordering.Infrastructure | Ordering.Api
│  │  └─ Identity/  Identity.Api
│  ├─ BuildingBlocks/
│  │  ├─ BuildingBlocks.Contracts   (eventos de integração)
│  │  └─ BuildingBlocks.Application (Result, ICommandHandler, IQueryHandler, decorators)
│  └─ Web/store-web                 (Angular)
├─ tests/  Catalog.UnitTests | Ordering.UnitTests | (Catalog.IntegrationTests)
├─ docs/   decisoes-arquitetura.md
├─ infra/terraform/   bootstrap (state) | aws (ambiente da Fase 4)
├─ docker-compose.yml
└─ .github/workflows/ci.yml
```

## Especificação por fase

### Fase 1 — Catalog.Api
- Domínio: `Category`, `Product` (Sku e Money como value objects; `DebitStock`, `ChangePrice`).
- Casos de uso: criar/editar/excluir/obter produto; listar com **filtro por nome e categoria + paginação**; CRUD de categoria.
- CQRS com handlers próprios; FluentValidation via decorator; Result + ProblemDetails; Swagger.
- EF Core + PostgreSQL, migrations, seed de categorias.
- Testes unitários do domínio e dos handlers.
- Dockerfile multi-stage; compose com Postgres.
- **Aceite:** `docker compose up` → Swagger do Catalog funcionando com CRUD e paginação; testes verdes.

### Fase 2 — Gateway + Angular + Ordering
- Gateway YARP roteando `/api/catalog/**` e `/api/orders/**`; CORS para o front.
- Angular: listagem de produtos (filtro com `debounceTime`, paginação), formulário reativo de criar/editar, exclusão com confirmação; facade + signals; interceptor de erro.
- Ordering: agregado `Order`/`OrderItem`; criar pedido consultando o Catálogo via `IProductCatalog` → `CatalogHttpAdapter` (Typed HttpClient + resiliência); frete com **Strategy** (keyed services).
- **Aceite:** front lista/edita produtos pelo gateway; pedido criado com preço vindo do Catálogo; derrubar o Catalog mostra o circuit breaker abrindo nos logs.

### Fase 3 — Eventos, JWT e acabamento
- MassTransit + RabbitMQ: `PedidoCriadoIntegrationEvent` → Catalog baixa estoque (consumidor idempotente). Extra: Outbox.
- Identity.Api emitindo JWT; serviços com `[Authorize]`; interceptor de token e guard no Angular; tela de login.
- Health checks, Serilog, Correlation Id.
- CI no GitHub Actions; README final com prints.
- **Aceite:** criar pedido pelo front reduz o estoque no Catálogo; rotas protegidas exigem login.

### Fase 3.5 — GraphQL no Catálogo (opcional) — ADR-32
- Hot Chocolate no Catalog.Api: queries de produtos e categorias com filtro, ordenação, paginação por cursor e projeção; DataLoader para categoria (evitar N+1); limite de complexidade.
- Resolvers chamam os query handlers existentes (nenhuma regra nova na borda). Sem mutations.
- Gateway roteia `/graphql`; Angular passa a usar GraphQL na listagem de produtos (Apollo Angular ou `HttpClient` com POST simples). Criar/editar/excluir continuam em REST.
- **Aceite:** listagem do front via GraphQL trazendo só os campos usados; REST e GraphQL retornam os mesmos dados; testes dos handlers seguem verdes.

### Fase 4 — Deploy na AWS (opcional, depois da Fase 3) — ADR-26 a ADR-31 e ADR-35 a ADR-38
- Como aplicar: [infra/terraform/README.md](infra/terraform/README.md).
- Terraform: VPC (subnets públicas, sem NAT), ECR por serviço, cluster ECS com Service Connect, serviços Fargate (Gateway, Catalog, Ordering, Identity, RabbitMQ), ALB na frente do Gateway, RDS PostgreSQL (um banco por serviço), S3 + CloudFront para o Angular, Secrets Manager, CloudWatch Logs.
- GitHub Actions com OIDC: build, testes, push no ECR e deploy só do serviço alterado; publicação do front no S3 com invalidação do CloudFront.
- Antes de tudo: alerta no AWS Budgets. Depois da demo: tasks a zero ou `terraform destroy`.
- **Aceite:** front acessível pela URL do CloudFront; criar pedido reduz o estoque também na AWS; push na `main` publica automaticamente; ambiente recriável com `terraform apply`.

### Prioridade do escopo (nesta ordem)
1. Fase 3.5 (GraphQL), Outbox e Testcontainers
2. Identity completo → JWT com chave fixa de desenvolvimento
3. RabbitMQ (manter só HTTP)
Nunca cortar: Fase 1 completa e o CRUD no Angular — são o núcleo do projeto.

## Como rodar

### Pré-requisitos
.NET 10 SDK, Node 24 LTS (ou 22.22+), a ferramenta do EF Core (`dotnet tool install -g dotnet-ef`) e um PostgreSQL. Docker Desktop para a opção 1.

### Conexão com o banco (sem senha no repositório)
As APIs leem a variável de ambiente **`POSTGRES_CONNECTION`** com host, porta, usuário e senha, sem o nome do banco:
```powershell
[Environment]::SetEnvironmentVariable('POSTGRES_CONNECTION', 'Host=localhost;Port=5432;Username=postgres;Password=SUA_SENHA', 'User')
```
Cada serviço completa com o próprio banco (`catalog`, `ordering`), definido em `Database:Name` no appsettings. Reabra terminal e Visual Studio depois de criar a variável.

### Outros segredos (Fase 3)
```powershell
[Environment]::SetEnvironmentVariable('Jwt__SigningKey', '<frase com 32+ caracteres>', 'User')
[Environment]::SetEnvironmentVariable('RabbitMq__Password', '<senha do RabbitMQ>', 'User')
```
No Docker, os mesmos valores vão no `.env` (`JWT_SIGNING_KEY`, `RABBITMQ_PASSWORD`). Para rodar pelo Visual Studio, suba só o RabbitMQ: `docker compose up -d rabbitmq`.

Login de demonstração: `admin@store.dev` / `Store@2026`.


### Banco de dados (migrations)
As migrations ficam no repositório e são aplicadas automaticamente quando cada API sobe em Development (criam os bancos `catalog` e `ordering`).
Ao mudar o modelo de um serviço, gere uma nova:
```powershell
dotnet ef migrations add <Nome> -p src/Services/<Servico>/<Servico>.Infrastructure -s src/Services/<Servico>/<Servico>.Api -o Persistence/Migrations
```

### Opção 1 — back-end inteiro no Docker
Copie `.env.example` para `.env` e defina `POSTGRES_PASSWORD` (o `.env` não vai para o Git).
```powershell
docker compose up -d --build
```
| Serviço | Endereço |
|---|---|
| Gateway (entrada do front) | http://localhost:5000 |
| Catalog.Api | http://localhost:5101/scalar |
| Ordering.Api | http://localhost:5102/scalar |
| Identity.Api | http://localhost:5103/scalar |
| RabbitMQ (painel) | http://localhost:15672 |

As migrations e o seed de categorias são aplicados na inicialização.

### Opção 2 — no Visual Studio (para debugar)
Back e front têm **solutions separadas**, cada uma com o próprio botão Iniciar:

| Solution | Conteúdo | Como rodar |
|---|---|---|
| `store-server.slnx` (raiz) | Gateway, serviços, building blocks e testes | Perfil **"Back-end (Gateway + Catalog + Ordering)"** no menu ao lado do botão Iniciar (definido em `store-server.slnLaunch`) |
| `src/Web/store-web/store-web.slnx` | Projeto Angular (`store-web.esproj`) | F5: roda `npm start` e abre o navegador em http://localhost:4200 com depuração |

As APIs usam o PostgreSQL apontado por `POSTGRES_CONNECTION` (um servidor, um banco por serviço, criados pelas migrations).

### Front-end pela linha de comando
```powershell
cd src/Web/store-web
npm install
npm start
```
Abre em http://localhost:4200 e conversa com o Gateway em http://localhost:5000.

### Testes
```powershell
dotnet test
```

### Documentação
