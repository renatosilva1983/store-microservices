# Decisões de Arquitetura e Design — store-microservices

> Formato ADR (Architecture Decision Record): cada decisão tem **contexto, escolha, porquê e alternativa descartada**.
> Registrar decisões em ADR mantém o porquê de cada escolha junto do código.
> No fim de cada item, a decisão **em resumo**.

---

## Visão geral em 30 segundos

> "É uma loja dividida em microsserviços por contexto de negócio (DDD): Catálogo, Pedidos e Identidade. Cada serviço segue Clean Architecture e tem seu próprio banco. O front em Angular fala só com um API Gateway (YARP). Entre os serviços, uso HTTP com resiliência (Polly) quando preciso de resposta imediata e eventos no RabbitMQ (MassTransit) quando posso trabalhar com consistência eventual. Tudo sobe com docker-compose."

```
Angular 22 ──► Gateway (YARP) ──┬──► Catalog.Api ──► PostgreSQL (catalog)
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
- **Em resumo:** "Dividi os serviços por bounded context, como o DDD propõe: cada um tem seu modelo e sua linguagem, então podem evoluir e ser publicados de forma independente."

### ADR-02 · Clean Architecture dentro de cada serviço
- **Escolha:** cada serviço com 4 projetos: `Domain`, `Application`, `Infrastructure`, `Api`. Dependências apontam para dentro.
- **Porquê:** regra de negócio isolada de banco e framework; testável sem infraestrutura; projetos separados fazem o **compilador** impedir referência errada.
- **Descartado:** Vertical Slice pura — também é válida, mas a Clean Architecture deixa as fronteiras entre as camadas explícitas e verificáveis. Dentro de `Application`, as pastas são **por funcionalidade** (feature folders), unindo o melhor dos dois.
- **Em resumo:** "Cada microsserviço segue Clean Architecture; dentro da Application organizo por funcionalidade. O domínio não conhece EF Core nem ASP.NET."

### ADR-03 · Database per Service
- **Escolha:** cada serviço tem seu próprio banco PostgreSQL (containers separados no compose). Nenhum serviço lê o banco do outro.
- **Porquê:** é o que garante independência real. Banco compartilhado = acoplamento escondido (mudar uma tabela quebra outro serviço).
- **Consequência assumida:** sem JOIN entre serviços → dados de outro serviço chegam por API ou por evento (ver ADR-05/06).
- **Por que PostgreSQL:** leve no Docker, gratuito, mesmo banco do FleetExecutive. O acesso fica atrás de Repository, então trocar por SQL Server muda só a Infrastructure.
- **Em resumo:** "Cada serviço é dono dos seus dados. Se o Pedidos precisa de algo do Catálogo, pergunta via API ou recebe por evento — nunca acessa o banco dele."

### ADR-04 · API Gateway com YARP
- **Escolha:** `Store.Gateway` com YARP (proxy reverso da Microsoft) como único ponto de entrada do front.
- **Porquê:** o Angular conhece uma URL só; o gateway centraliza roteamento, CORS e (opcional) rate limiting. Os serviços internos ficam escondidos.
- **Descartado:** Ocelot — funciona, mas YARP é mantido pela Microsoft e mais performático. Front chamando cada serviço direto — espalha URLs e CORS.
- **Padrão:** **API Gateway** (variação **BFF – Backend for Frontend** quando é dedicado a um front).
- **Em resumo:** "Usei o padrão API Gateway com YARP: o front fala com um único endereço e os serviços internos podem mudar sem afetar o cliente."

### ADR-05 · Comunicação síncrona: HTTP + resiliência (Polly)
- **Escolha:** Ordering consulta o Catálogo por HTTP (preço e existência do produto) usando **Typed HttpClient** (`IHttpClientFactory`) + `Microsoft.Extensions.Http.Resilience` (Polly): **Retry** com backoff, **Circuit Breaker** e **Timeout**.
- **Porquê:** criar pedido precisa da resposta *agora* (preço válido). Rede falha: sem resiliência, uma instabilidade no Catálogo derruba o Pedidos.
- **Padrões:** Retry, Circuit Breaker, Timeout (padrões de resiliência).
- **Cuidado citável:** nunca `new HttpClient()` por requisição (esgotamento de sockets) → `IHttpClientFactory`.
- **Em resumo:** "Para chamadas síncronas uso HttpClient tipado com retry e circuit breaker: se o Catálogo cair, o circuito abre e o Pedidos falha rápido, em vez de travar tudo."

### ADR-06 · Comunicação assíncrona: eventos com RabbitMQ + MassTransit
- **Escolha:** ao criar um pedido, Ordering publica `PedidoCriadoIntegrationEvent`; Catalog consome e baixa o estoque.
- **Porquê:** baixar estoque não precisa travar o cliente; desacopla os serviços (o Ordering nem sabe quem consome). Aceito **consistência eventual**.
- **Por que MassTransit:** abstrai o RabbitMQ (filas, exchanges, retry, dead-letter) e permite trocar o broker. Usei RabbitMQ na Kakau. Versão **8.x** (open source); a 9.x passou a ter licença comercial.
- **Resposta do Catálogo:** `StockReserved` confirma e `StockRejected` cancela o pedido — coreografia, sem orquestrador.
- **Padrões:** **Publish/Subscribe**, **Event-Driven Architecture**, **consumidor idempotente** (processar o mesmo evento duas vezes não pode baixar estoque duas vezes).
- **Extra (se der tempo): Outbox Pattern** com o outbox do MassTransit + EF Core — grava o evento na mesma transação do pedido, evitando "salvou o pedido mas o evento se perdeu".
- **Em resumo:** "Quando não preciso de resposta imediata, publico um evento. O estoque é atualizado com consistência eventual, e o consumidor é idempotente porque mensageria garante entrega pelo menos uma vez."

### ADR-07 · Autenticação JWT
- **Escolha:** `Identity.Api` emite JWT; cada serviço valida o token (`AddJwtBearer`) e usa `[Authorize]` / políticas.
- **Porquê:** token stateless — cada serviço valida sozinho, sem consultar o Identity a cada chamada.
- **Simplificação consciente:** usuários fixos/em memória no Identity; em produção seria IdentityServer/Keycloak/Entra ID.
- **Em resumo:** "Autenticação com JWT emitido por um serviço de identidade; cada API valida o token de forma independente."

### ADR-08 · Observabilidade básica
- **Escolha:** Health Checks (`/health`) por serviço, logs estruturados com o logging nativo (texto no desenvolvimento, JSON em container), Correlation Id propagado entre serviços — tudo em `ServiceDefaults` (BuildingBlocks.Api).
- **Por que sem Serilog:** o logging nativo já faz escopos e JSON; Serilog entraria para enviar logs a destinos como Seq ou Elastic.
- **Porquê:** em microsserviços uma requisição passa por vários processos; sem correlation id não dá para rastrear um erro.
- **Em resumo:** "Cada serviço expõe health check e loga com um correlation id, para eu seguir uma requisição de ponta a ponta."

### ADR-09 · Docker multi-stage + docker-compose
- **Escolha:** Dockerfile multi-stage por serviço (SDK para build, runtime enxuto para rodar); `docker-compose.yml` sobe tudo.
- **Porquê:** imagem final menor e reprodutível; qualquer avaliador roda o projeto com um comando.
- **Em resumo:** "Com `docker compose up` sobe o sistema inteiro: serviços, gateway, RabbitMQ e bancos."

### ADR-10 · Versões
- **.NET 10 (LTS)** — versão de suporte longo atual; o .NET 8 sai de suporte em nov/2026. C# moderno (records, `init`, pattern matching, primary constructors).
- **Angular 22 (versão mais recente)** — usar a mais recente mantém o projeto atualizado. Exige Node ^22.22.3, ^24.15.0 ou ^26 e TypeScript 6.0. Criação sem depender do CLI global: `npx @angular/cli@22 new store-web`. De 20 para 22 os pilares são os mesmos (standalone, signals, novo control flow).
- **Em resumo:** "No back, a versão LTS do .NET por estabilidade e suporte; no front, o Angular mais recente, com os pilares atuais: standalone, signals e novo control flow."

---

## PARTE 2 — Design Patterns e práticas (nível código)

### ADR-11 · DDD tático no domínio
- **Entidades ricas** (comportamento junto dos dados): `Order.AddItem()`, `Order.Cancel()`, `Product.DebitStock()` — nada de setter público alterando status.
- **Value Objects** (records, igualdade por valor): `Money`, `Sku`, `Address`.
- **Aggregate** `Order` (raiz) com `OrderItem`: só se altera item pela raiz, que garante as invariantes (ex.: pedido sem itens não pode ser confirmado).
- **Domain Events** dentro do serviço (ex.: `OrderCreatedDomainEvent`) → convertidos em evento de integração para o RabbitMQ.
- **Em resumo:** "O domínio é rico: as regras ficam nas entidades e no agregado, não espalhadas em services."

### ADR-12 · CQRS sem MediatR
- **Escolha:** separar **Commands** (escrita) e **Queries** (leitura) com interfaces próprias `ICommandHandler<TCommand, TResult>` e `IQueryHandler<TQuery, TResult>`, registradas via DI.
- **Porquê:** escrita usa o domínio + EF Core; leitura pode ir direto para DTO (projeção com `Select`, ou Dapper), mais simples e rápida.
- **Por que sem MediatR:** desde 2025 o MediatR tem licença comercial para empresas; e implementar a abstração mostra que entendo o padrão em vez de só usar a biblioteca.
- **Padrões:** CQRS; a ideia do **Mediator** (desacoplar quem pede de quem executa).
- **Em resumo:** "Separei comandos e consultas. Não usei MediatR por causa da mudança de licença, e a abstração própria tem poucas linhas."

### ADR-13 · Decorator para validação e log
- **Escolha:** decorators em volta dos handlers (`ValidationDecorator`, `LoggingDecorator`) registrados com **Scrutor** (`services.Decorate(...)`).
- **Porquê:** validação e log são preocupações transversais; o handler fica só com a regra (Open/Closed + Single Responsibility).
- **Em resumo:** "Validação e log entram como decorators: adiciono comportamento sem mexer no handler."

### ADR-14 · Repository + Unit of Work
- **Escolha:** repositório **por agregado** (`IOrderRepository`, `IProductRepository`), interface na camada interna, implementação EF Core na Infrastructure. `IUnitOfWork.SaveChangesAsync()` encapsula o `DbContext`.
- **Porquê:** o domínio não conhece EF; teste com mock. O `DbContext` já é um Unit of Work — a interface só o expõe sem vazar o EF.
- **Descartado:** repositório genérico `IRepository<T>` para tudo — vira um "DbSet disfarçado" e não expressa o negócio.
- **Em resumo:** "Repositório por agregado, não genérico; e o DbContext já é um Unit of Work, eu só o abstraio."

### ADR-15 · Strategy + Factory (cálculo de frete)
- **Escolha:** `IShippingStrategy` com `StandardShipping`, `ExpressShipping`, `PickupInStoreShipping`; o `ShippingCalculator` recebe todas por `IEnumerable<IShippingStrategy>` e escolhe pelo método (Factory).
- **Por que não Keyed Services:** com a chave vinda do request, o handler teria de pedir a estratégia ao `IServiceProvider` em tempo de execução — Service Locator, que esconde dependências. Keyed Services serve bem quando a chave é fixa no código (`[FromKeyedServices("express")]`).
- **Porquê:** elimina `switch` crescente; novo tipo de frete = nova classe, sem alterar as existentes (**Open/Closed**).
- **Em resumo:** "O frete usa Strategy: cada regra numa classe, escolhida por chave. Novo tipo de frete não mexe no código existente."

### ADR-16 · Adapter / Anti-Corruption Layer (Ordering → Catalog)
- **Escolha:** no Ordering, a interface de domínio `IProductCatalog` (o que o Pedidos precisa saber) é implementada por `CatalogHttpAdapter` na Infrastructure, que chama a API do Catálogo e traduz o DTO externo.
- **Porquê:** se o contrato do Catálogo mudar, só o adapter muda. Protege o modelo do Ordering do modelo alheio (**camada anticorrupção** do DDD).
- **Em resumo:** "A integração com outro serviço passa por um Adapter: o domínio do Pedidos fala a própria língua e o adapter traduz. Fiz o mesmo com integrações SOAP em projetos reais."

### ADR-17 · Result Pattern + ProblemDetails + tratamento global de erro
- **Escolha:** erro de negócio esperado retorna `Result<T>` (sucesso/falha com código); exceção só para o inesperado. `IExceptionHandler` global converte tudo em **ProblemDetails** (RFC 7807).
- **Porquê:** exceção para fluxo de negócio é cara e esconde o caminho; ProblemDetails dá erro padronizado ao front, sem vazar stack trace.
- **Em resumo:** "Erro de negócio é resultado, não exceção; o inesperado cai num handler global que devolve ProblemDetails."

### ADR-18 · Validação com FluentValidation
- **Porquê:** regras de entrada fora dos DTOs, testáveis e reutilizáveis. Validação de **formato** na Application; **invariante de negócio** no domínio.

### ADR-19 · Options Pattern
- **Escolha:** `IOptions<JwtSettings>`, `IOptions<RabbitMqSettings>` com validação na inicialização (`ValidateOnStart`).
- **Porquê:** configuração tipada, sem string mágica espalhada.

### ADR-20 · Injeção de Dependência organizada por camada
- **Escolha:** extensões `AddApplication()`, `AddInfrastructure(config)` em cada camada; `Program.cs` enxuto.
- **Lifetimes:** `DbContext`/repositórios/handlers = **Scoped**; strategies sem estado = Singleton ou Transient; consumidores do MassTransit criam escopo próprio (evita **captive dependency**).
- **Em resumo:** "Cada camada registra suas dependências; Scoped para o que depende da requisição e cuidado para não injetar Scoped em Singleton."

### ADR-21 · Controllers (MVC) em vez de Minimal APIs
- **Porquê:** controllers deixam explícitos o model binding, os filtros e as convenções do MVC. Minimal API seria igualmente válida para serviços pequenos.
- **Em resumo:** "Usei controllers porque o time trabalha com MVC; Minimal APIs seriam uma alternativa válida em serviços menores."

### ADR-22 · Testes
- **Escolha:** xUnit + NSubstitute + Shouldly. Unitários no domínio e nos handlers (AAA). Extra: integração com `WebApplicationFactory` + **Testcontainers** (PostgreSQL real).
- **Porquê das bibliotecas:** FluentAssertions 8 passou a ter licença comercial (2025) e o Moq teve a polêmica do SponsorLink (2023) — NSubstitute e Shouldly são livres e simples.
- **Em resumo:** "Testo regra de negócio no domínio e nos handlers; integração com banco real via Testcontainers."

### ADR-23 · CI com GitHub Actions
- Build + testes a cada push/PR. Mesmo conhecimento usado na Montanari e no FleetExecutive.

---

## PARTE 3 — Front-end (Angular 22)

### ADR-24 · Angular moderno
- **Standalone components** (sem NgModule), **Signals** (`signal`, `computed`, `input()`, `output()`), **novo control flow** (`@if`, `@for ... track`), `inject()`, lazy loading com `loadComponent`, **zoneless** se estável.
- **Porquê:** é o Angular atual (22), com os recursos que viraram padrão nas últimas versões.

### ADR-25 · Organização e padrões no front
- **Feature folders** (`features/catalog`, `features/orders`) + `core` (interceptors, guards, auth) + `shared` (componentes reutilizáveis).
- **Smart/Dumb components** (container/presentational): a página busca dados; o componente de lista só recebe `input()` e emite `output()`.
- **Facade service** por feature: a página fala com um service que esconde HTTP + estado (signals).
- **Interceptors funcionais** (JWT e tratamento de erro); **guard funcional** de rota.
- **Formulários reativos tipados** (`nonNullable.group`).
- **Em resumo:** "No front, organizo por feature, separo componentes de página e de apresentação, e uso um service como facade com signals para o estado."

### ADR-32 · GraphQL para consultas do front (Hot Chocolate) — Fase 3.5
- **Divisão:**
  - **REST:** todos os comandos (criar, editar, excluir — no Catalog e no Ordering), a comunicação entre serviços (Ordering → Catalog via Adapter) e o Identity.
  - **GraphQL:** consultas do Catálogo feitas pelo front (produtos com categoria, filtro, ordenação, paginação). Sem mutations — escrita continua em REST.
  - Os GETs REST do Catalog continuam existindo para integração entre serviços e Swagger.
- **Biblioteca:** Hot Chocolate (MIT), com paginação por cursor, filtros e projeções.
- **Porquê:** o front pede só os campos que usa (evita **over-fetching**) e traz produto + categoria numa chamada (evita **under-fetching** / várias idas ao servidor). REST segue melhor para comandos e integração: simples, padronizado, cacheável por HTTP.
- **Arquitetura:** os resolvers GraphQL chamam os **mesmos query handlers** da Application. GraphQL e REST são só bordas diferentes (Api) sobre o mesmo núcleo — prova prática da Clean Architecture.
- **Gateway:** YARP roteia `/graphql` para o Catalog.
- **Cuidados citáveis:** problema **N+1** — aqui evitado pelo read model, que já traz a categoria com JOIN; quando um campo é resolvido item a item, a solução é **DataLoader**, que agrupa as buscas. Profundidade máxima da consulta limitada (`AddMaxExecutionDepthRule`).
- **Endpoint:** `/api/graphql` no Catalog; pelo Gateway, `/api/catalog/graphql` (a rota existente já cobre, sem mudança no Gateway).
- **Em resumo:** "Usei REST para comandos e integração entre serviços, e GraphQL para as consultas do front, sobre os mesmos casos de uso. Isso só foi simples porque a lógica está na Application, não nos controllers. Para evitar N+1 no GraphQL, usei DataLoader."

---

## PARTE 4 — Deploy na AWS (Fase 4, opcional)

### ADR-26 · ECS Fargate para os microsserviços
- **Escolha:** cada serviço (Gateway, Catalog, Ordering, Identity) é um **ECS Service** com tasks **Fargate**; imagens no **ECR** (um repositório por serviço).
- **Porquê:** containers gerenciados sem administrar servidor; cada serviço escala e é publicado sozinho — o mesmo princípio da arquitetura, agora na infraestrutura. Reaproveita o que já fiz no FleetExecutive.
- **Descartado:** EKS (Kubernetes) — mais poderoso, mas caro e complexo para o tamanho do projeto; EC2 com compose — funciona, mas não demonstra deploy independente por serviço.
- **Em resumo:** "Na AWS cada microsserviço é um serviço ECS em Fargate, com imagem própria no ECR: deploy e escala independentes, sem gerenciar servidor."

### ADR-27 · Rede e comunicação interna
- **ALB** só na frente do **Gateway** (único ponto público da API). Serviços internos sem acesso público, liberados só por **Security Group**.
- **ECS Service Connect** para descoberta de serviços: o Gateway e o Ordering chamam `http://catalog-api` pelo nome, sem IP fixo (padrão **Service Discovery**).
- Tasks em **subnets públicas com security groups restritivos**, para evitar o custo do **NAT Gateway** (em produção: subnets privadas + NAT ou VPC Endpoints).
- **Em resumo:** "Só o gateway é exposto pelo load balancer; os serviços se encontram por service discovery e só aceitam tráfego interno."

