terraform {
  # 1.11+: lock do state direto no S3 (use_lockfile), sem tabela DynamoDB.
  required_version = ">= 1.11"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 6.0"
    }

    # Gera as senhas e a chave JWT dentro do próprio Terraform, sem ninguém digitar segredo.
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }
}
