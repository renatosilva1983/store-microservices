# Guia de estudo — Fase 3.5 (GraphQL no Catálogo)

## O que mudou
- **Catalog.Api** expõe `POST /api/graphql` (Hot Chocolate) só para **leitura**: `products`, `product`, `categories`.
- Pelo Gateway: `http://localhost:5000/api/catalog/graphql` — a rota `/api/catalog/**` já existente reescreve para `/api/graphql`; o Gateway não precisou mudar.
- **Angular**: a listagem de produtos usa GraphQL e pede só os campos que a tabela mostra. Criar, editar e excluir continuam em REST.
- IDE do GraphQL (Nitro): abra http://localhost:5086/api/graphql no navegador com o Catalog rodando.

## Consulta usada pelo front
```graphql
query Products($name: String, $categoryId: UUID, $page: Int!, $pageSize: Int!) {
  products(name: $name, categoryId: $categoryId, page: $page, pageSize: $pageSize) {
    page pageSize totalCount totalPages
    items { id name description sku price currency stockQuantity categoryName }
  }
}
```

## Breakpoints
| # | Onde | O que observar |
|---|---|---|
| 1 | `CatalogQuery.GetProducts` | Chama o mesmo `ListProductsQuery` do REST; o `ValidationDecorator` também roda (tente `pageSize: 500`) |
| 2 | `CatalogQueries.ListProductsAsync` (Infrastructure) | Uma consulta SQL só, com JOIN da categoria: o read model já evita o N+1 |
| 3 | DevTools → Network no Angular | Um POST para `/graphql` com `query` + `variables`; a resposta tem só os campos pedidos |

## Para a entrevista
- **Over-fetching / under-fetching:** o cliente escolhe os campos e junta produto + categoria numa chamada.
- **REST × GraphQL:** REST para comandos e integração (simples, cacheável, padronizado); GraphQL para consultas flexíveis do front.
- **N+1:** aparece quando cada item resolve um campo com uma consulta própria. Aqui não acontece porque o read model faz o JOIN; quando o campo é resolvido por item, a solução é um **DataLoader**, que agrupa as buscas num lote.
- **Proteção:** profundidade máxima da consulta limitada (`AddMaxExecutionDepthRule`), para ninguém montar uma consulta gigante.
- **Erros:** em GraphQL a resposta costuma ser 200 com `errors[]`; "não encontrado" é `null` no campo.
