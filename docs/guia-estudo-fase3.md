# Guia de estudo — Fase 3 (eventos, JWT, observabilidade e CI)

## Antes de começar

1. **Variáveis de ambiente** (usuário do Windows; reabra terminal e Visual Studio depois):
   ```powershell
   [Environment]::SetEnvironmentVariable('Jwt__SigningKey', '<uma frase com 32+ caracteres>', 'User')
   [Environment]::SetEnvironmentVariable('RabbitMq__Password', '<senha do RabbitMQ>', 'User')
   ```
   E no `.env` da raiz (copie do `.env.example`): `RABBITMQ_PASSWORD` com a mesma senha e `JWT_SIGNING_KEY` com a mesma chave.
2. **RabbitMQ** (só ele no Docker): `docker compose up -d rabbitmq` → painel em http://localhost:15672 (usuário `store`).
3. **Migrations novas** (tabelas do Outbox/Inbox):
   ```powershell
   dotnet ef migrations add AddMessagingOutbox -p src/Services/Catalog/Catalog.Infrastructure -s src/Services/Catalog/Catalog.Api -o Persistence/Migrations
   dotnet ef migrations add AddMessagingOutbox -p src/Services/Ordering/Ordering.Infrastructure -s src/Services/Ordering/Ordering.Api -o Persistence/Migrations
   ```
4. Visual Studio: perfil **"Back-end (Gateway + Identity + Catalog + Ordering)"**. Front: F5 na `store-web.slnx`.
5. Login de demonstração: `admin@store.dev` / `Store@2026`.

## Fluxo 1: criar pedido → baixa de estoque por evento (coreografia)

```
POST /api/orders (Ordering)
  → CreateOrderCommandHandler
      → Order (Pending) + IIntegrationEventPublisher.Publish(OrderCreated)
      → SaveChanges  ── pedido e mensagem gravados na MESMA transação (tabela OutboxMessage)
  ← 201 (o cliente não espera o estoque)
Entrega do Outbox (serviço em segundo plano) → RabbitMQ → fila catalog-order-created
  → OrderCreatedConsumer (Catalog) → Inbox confere se a mensagem já foi processada
      → ReserveStockForOrderCommandHandler: valida todos os itens → debita → Publish(StockReserved | StockRejected)
RabbitMQ → fila ordering-stock-reserved / ordering-stock-rejected
  → StockReservedConsumer → ConfirmOrderStock → pedido Confirmed
  → StockRejectedConsumer → RejectOrderStock → pedido Cancelled
```

| # | Onde | O que observar |
|---|---|---|
| 1 | `CreateOrderCommandHandler`, linha do `PublishAsync` | Publica **antes** do `SaveChanges`. Olhe a tabela `ordering."OutboxMessage"`: a mensagem aparece junto com o pedido |
| 2 | `OrderCreatedConsumer.Consume` (Catalog) | O breakpoint para em **outro serviço**, sem chamada HTTP: comunicação assíncrona |
| 3 | `ReserveStockForOrderCommandHandler` | Tudo ou nada: valida todos os itens antes de debitar qualquer um |
| 4 | Painel do RabbitMQ → *Queues* | Filas `catalog-order-created`, `ordering-stock-reserved`, `ordering-stock-rejected` e as `_error` |
| 5 | `GET /api/orders/{id}` logo após criar | Primeiro `Pending`, segundos depois `Confirmed`: **consistência eventual** |

### Experimentos
- **Sem estoque:** peça mais unidades do que existem (o Ordering pré-valida; para forçar, crie dois pedidos seguidos que somados passem do estoque). O segundo termina `Cancelled`.
- **Ordering fora do ar depois de criar:** crie o pedido, pare o Ordering. O Catalog debita e publica `StockReserved`, que **fica na fila**. Suba o Ordering: ele consome e confirma. Nenhuma mensagem se perde.
- **Idempotência:** no painel do RabbitMQ, republique manualmente uma mensagem já consumida (mesmo MessageId). O Inbox descarta: o estoque não baixa duas vezes.

