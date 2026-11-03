# Changelog

Formato baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/) e versionamento [SemVer](https://semver.org/lang/pt-BR/).

## [Não publicado]
### Adicionado
- Fase 4 (início) — Terraform: bootstrap do bucket de state (S3 com lock nativo), VPC com subnets públicas e de dados, repositórios ECR por serviço e role de deploy do GitHub Actions via OIDC (`infra/terraform`).
- CI valida o Terraform (`fmt`, `validate`).

## [1.1.0] - 2026-11-02
### Adicionado
- Fase 3.5 — GraphQL (Hot Chocolate) para consultas do Catálogo; listagem do Angular via GraphQL.

## [1.0.0] - 2026-11-02
### Adicionado
- Fase 3 — mensageria com RabbitMQ + MassTransit (Outbox/Inbox): pedido criado baixa o estoque no Catálogo por evento e é confirmado ou cancelado; Identity.Api com JWT e APIs protegidas; login no Angular; health checks, correlation id e logs estruturados; CI no GitHub Actions.
- Migrations do Outbox/Inbox do MassTransit (Catalog e Ordering).

## [0.2.0] - 2026-11-01
### Adicionado
- Fase 2 — Gateway YARP; serviço de Pedidos (agregado Order, frete com Strategy, integração com o Catálogo via HTTP com resiliência, API REST); front Angular 22 com listagem, filtro, paginação e formulário de produtos; docker-compose com banco por serviço.
- Migration inicial do banco de Pedidos.

### Alterado
- Senha do banco fora do repositório: conexão-base na variável `POSTGRES_CONNECTION` (APIs) e `POSTGRES_PASSWORD` via `.env` (docker-compose).
- Solutions separadas no Visual Studio: `store-server.slnx` (back, com perfil de execução multiprojeto) e `src/Web/store-web/store-web.slnx` (Angular via `.esproj`).
- Versões NuGet centralizadas em `Directory.Packages.props`; `ApiController` movido para `BuildingBlocks.Api`.

## [0.1.0] - 2026-11-01
### Adicionado
- Fase 1 — Catálogo: building blocks (Result, CQRS, decorators), domínio (Product, Category, Money, Sku), casos de uso de produtos e categorias, persistência EF Core + PostgreSQL, API REST com ProblemDetails e Scalar, Docker e testes unitários.
- Migration inicial do banco do Catálogo.
- Estrutura inicial da solução em Clean Architecture (Catalog, BuildingBlocks, testes).
- Documentação: especificação por fase (README), decisões de arquitetura (ADRs) e fluxo de Git.
