terraform {
  # 1.11+: lock do state direto no S3 (use_lockfile), sem tabela DynamoDB.
  required_version = ">= 1.11"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 6.0"
    }
  }
}