## Fluxo 2: login e token

```
Angular Login → POST /api/auth/login (Gateway → Identity.Api) → JWT assinado (HS256)
authInterceptor anexa "Authorization: Bearer <token>" em toda chamada
Catalog/Ordering: AddJwtAuthentication valida assinatura, emissor, audiência e validade — sem chamar o Identity
```

| # | Onde | O que observar |
|---|---|---|
| 1 | `TokenService.Create` | Claims `sub`, `email`, `name`, `role`. Cole o token em jwt.io para ver o conteúdo (o conteúdo é legível; a **assinatura** é que impede alteração) |
| 2 | `AuthenticationRegistration` | Cada serviço valida o token sozinho (stateless) |
| 3 | `ProductsController` | `[Authorize]` na classe e `[AllowAnonymous]` nos GETs: listar é público, alterar exige login |
| 4 | `auth.guard.ts` | Protege a rota no front; a proteção de verdade é o `[Authorize]` no back |
| 5 | `auth.interceptor.ts` | 401 → sai e volta para o login com `returnUrl` |

## Fluxo 3: observabilidade

| Onde | O que observar |
|---|---|
| `GET http://localhost:5086/health` | `Healthy` só se o banco responde (e o barramento do MassTransit está conectado) |
| Header `X-Correlation-Id` na resposta | Envie um pedido com o header `X-Correlation-Id: teste-123`: o mesmo id aparece nos logs do Ordering **e** do Catalog (propagado no HttpClient) |
| `ServiceDefaults.cs` | O que todo serviço tem igual, num lugar só: ProblemDetails, health, correlation, formato de log (texto em dev, JSON em container) |

## Fluxo 4: CI

`.github/workflows/ci.yml`: a cada push/PR em `main` ou `develop`, o GitHub compila e testa o back (`dotnet test`) e faz o build de produção do Angular. Veja na aba *Actions* do repositório depois do push.

## Mapa: arquivo → padrão → pergunta de entrevista

| Arquivo | Padrão (ADR) | Pergunta |
|---|---|---|
| `BuildingBlocks.Contracts/*` | Contratos de integração | "O que dois serviços compartilham? Como versionar um evento?" |
| `MessagingRegistration` | Publish/Subscribe, Outbox, Inbox (06) | "Como garantir que o evento sai se o banco salvou? E se chegar duas vezes?" |
| `ReserveStockForOrderCommandHandler` | Coreografia, consistência eventual | "Saga orquestrada × coreografada?" |
| `ConfirmOrderStockCommandHandler` | Consumidor idempotente | "O que é idempotência e por que mensageria exige?" |
| Fila `_error` + `UseMessageRetry` | Retry + dead-letter | "O que acontece com uma mensagem que sempre falha?" |
| `TokenService`, `AuthenticationRegistration` | JWT stateless (07) | "Por que o serviço não consulta o Identity a cada requisição? Como revogar?" |
| `AuthController.Login` | Segurança básica | "Por que a mesma mensagem para e-mail inexistente e senha errada?" |
| `CorrelationId.cs` | Correlation Id (08) | "Como rastrear uma requisição entre microsserviços?" |
| `ServiceDefaults.cs` | Configuração comum | "Como padronizar vários serviços sem copiar código?" |
| `ci.yml` | CI (23) | "O que roda no seu pipeline?" |

## Decisões que valem ser defendidas
- **MassTransit 8.x**, não 9: a versão 9 passou a ser comercial; a 8 continua open source.
- **Logs sem Serilog**: o logging nativo do .NET já faz escopo e JSON estruturado; menos uma dependência. Serilog entraria para enviar a destinos como Seq ou Elastic.
- **Token no sessionStorage**: simples para o estudo. Em produção, o mais seguro é cookie HttpOnly emitido por um BFF.
- **Usuários de demonstração em configuração**: simplificação; em produção, Keycloak / Entra ID.
- **Coreografia, sem orquestrador**: com dois passos basta; com muitos passos e compensações, uma saga orquestrada (MassTransit State Machine) fica mais clara.
