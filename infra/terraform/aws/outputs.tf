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
