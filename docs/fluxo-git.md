# Fluxo de Git — store-microservices

Modelo: **Git Flow simplificado** (ADR-33), com versionamento semântico por fase.

## Branches permanentes

| Branch | Papel | Recebe merge de |
|---|---|---|
| `main` | Versão estável e publicada. Cada merge recebe uma tag (`v0.1.0`) | `release/*`, `hotfix/*` |
| `develop` | Integração do que está em construção | `feature/*`, `bugfix/*`, `release/*`, `hotfix/*` |

## Branches temporárias

| Tipo | Sai de | Volta para | Quando usar | Commit típico |
|---|---|---|---|---|
| `feature/` | `develop` | `develop` | Funcionalidade nova | `feat:` |
| `bugfix/` | `develop` | `develop` | Bug encontrado antes de publicar | `fix:` |
| `release/` | `develop` | `main` e `develop` | Fechar uma versão: ajustes finais, README, CHANGELOG | `chore:` / `docs:` |
| `hotfix/` | `main` | `main` e `develop` | Bug na versão publicada que não pode esperar | `fix:` |

**Nome:** `tipo/fN-escopo-descricao` (kebab-case, inglês; `fN` = fase). Releases e hotfixes usam a versão: `release/0.1.0`, `hotfix/0.1.1`.

## Versões por fase (SemVer)

| Fase | Versão | Conteúdo |
|---|---|---|
| 1 | `0.1.0` | Catálogo |
| 2 | `0.2.0` | Gateway, Angular e Ordering |
| 3 | `1.0.0` | Eventos, JWT e acabamento — sistema completo |
| 3.5 | `1.1.0` | GraphQL no Catálogo |
| 4 | `1.2.0` | Deploy na AWS |

## Branches planejadas

### Fase 1 → `release/0.1.0`
- `feature/f1-building-blocks`
- `feature/f1-catalog-domain`
- `feature/f1-catalog-application`
- `feature/f1-catalog-infrastructure`
- `feature/f1-catalog-api`
- `feature/f1-catalog-docker`

### Fase 2 → `release/0.2.0`
- `feature/f2-central-package-management`
- `feature/f2-building-blocks-api`
- `feature/f2-ordering-domain`
- `feature/f2-ordering-shipping-strategy`
- `feature/f2-ordering-application`
- `feature/f2-ordering-infrastructure`
- `feature/f2-ordering-catalog-adapter`
- `feature/f2-ordering-api`
- `feature/f2-gateway-yarp`
- `feature/f2-web-setup`
- `feature/f2-web-catalog-list`
- `feature/f2-web-catalog-form`
- `feature/f2-docker-compose`
- `feature/f2-study-guide`

### Fase 3 → `release/1.0.0`
- `feature/f3-messaging-rabbitmq`
- `feature/f3-catalog-stock-consumer`
- `feature/f3-identity-jwt`
- `feature/f3-web-auth`
- `feature/f3-observability`
- `feature/f3-ci-github-actions`

### Fase 3.5 → `release/1.1.0`
- `feature/f35-catalog-graphql`
- `feature/f35-web-graphql-list`

### Fase 4 → `release/1.2.0`
- `feature/f4-terraform-network`
- `feature/f4-terraform-ecs`
- `feature/f4-deploy-pipeline`

### Bugfix e hotfix (criados quando o problema aparecer)
- `bugfix/f1-product-pagination-total`
- `hotfix/0.1.1`

## Ciclo de uma fase
1. `feature/*` sai da `develop` e volta por Pull Request (modelo em `.github/pull_request_template.md`).
2. Fase pronta: `release/X.Y.Z` sai da `develop`; só entram ajustes finais e `CHANGELOG.md`.
3. Merge da release na `main` + tag `vX.Y.Z` + release no GitHub; merge de volta na `develop`.
4. Problema na `main`: `hotfix/X.Y.Z+1` sai da `main`, volta para `main` (nova tag) e `develop`.

## Comandos de referência
```powershell
# Nova feature
git switch develop; git pull
git switch -c feature/f1-catalog-domain

# Fechar uma fase
git switch develop; git pull
git switch -c release/0.1.0
# ... ajustes finais, CHANGELOG ...
# PR release/0.1.0 -> main; após o merge:
git switch main; git pull
git tag -a v0.1.0 -m "Fase 1: Catálogo"
git push origin v0.1.0
# PR (ou merge) release/0.1.0 -> develop

# Hotfix
git switch main; git pull
git switch -c hotfix/0.1.1
```
