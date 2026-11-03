output "vpc_id" {
  value = aws_vpc.main.id
}

output "public_subnet_ids" {
  value = aws_subnet.public[*].id
}

output "data_subnet_ids" {
  value = aws_subnet.data[*].id
}

output "ecr_repository_urls" {
  description = "Endereço de cada repositório, usado no docker push e na definição das tasks."
  value       = { for name, repo in aws_ecr_repository.service : name => repo.repository_url }
}

output "github_deploy_role_arn" {
  description = "Vai no secret/variável AWS_DEPLOY_ROLE_ARN do repositório no GitHub."
  value       = aws_iam_role.github_deploy.arn
}

output "app_secret_arn" {
  description = "Secret com as chaves PostgresPassword, JwtSigningKey e RabbitMqPassword."
  value       = aws_secretsmanager_secret.app.arn
}

output "database_endpoint" {
  description = "Host do PostgreSQL, usado na variável POSTGRES_CONNECTION das tasks."
  value       = aws_db_instance.main.address
}

output "services_security_group_id" {
  value = aws_security_group.services.id
}
