# CLAUDE.md — store-microservices

Projeto de estudo do Renato para o teste prático da LUZA Group (Fullstack C#/.NET + Angular).
Objetivo duplo: **aprender e conseguir justificar** cada escolha de arquitetura e design pattern.
As decisões estão em `docs/decisoes-arquitetura.md` (ADR-01 a ADR-33). Leia esse arquivo antes de qualquer tarefa.

## Regra nº 1: modo estudo
Decisão de 31/10/2026, por causa do prazo do teste: o Renato estuda o projeto **analisando e debugando** o código.
- O Claude escreve o código de cada fase em branches `feature/*` (ver `docs/fluxo-git.md`), com commits pequenos e mensagens convencionais.
- Cada entrega vem com o objetivo, os padrões usados (ADR) e onde colocar breakpoints para estudar (`docs/guia-estudo-*.md`).
- Dúvidas do Renato: responder ligando o trecho de código ao conceito e à pergunta de entrevista correspondente.

## Modo professor
Ao terminar CADA tarefa, encerre com uma seção curta:

```
### Padrões usados aqui
- <Nome do padrão/arquitetura> (ADR-xx): <onde aparece no código> — <porquê em 1 linha>
### Pergunta de fixação
<uma pergunta de entrevista sobre o que foi feito; não responda — espere o Renato>
```

- A cada 3 tarefas, faça também uma **revisão relâmpago**: peça ao Renato para explicar com as palavras dele 2 padrões já usados e corrija o que faltar.
- Se uma decisão nova surgir, proponha um novo ADR em `docs/decisoes-arquitetura.md` antes de implementar.
- Explique em português; código em inglês.

## Fluxo de trabalho (SDD)
1. Ler a fase atual em `README.md` (especificação).
2. Propor o plano da tarefa (arquivos e passos) e esperar o "ok".
3. Implementar em passos pequenos; compilar e rodar os testes a cada passo.
4. Commit com mensagem convencional (`feat:`, `fix:`, `test:`, `docs:`, `refactor:`, `chore:`) em branch por funcionalidade; merge na `develop`.
5. Modo professor (acima).

## Stack
- .NET 10 (LTS), C# moderno, ASP.NET Core com **Controllers**.
- EF Core + PostgreSQL (Npgsql); migrations por serviço.
- YARP (gateway), MassTransit 8 + RabbitMQ, `Microsoft.Extensions.Http.Resilience` (Polly), FluentValidation, Scrutor, JWT Bearer.
- Testes: xUnit, NSubstitute, Shouldly; Testcontainers (opcional).
- **Não usar:** MediatR, AutoMapper, FluentAssertions 8+, Moq (motivos no ADR-12 e ADR-22).
- Angular 22 (versão mais recente; exige Node ^22.22.3 ou ^24.15.0 e TypeScript 6.0) standalone, signals, novo control flow, `inject()`, SCSS.

## Regras de arquitetura (não violar)
- Dependências: `Api → Application + Infrastructure`; `Infrastructure → Application → Domain`; `Domain` não referencia nada.
- `Domain` sem EF Core, ASP.NET ou atributos de persistência (mapeamento via `IEntityTypeConfiguration<T>`).
- Nenhum serviço acessa o banco de outro. Integração só por HTTP (via Adapter) ou evento.
- Entidades sem setters públicos; comportamento em métodos do domínio.
- Nunca expor entidade na API: DTOs (records) de entrada e saída.
- Erro de negócio → `Result<T>`; inesperado → exceção tratada pelo `IExceptionHandler` global → ProblemDetails.
- Controllers finos: recebem, chamam o handler, traduzem o Result em HTTP.
- `async` até o fim, `CancellationToken` em todas as actions e handlers; nunca `.Result`/`.Wait()`.
- Repositório por agregado (sem `IRepository<T>` genérico).
- Contratos de eventos de integração ficam em `BuildingBlocks.Contracts` (só records, sem lógica).
- Consumidores de eventos devem ser idempotentes.

## Clean Code
- Nomes que revelam intenção; métodos curtos; guard clauses; sem números/strings mágicas; sem código comentado; sem `Console.WriteLine`.
- Nullable habilitado; warnings tratados como erro nos projetos de domínio.

## Comandos úteis
- `docker compose up -d` — sobe tudo
- `dotnet build` / `dotnet test` na raiz
- `dotnet ef migrations add <Nome> -p src/Services/Catalog/Catalog.Infrastructure -s src/Services/Catalog/Catalog.Api`
- Front: `cd src/Web/store-web && npm start`
