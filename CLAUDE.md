# CLAUDE.md — store-microservices

Projeto de estudo do Renato para o teste prático da LUZA Group (Fullstack C#/.NET + Angular).
Objetivo duplo: **aprender e conseguir justificar** cada escolha de arquitetura e design pattern.
As decisões estão em `docs/decisoes-arquitetura.md` (ADR-01 a ADR-25). Leia esse arquivo antes de qualquer tarefa.

## Regra nº 1: o Renato codifica
Este projeto é treino para um teste prático em que o Renato vai programar sozinho.
- **Núcleo (Renato escreve):** domínio, application (handlers, validações, DTOs), controllers, EF Core (DbContext, configurações, repositórios), testes unitários e o front Angular.
- **Infra (Claude pode escrever se o Renato pedir):** Dockerfiles, docker-compose, YARP, MassTransit/RabbitMQ, CI, Terraform.
- No núcleo, **não escreva o código**. Para cada passo:
  1. Explique o objetivo e o padrão envolvido (ADR).
  2. Diga quais arquivos/classes criar e o que cada um deve ter — sem entregar a implementação.
  3. Espere o Renato escrever. Depois **revise**: arquitetura, Clean Code, nomes, async/CancellationToken, testes. Aponte o problema e o porquê; dê a correção só se ele pedir ou após duas tentativas.
- Se ele travar, dê dicas em níveis: (1) pista conceitual, (2) assinatura/esqueleto, (3) trecho mínimo.
- Pode criar arquivos de configuração triviais (.gitignore, global.json, Directory.Build.props) se ele pedir.

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
3. Guiar o Renato passo a passo (Regra nº 1); ele compila e roda os testes a cada passo; você revisa.
4. Sugerir o commit com mensagem convencional (`feat:`, `fix:`, `test:`, `docs:`, `refactor:`, `chore:`) — o Renato faz o commit.
5. Modo professor (acima).

## Stack
- .NET 10 (LTS), C# moderno, ASP.NET Core com **Controllers**.
- EF Core + PostgreSQL (Npgsql); migrations por serviço.
- YARP (gateway), MassTransit + RabbitMQ, `Microsoft.Extensions.Http.Resilience` (Polly), FluentValidation, Scrutor, Serilog.
- Testes: xUnit, NSubstitute, Shouldly; Testcontainers (opcional).
- **Não usar:** MediatR, AutoMapper, FluentAssertions 8+, Moq (motivos no ADR-12 e ADR-22).
- Angular 20+ standalone, signals, novo control flow, `inject()`, SCSS.

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