### ADR-28 · Dados e mensageria com economia de crédito
- **RDS PostgreSQL** (instância pequena) com **um banco por serviço**: o isolamento lógico do Database per Service se mantém; em produção seriam instâncias (ou ao menos usuários) separados. Detalhes no ADR-40.
- **RabbitMQ como task Fargate** em vez de Amazon MQ (custo bem menor). Sem persistência durável — aceitável para demonstração; em produção: Amazon MQ ou Amazon SQS/SNS.
- **Em resumo:** "Mantive banco por serviço como isolamento lógico numa instância RDS e o RabbitMQ em container, por custo; em produção usaria instâncias separadas e Amazon MQ ou SQS."

### ADR-29 · Front estático: S3 + CloudFront
- **Escolha:** build do Angular publicado num bucket **S3** privado servido pelo **CloudFront** (HTTPS, cache, CDN).
- **Porquê:** front Angular é estático — não precisa de container; fica barato e rápido.
- **Em resumo:** "O Angular é estático, então vai para S3 com CloudFront; container só para o que executa código no servidor."

### ADR-30 · Configuração, segredos e logs
- Segredos (connection strings, chave JWT) no **Secrets Manager / SSM Parameter Store**, injetados na task — nada de segredo no repositório ou na imagem.
- Logs dos containers no **CloudWatch Logs**, em JSON pelo logging nativo do .NET (ADR da Fase 3, sem Serilog). Health check do ALB apontando para `/health`.
- **Em resumo:** "Configuração vem do ambiente e segredos do Secrets Manager — a mesma imagem roda local e na nuvem (princípio do Twelve-Factor App)."

