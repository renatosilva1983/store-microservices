# Changelog

Formato baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/) e versionamento [SemVer](https://semver.org/lang/pt-BR/).

## [Não publicado]
### Adicionado
- Fase 2 — Gateway YARP; serviço de Pedidos (agregado Order, frete com Strategy, integração com o Catálogo via HTTP com resiliência, API REST); front Angular 22 com listagem, filtro, paginação e formulário de produtos; docker-compose com banco por serviço.

### Alterado
- Versões NuGet centralizadas em `Directory.Packages.props`; `ApiController` movido para `BuildingBlocks.Api`.

- Fase 1 — Catálogo: building blocks (Result, CQRS, decorators), domínio (Product, Category, Money, Sku), casos de uso de produtos e categorias, persistência EF Core + PostgreSQL, API REST com ProblemDetails e Scalar, Docker e testes unitários.
- Estrutura inicial da solução em Clean Architecture (Catalog, BuildingBlocks, testes).
- Documentação: especificação por fase (README), decisões de arquitetura (ADRs) e fluxo de Git.
