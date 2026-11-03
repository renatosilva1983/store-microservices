# Infraestrutura na AWS (Fase 4)

Terraform que descreve o ambiente de demonstração do store-microservices. Decisões em `docs/decisoes-arquitetura.md` (ADR-26 a ADR-40).

| Pasta | O que cria | State |
|---|---|---|
| `bootstrap/` | Bucket S3 do state remoto | Local (roda uma vez) |
| `aws/` | Rede, ECR, role do GitHub Actions, security groups, Secrets Manager, RDS e, nas próximas etapas, ECS, ALB, S3/CloudFront | Remoto, no bucket do bootstrap |

## Pré-requisitos
- Terraform 1.11 ou mais novo: `terraform -version`.
- AWS CLI com o usuário `renato-dev`: `aws sts get-caller-identity` deve mostrar a conta `060622564703`.

## Primeira vez: bootstrap
```powershell
cd infra/terraform/bootstrap
terraform init
terraform plan      # deve criar 6 recursos, todos do bucket
terraform apply
```
O `terraform.tfstate` desta pasta fica só na sua máquina (está no `.gitignore`). Se ele se perder, o bucket continua lá e pode ser reimportado com `terraform import`.

## Módulo principal
```powershell
cd infra/terraform/aws
terraform init      # conecta no bucket do state
terraform plan      # leia o plano antes de aplicar
terraform apply
terraform output    # endereços do ECR e ARN da role de deploy
```

## Aplicar só uma parte
Para subir o código novo sem criar o banco ainda (o RDS é o que começa a custar por hora):
```powershell
terraform plan -out tfplan -target="aws_security_group.services"
```
O `-target` serve para casos assim, pontuais; o normal é aplicar tudo.

## Ver um segredo (só se precisar depurar)
```powershell
aws secretsmanager get-secret-value --secret-id store-demo/app --query SecretString --output text
```

## Custo
| Etapa | Custo aproximado |
|---|---|
| Bootstrap, rede, ECR e role (esta etapa) | Praticamente zero: VPC, subnets e role não cobram; o ECR cobra centavos por GB |
| Banco RDS e Secrets Manager | Por volta de US$ 0,50 por dia enquanto existir (instância, disco e secret) |
| Ambiente completo (ECS, ALB, RDS) | Por volta de US$ 2,50 a 3 por dia |

Para o ambiente completo, o plano é subir alguns dias antes da apresentação e destruir logo depois:
```powershell
terraform destroy   # no módulo aws/; o bucket do bootstrap fica (prevent_destroy)
```

## Ordem das próximas etapas
1. Bootstrap, rede, ECR e role OIDC. Testado com `apply` e `destroy`; hoje só o bucket do state existe na conta.
2. Banco RDS e segredos (Secrets Manager). Escrito e validado com `plan`.
3. ECS (cluster, tasks, Service Connect, RabbitMQ) e ALB.
4. Front no S3 + CloudFront com domínio `store.renatosilva.dev`.
5. Pipeline de deploy no GitHub Actions.