### ADR-31 · Infraestrutura como código + CI/CD
- **Terraform** descrevendo VPC, ECR, ECS, ALB, RDS, S3/CloudFront. **Porquê:** recriar e destruir tudo com um comando (economiza crédito) e é a ferramenta de IaC mais pedida no mercado. Alternativa válida: AWS CDK em C#.
- **GitHub Actions com OIDC** (sem chave de acesso guardada): build → testes → push no ECR → atualização do serviço ECS só do serviço alterado.
- **Em resumo:** "A infraestrutura é código: com Terraform subo e destruo o ambiente inteiro. O pipeline usa OIDC, sem credencial fixa, e publica só o serviço que mudou."

### ADR-35 · State remoto do Terraform no S3, com bootstrap separado
- **Escolha:** o state fica num bucket S3 versionado, criptografado, sem acesso público e com `prevent_destroy`; o lock usa `use_lockfile` (arquivo de lock no próprio S3, Terraform 1.11+). O bucket é criado por um módulo `bootstrap` à parte, o único com state local.
- **Porquê:** o state é a memória do Terraform. Local, ele se perde ou diverge entre a máquina e o pipeline; no S3 é compartilhado, tem histórico e o lock impede dois applies ao mesmo tempo. O bootstrap resolve o "ovo e a galinha": o Terraform não pode guardar o state num bucket que ele ainda vai criar.
- **Descartado:** tabela DynamoDB para lock (era o padrão até o Terraform 1.10; hoje é recurso a mais sem ganho); HCP Terraform (bom, mas outra conta e outro serviço para o tamanho do projeto).
- **Em resumo:** "O state fica no S3 com versionamento e lock nativo; o bucket vem de um bootstrap separado, e o destroy do ambiente nunca apaga o state."

