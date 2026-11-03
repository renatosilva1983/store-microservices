variable "region" {
  description = "Região da AWS."
  type        = string
  default     = "us-east-1"
}

variable "project" {
  description = "Prefixo dos nomes dos recursos."
  type        = string
  default     = "store"
}

variable "environment" {
  description = "Nome do ambiente. Um só (demo): o projeto sobe para a apresentação e é destruído depois."
  type        = string
  default     = "demo"
}

variable "vpc_cidr" {
  description = "Faixa de IPs da VPC. Diferente da VPC do FleetExecutive para nunca conflitar."
  type        = string
  default     = "10.20.0.0/16"
}

variable "github_repository" {
  description = "Repositório (dono/nome) autorizado a assumir a role de deploy via OIDC."
  type        = string
  default     = "renatosilva1983/store-microservices"
}

variable "github_deploy_branch" {
  description = "Única branch que pode publicar. main = o que já passou por release (Git Flow)."
  type        = string
  default     = "main"
}

variable "services" {
  description = "Microsserviços que viram imagem no ECR e, depois, serviço no ECS."
  type        = set(string)
  default     = ["gateway", "identity-api", "catalog-api", "ordering-api"]
}
