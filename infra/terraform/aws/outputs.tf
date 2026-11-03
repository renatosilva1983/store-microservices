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