### ADR-36 · Duas camadas de subnet e sem NAT Gateway
- **Escolha:** VPC própria (10.20.0.0/16) em duas zonas de disponibilidade: subnets **públicas** para ALB e tasks; subnets **de dados** sem rota para a internet, só para o RDS. O security group padrão da VPC fica sem regras.
- **Porquê:** o banco fica isolado de verdade, sem caminho de saída. O NAT Gateway custaria uns US$ 32 por mês só por existir, mais que o resto da rede; as tasks nas subnets públicas resolvem a saída para a internet, e o security group de cada uma só aceita o ALB ou os outros serviços. Duas zonas porque o ALB e o RDS exigem.
- **Em produção:** tasks em subnets privadas, com NAT Gateway ou VPC Endpoints (ECR, Secrets Manager, CloudWatch).
- **Em resumo:** "Banco em subnet isolada, sem rota para a internet. Tirei o NAT por custo e compensei com security groups fechados; em produção as tasks iriam para subnets privadas."

### ADR-37 · ECR com tag imutável e limpeza automática
- **Escolha:** um repositório por serviço; tag = SHA do commit, **imutável**; scan de vulnerabilidades no push; lifecycle policy que mantém as 10 imagens mais recentes.
- **Porquê:** tag imutável garante que `catalog-api:3f2a1c9` é sempre o mesmo código: rastreabilidade e rollback seguro (voltar é apontar para a tag anterior). `latest` muda sem aviso e não diz o que está rodando. A lifecycle policy evita pagar por imagens velhas.
- **Em resumo:** "Cada imagem tem como tag o commit que a gerou, e a tag não pode ser sobrescrita: sei exatamente o que está em produção e volto uma versão sem rebuild."

