provider "aws" {
  region = var.region

  # Tags em todos os recursos: mostra no Cost Explorer quanto custa o projeto
  # e separa o que é do store do que é do FleetExecutive na mesma conta.
  default_tags {
    tags = {
      Project    = var.project
      Env        = var.environment
      ManagedBy  = "Terraform"
      Repository = var.github_repository
    }
  }
}
