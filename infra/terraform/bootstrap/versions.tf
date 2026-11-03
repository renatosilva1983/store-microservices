# Bootstrap: cria o bucket onde o Terraform principal guarda o state (ADR-35).
# Este módulo é o único com state local, porque ele cria o próprio lugar do state remoto
# (problema do "ovo e da galinha"). Roda uma vez por conta e quase nunca muda.
terraform {
  required_version = ">= 1.11"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 6.0"
    }
  }
}

provider "aws" {
  region = var.region

  default_tags {
    tags = {
      Project   = "store-microservices"
      Env       = "shared"
      ManagedBy = "Terraform"
    }
  }
}