### ADR-38 · Role de deploy com OIDC e menor privilégio
- **Escolha:** role `gha-store-deploy` assumida pelo GitHub Actions via OIDC, aceita só para `repo:renatosilva1983/store-microservices:ref:refs/heads/main` (StringEquals, sem curinga). Permissão inicial: push nos repositórios ECR do projeto; ECS e S3 entram com esses recursos. O provedor OIDC da conta é lido, não criado, porque já existe e é compartilhado com outro projeto.
- **Porquê:** nenhuma chave da AWS guardada no GitHub; as credenciais são temporárias (1 hora). Restringir repositório e branch impede que um fork ou uma branch qualquer publique. Só a `main`, que no Git Flow é o que passou por release, chega na nuvem.
- **Em resumo:** "O pipeline troca o token do GitHub por credenciais temporárias; a role só aceita a branch main deste repositório e só pode fazer o que o deploy precisa."

### ADR-39 · Segredos gerados pelo Terraform, num secret JSON
- **Escolha:** o Terraform gera a senha do banco, a chave JWT e a senha do RabbitMQ (`random_password`) e grava as três num único secret do **Secrets Manager**, em JSON. A task do ECS recebe cada chave como variável de ambiente ao subir. Para isso, as APIs aceitam a senha do banco separada da conexão (`Database:Password`): a conexão-base (`POSTGRES_CONNECTION`) deixa de ter segredo.
- **Porquê:** ninguém digita, copia ou vê o segredo; nada fica no repositório, na imagem ou na definição da task. Um secret com várias chaves custa um quarto de quatro secrets.
- **Cuidado assumido:** os valores ficam no state do Terraform, que por isso é criptografado e privado (ADR-35).
- **Descartado neste ambiente:** senha do master gerenciada pelo próprio RDS (`manage_master_user_password`), que gira a senha a cada 7 dias: em produção é melhor, mas uma task que já subiu continuaria com a senha antiga até ser reiniciada. Também ficam para depois os argumentos *write-only* do Terraform 1.11+ (segredo que nem chega ao state) e a autenticação no banco por IAM.
- **Em resumo:** "Os segredos nascem no Terraform, vão para o Secrets Manager e chegam à aplicação como variável de ambiente na subida da task. Ninguém conhece a senha do banco, nem eu."

