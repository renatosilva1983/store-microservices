# State remoto (ADR-35): fica no bucket criado por infra/terraform/bootstrap.
# - Compartilhado entre a máquina local e o pipeline, sem arquivo .tfstate solto.
# - use_lockfile: o S3 grava um arquivo de lock; dois applies ao mesmo tempo são bloqueados.
# Valores fixos: o bloco backend não aceita variáveis. O ID da conta não é segredo.
terraform {
  backend "s3" {
    bucket       = "store-microservices-tfstate-060622564703"
    key          = "aws/terraform.tfstate"
    region       = "us-east-1"
    encrypt      = true
    use_lockfile = true
  }
}