### ADR-40 · RDS PostgreSQL pequeno, isolado e descartável
- **Escolha:** uma instância `db.t4g.micro`, PostgreSQL 18 (igual ao local), disco gp3 de 20 GB criptografado, nas subnets de dados, sem acesso público; o security group só aceita a porta 5432 vinda do security group das tasks. Os bancos `catalog` e `ordering` são criados pelas migrations na subida de cada serviço.
- **Porquê:** o menor custo que ainda é um PostgreSQL gerenciado (backup, patch e criptografia pela AWS). A origem da regra é outro security group, não uma faixa de IP: vale para qualquer task, mesmo com IPs que mudam a cada deploy.
- **Simplificações de demo:** sem Multi-AZ, backup de 1 dia, sem snapshot final e sem proteção contra exclusão, para o `destroy` ser rápido. Um usuário master para os dois bancos.
- **Em produção:** Multi-AZ, backups de 7 dias ou mais, `deletion_protection`, um usuário por serviço com acesso só ao próprio banco e, com mais carga, instâncias separadas.
- **Em resumo:** "O banco fica numa subnet sem saída para a internet e só aceita conexão do security group dos serviços. Para a demo desliguei Multi-AZ e proteção contra exclusão; em produção, ligaria os dois e daria um usuário por serviço."

### Controle de custo (operacional)
- Alerta no **AWS Budgets** antes do primeiro deploy; conferir validade e cobertura dos créditos.
- Após gravar a demo: `desired_count = 0` nas tasks ou `terraform destroy`.

---

## PARTE 5 — Processo

### ADR-34 · Central Package Management
- **Escolha:** versões NuGet em `Directory.Packages.props`; configurações comuns (`TargetFramework`, `Nullable`) em `Directory.Build.props`. Os `.csproj` só dizem o nome do pacote.
- **Porquê:** com vários serviços na mesma solução, evita versões divergentes do mesmo pacote e concentra a atualização num arquivo.
- **Em resumo:** "As versões dos pacotes ficam centralizadas: atualizar o EF Core é mudar uma linha, e todos os serviços sobem juntos."

### ADR-33 · Git Flow simplificado + SemVer por fase
- **Escolha:** `main` (estável, com tags) e `develop` (integração); branches `feature/`, `bugfix/`, `release/` e `hotfix/` nomeadas `tipo/fN-escopo-descricao`. Cada fase fecha numa release (`0.1.0`, `0.2.0`, `1.0.0`...). Pull Request com modelo; CHANGELOG no formato Keep a Changelog. Detalhes em `docs/fluxo-git.md`.
- **Porquê:** o projeto tem versões numeradas por fase, e o Git Flow separa bem o que está em construção do que está publicado; o histórico conta a evolução do projeto para quem o avalia.
- **Descartado:** GitHub Flow / trunk-based — mais simples e preferidos com deploy contínuo, mas não diferenciam release e hotfix, que aqui são parte do exercício.
- **Em resumo:** "Usei Git Flow: features e bugfixes saem da develop, cada fase fecha numa release com versão semântica e tag na main, e correções urgentes vão por hotfix direto da main, voltando também para a develop. Com deploy contínuo eu iria de trunk-based."

---

## Decisões de escopo e próximos passos
- Identity com usuários fixos (em produção: Keycloak/Entra ID/IdentityServer).
- Sem Kubernetes, service discovery ou tracing distribuído (OpenTelemetry seria o próximo passo).
- Saga/orquestração de pedido não implementada; o fluxo é coreografado por eventos.
- **Em resumo:** "Mantive o escopo enxuto de propósito; os próximos passos seriam OpenTelemetry, Outbox completo e orquestração com Kubernetes."
